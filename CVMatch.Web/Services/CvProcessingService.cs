using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using PDFtoImage;
using SkiaSharp;

namespace CVMatch.Web.Services;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CvProcessingService : ICvProcessingService
{
    private const int PreviewDpi = 120;
    private const int PreviewJpegQuality = 85;

    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IPdfTextExtractor _textExtractor;
    private readonly IPdfPhotoExtractor _photoExtractor;
    private readonly ICvExtractionService _extraction;
    private readonly ILogger<CvProcessingService> _logger;

    public CvProcessingService(
        ApplicationDbContext db,
        IFileStorage storage,
        IPdfTextExtractor textExtractor,
        IPdfPhotoExtractor photoExtractor,
        ICvExtractionService extraction,
        ILogger<CvProcessingService> logger)
    {
        _db = db;
        _storage = storage;
        _textExtractor = textExtractor;
        _photoExtractor = photoExtractor;
        _extraction = extraction;
        _logger = logger;
    }

    public async Task ProcessAsync(int submissionId, CancellationToken ct = default)
    {
        var submission = await _db.CvSubmissions
            .FirstOrDefaultAsync(x => x.Id == submissionId, ct);

        if (submission is null)
        {
            _logger.LogWarning("İşlenecek başvuru bulunamadı: {Id}", submissionId);
            return;
        }

        submission.Status = SubmissionStatus.Processing;
        await _db.SaveChangesAsync(ct);

        try
        {
            var pdfBytes = await _storage.ReadAsync(submission.StoredFileName, ct);

            // 1. Önizleme görseli (başarısız olursa akış devam eder)
            submission.PreviewImageFileName = await TryCreatePreviewAsync(pdfBytes, ct);

            // 2. Fotoğraf (çoğu CV'de yok, normal)
            submission.PhotoFileName = await TryExtractPhotoAsync(pdfBytes, ct);

            // 3. Metin çıkarma
            var textResult = _textExtractor.Extract(pdfBytes);
            submission.ExtractedText = textResult.Text;

            if (!textResult.HasUsableText)
            {
                submission.Status = SubmissionStatus.Failed;
                submission.ErrorMessage =
                    "CV'den metin okunamadı. Dosya taranmış bir belge olabilir.";
                await _db.SaveChangesAsync(ct);
                return;
            }

            // 4. AI ile yapılandırma
            var extraction = await _extraction.ExtractAsync(textResult.Text, ct);

            if (!extraction.Success)
            {
                submission.Status = SubmissionStatus.Failed;
                submission.ErrorMessage = extraction.ErrorMessage;
                await _db.SaveChangesAsync(ct);
                return;
            }

            submission.ExtractedJson = extraction.RawJson;
            submission.Status = SubmissionStatus.AwaitingReview;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("CV işlendi: {Id}", submission.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CV işlenirken hata: {Id}", submissionId);

            submission.Status = SubmissionStatus.Failed;
            submission.ErrorMessage = "CV işlenirken beklenmeyen bir hata oluştu.";
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<string?> TryCreatePreviewAsync(byte[] pdfBytes, CancellationToken ct)
    {
        try
        {
            using var bitmap = Conversion.ToImage(
                pdfBytes,
                page: 0,
                options: new RenderOptions(Dpi: PreviewDpi));

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, PreviewJpegQuality);
            using var stream = data.AsStream();

            return await _storage.SaveAsync(stream, ".jpg", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Önizleme görseli oluşturulamadı.");
            return null;
        }
    }

    private async Task<string?> TryExtractPhotoAsync(byte[] pdfBytes, CancellationToken ct)
    {
        var photo = _photoExtractor.TryExtractPhoto(pdfBytes);
        if (photo is null) return null;

        try
        {
            using var stream = new MemoryStream(photo.Bytes);
            return await _storage.SaveAsync(stream, photo.Extension, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Aday fotoğrafı kaydedilemedi.");
            return null;
        }
    }
}