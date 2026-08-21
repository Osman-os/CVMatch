using CVMatch.Web.Services;

namespace CVMatch.Tests;

public class PdfPageLimitTests
{
    private static string CokSayfaliPdf =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "cok-sayfali.pdf");

    [Fact]
    public void OtuzSayfadanSonrasi_Okunmaz()
    {
        Assert.True(File.Exists(CokSayfaliPdf),
            $"Çok sayfalı test PDF'i bulunamadı: {CokSayfaliPdf}");

        var extractor = new PdfPigTextExtractor();
        var sonuc = extractor.Extract(File.ReadAllBytes(CokSayfaliPdf));

        Assert.True(sonuc.PageCount > 30,
            "Test PDF'i 30 sayfadan uzun olmalı.");

        Assert.Contains("SAYFA 1", sonuc.Text);
        Assert.Contains("SAYFA 30", sonuc.Text);

        Assert.DoesNotContain("SAYFA 31", sonuc.Text);
    }
}