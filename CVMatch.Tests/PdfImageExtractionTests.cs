using UglyToad.PdfPig;

namespace CVMatch.Tests;

public class PdfImageExtractionTests
{
    [Fact]
    public void PdfIcindekiGorseller_Cikarilabilir()
    {
        var pdfPath = TestPaths.TestCvPdf;
        Assert.True(File.Exists(pdfPath), $"Test PDF bulunamadı: {pdfPath}");

        var outputDir = TestPaths.CreateOutputDirectory();

        using var document = PdfDocument.Open(pdfPath);

        var index = 0;
        foreach (var page in document.GetPages())
        {
            foreach (var image in page.GetImages())
            {
                index++;

                var bounds = image.BoundingBox;
                var info =
                    $"#{index}  {image.WidthInSamples}x{image.HeightInSamples} px  " +
                    $"sayfadaki alan: {bounds.Width:F0}x{bounds.Height:F0}";

                // Önce hazır bayt dizisi varsa onu kullan
                if (image.TryGetPng(out var png))
                {
                    File.WriteAllBytes(Path.Combine(outputDir, $"gorsel-{index}.png"), png);
                }
                else
                {
                    File.WriteAllBytes(
                        Path.Combine(outputDir, $"gorsel-{index}.bin"),
                        image.RawBytes.ToArray());
                }

                File.AppendAllLines(Path.Combine(outputDir, "bilgi.txt"), new[] { info });
            }
        }

        Assert.True(index > 0, "PDF içinde gömülü görsel bulunamadı.");
    }
}