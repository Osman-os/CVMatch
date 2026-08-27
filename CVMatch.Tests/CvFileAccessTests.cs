using CVMatch.Web.Controllers;
using CVMatch.Web.Data;
using CVMatch.Web.Models.Entities;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CVMatch.Tests;

/// <summary>
/// CvController.File action'ının taslak geçerliliğini gerçekten uyguladığını doğrular.
/// Yardımcı metodu değil, action'ın kendisini test eder.
/// </summary>
public class CvFileAccessTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CvController CreateController(ApplicationDbContext db)
        => new(db, new SahteDepolama(), new SahteIsleme(),
               NullLogger<CvController>.Instance);

    private static async Task<Guid> SubmissionEkleAsync(
        ApplicationDbContext db, SubmissionStatus status, DateTime expiresAt)
    {
        var token = Guid.NewGuid();

                db.CvSubmissions.Add(new CvSubmission
        {
            Token = token,
            OriginalFileName = "test.pdf",
            StoredFileName = "abc.pdf",
            FileSizeBytes = 1024,
            Status = status,
            UploadedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = expiresAt,

            RowVersion = Guid.NewGuid()
        });

        await db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task OnaylanmisBasvuru_DosyaErisimi_NotFound()
    {
        using var db = CreateDb();

        var token = await SubmissionEkleAsync(
            db, SubmissionStatus.Approved, DateTime.UtcNow.AddHours(12));

        var sonuc = await CreateController(db).File(token, "pdf", default);

        Assert.IsType<NotFoundResult>(sonuc);
    }

    [Fact]
    public async Task SuresiDolmusTaslak_DosyaErisimi_NotFound()
    {
        using var db = CreateDb();

        var token = await SubmissionEkleAsync(
            db, SubmissionStatus.AwaitingReview, DateTime.UtcNow.AddMinutes(-1));

        var sonuc = await CreateController(db).File(token, "pdf", default);

        Assert.IsType<NotFoundResult>(sonuc);
    }

    [Fact]
    public async Task BilinmeyenToken_DosyaErisimi_NotFound()
    {
        using var db = CreateDb();

        var sonuc = await CreateController(db).File(Guid.NewGuid(), "pdf", default);

        Assert.IsType<NotFoundResult>(sonuc);
    }

    // ---- Sahte bağımlılıklar ----

    private sealed class SahteDepolama : IFileStorage
    {
        public Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
            => Task.FromResult("sahte" + extension);

        public Task<byte[]> ReadAsync(string storedFileName, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<byte>());

        public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream());

        // Dosya var sayılır; testin başarısı yalnızca erişim kuralına bağlı kalsın
        public bool Exists(string storedFileName) => true;

        public void Delete(string storedFileName) { }
    }

    private sealed class SahteIsleme : ICvProcessingService
    {
        public Task ProcessAsync(int submissionId, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}