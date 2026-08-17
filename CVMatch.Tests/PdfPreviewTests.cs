using PDFtoImage;
using SkiaSharp;

namespace CVMatch.Tests;

public class PdfPreviewTests
{
    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void IlkSayfa_JpegOlarakUretilebilir()
    {
        var pdfPath = TestPaths.TestCvPdf;
        Assert.True(File.Exists(pdfPath), $"Test PDF bulunamadı: {pdfPath}");

        var outputDir = TestPaths.CreateOutputDirectory();
        var outPath = Path.Combine(outputDir, "test-cv-onizleme.jpg");

        var pdfBytes = File.ReadAllBytes(pdfPath);

        using SKBitmap bitmap = Conversion.ToImage(
            pdfBytes,
            page: 0,
            options: new RenderOptions(Dpi: 150));

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

        using (var fs = File.OpenWrite(outPath))
        {
            data.SaveTo(fs);
        }

        Assert.True(bitmap.Width > 0 && bitmap.Height > 0);
        Assert.True(new FileInfo(outPath).Length > 0, "Önizleme dosyası oluşmadı.");
    }
}