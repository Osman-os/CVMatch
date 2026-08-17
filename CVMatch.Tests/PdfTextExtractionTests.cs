using CVMatch.Web.Services;

namespace CVMatch.Tests;

public class PdfTextExtractionTests
{
    [Fact]
    public void MetinTabanliPdf_MetinCikarilabilir()
    {
        var pdfPath = TestPaths.TestCvPdf;
        Assert.True(File.Exists(pdfPath), $"Test PDF bulunamadı: {pdfPath}");

        var extractor = new PdfPigTextExtractor();
        var result = extractor.Extract(File.ReadAllBytes(pdfPath));

        Assert.True(result.PageCount > 0);
        Assert.True(result.HasUsableText, "Metin çıkarılamadı, PDF taranmış olabilir.");

        // Gözle kontrol için geçici klasöre yaz
        var outputDir = TestPaths.CreateOutputDirectory();
        File.WriteAllText(Path.Combine(outputDir, "test-cv-metin.txt"), result.Text);
    }
}