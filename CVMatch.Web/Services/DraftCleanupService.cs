using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.Entities;
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
    
        var takilmaSiniri = DateTime.UtcNow.AddMinutes(-15);

        var takilanlar = await db.CvSubmissions
            .Where(s => s.Status == SubmissionStatus.Processing
                        && s.UploadedAt <= takilmaSiniri)
            .ToListAsync(ct);

        if (takilanlar.Count > 0)
        {
            foreach (var s in takilanlar)
                s.Status = SubmissionStatus.Uploaded;

            await db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "{Sayi} taslak işleme durumunda takılı kalmıştı, yeniden denenecek.",
                takilanlar.Count);
        }

        var suresiDolan = await db.CvSubmissions
            .Where(s => s.Status != SubmissionStatus.Approved
                        && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (suresiDolan.Count == 0) return;

        var silinecekler = new List<CvSubmission>();

        foreach (var s in suresiDolan)
        {
            // Dosyalardan biri silinemezse kaydı bırak, sonraki turda tekrar denenir
            var tumDosyalarSilindi =
                TryDelete(storage, s.StoredFileName)
                & TryDelete(storage, s.PreviewImageFileName)
                & TryDelete(storage, s.PhotoFileName);

            if (tumDosyalarSilindi)
                silinecekler.Add(s);
        }

        if (silinecekler.Count == 0) return;

        db.CvSubmissions.RemoveRange(silinecekler);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Süresi dolmuş {Sayi} taslak silindi.", silinecekler.Count);

        var kalan = suresiDolan.Count - silinecekler.Count;
        if (kalan > 0)
        {
            _logger.LogWarning(
                "{Sayi} taslağın dosyaları silinemedi, kayıtları korundu.", kalan);
        }
    }

    private bool TryDelete(IFileStorage storage, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return true;

        try
        {
            storage.Delete(fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dosya silinemedi: {Dosya}", fileName);
            return false;
        }
    }
}