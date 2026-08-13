using PDFtoImage;
using SkiaSharp;

namespace CVMatch.Tests;

public class PdfPreviewTests
{
    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void IlkSayfa_JpegOlarakUretilebilir()
    {
        var pdfPath = @"C:\Dev\CVMatch\test-cv.pdf";
        var outPath = @"C:\Dev\CVMatch\test-cv-onizleme.jpg";

        Assert.True(File.Exists(pdfPath), $"Test PDF bulunamadı: {pdfPath}");

        var pdfBytes = File.ReadAllBytes(pdfPath);

        using SKBitmap bitmap = Conversion.ToImage(
            pdfBytes,
            page: 0,
            options: new RenderOptions(Dpi: 150));

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        using var fs = File.OpenWrite(outPath);
        data.SaveTo(fs);

        Assert.True(bitmap.Width > 0 && bitmap.Height > 0);
    }
}