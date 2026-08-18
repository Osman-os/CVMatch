using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Services;

/// <summary>
/// Süresi dolmuş, onaylanmamış taslakları ve dosyalarını siler.
/// Saatte bir çalışır.
/// </summary>
public class DraftCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<DraftCleanupService> _logger;

    public DraftCleanupService(IServiceProvider services, ILogger<DraftCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await TemizleAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Taslak temizliği başarısız oldu.");
            }

            try
            {
                await Task.Delay(Interval, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task TemizleAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var suresiDolan = await db.CvSubmissions
            .Where(s => s.Status != SubmissionStatus.Approved
                        && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (suresiDolan.Count == 0) return;

        foreach (var s in suresiDolan)
        {
            // Dosya silinemese bile kayıt temizlenmeli
            TryDelete(storage, s.StoredFileName);
            TryDelete(storage, s.PreviewImageFileName);
            TryDelete(storage, s.PhotoFileName);
        }

        db.CvSubmissions.RemoveRange(suresiDolan);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Süresi dolmuş {Sayi} taslak silindi.", suresiDolan.Count);
    }

    private void TryDelete(IFileStorage storage, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        try
        {
            storage.Delete(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dosya silinemedi: {Dosya}", fileName);
        }
    }
}