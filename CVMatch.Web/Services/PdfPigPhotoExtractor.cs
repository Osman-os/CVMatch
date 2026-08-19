using UglyToad.PdfPig;

namespace CVMatch.Web.Services;

public class PdfPigPhotoExtractor : IPdfPhotoExtractor
{
    // İkon ve logoları elemek için alt sınır
    private const int MinDimension = 200;

    // Vesikalık portre veya kareye yakındır
    private const double MinAspectRatio = 0.5;   // genişlik / yükseklik
    private const double MaxAspectRatio = 1.3;

    // Yalnızca ilk sayfalara bak; fotoğraf sonlarda olmaz
    private const int MaxPagesToScan = 2;

    private readonly ILogger<PdfPigPhotoExtractor> _logger;

    public PdfPigPhotoExtractor(ILogger<PdfPigPhotoExtractor> logger)
    {
        _logger = logger;
    }

    public ExtractedPhoto? TryExtractPhoto(byte[] pdfBytes)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);

            ExtractedPhoto? best = null;
            long bestArea = 0;

            var pageIndex = 0;
            foreach (var page in document.GetPages())
            {
                if (++pageIndex > MaxPagesToScan) break;

                foreach (var image in page.GetImages())
                {
                    var width = image.WidthInSamples;
                    var height = image.HeightInSamples;

                    if (width < MinDimension || height < MinDimension)
                        continue;

                    var aspect = (double)width / height;
                    if (aspect < MinAspectRatio || aspect > MaxAspectRatio)
                        continue;

                    var bytes = GetBytes(image, out var extension);
                    if (bytes is null || bytes.Length == 0)
                        continue;

                    long area = (long)width * height;
                    if (area <= bestArea)
                        continue;

                    bestArea = area;
                    best = new ExtractedPhoto(bytes, extension, width, height);
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            // Fotoğraf bulunamaması akışı durdurmaz
            _logger.LogWarning(ex, "PDF'ten fotoğraf çıkarılamadı.");
            return null;
        }
    }

    private static byte[]? GetBytes(UglyToad.PdfPig.Content.IPdfImage image, out string extension)
    {
        if (image.TryGetPng(out var png) && png is { Length: > 0 })
        {
            extension = ".png";
            return png;
        }

        var raw = image.RawBytes.ToArray();
        if (raw.Length == 0)
        {
            extension = string.Empty;
            return null;
        }

        extension = DetectExtension(raw);

        if (extension == ".bin")
            return null;

        return raw;
    }

    private static string DetectExtension(byte[] bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ".jpg";

        // PNG: 89 50 4E 47
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 &&
            bytes[2] == 0x4E && bytes[3] == 0x47)
            return ".png";

        return ".bin";
    }
}