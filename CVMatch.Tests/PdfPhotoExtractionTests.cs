using CVMatch.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CVMatch.Tests;

public class PdfPhotoExtractionTests
{
    [Fact]
    public void AdayFotografi_Ayiklanabilir()
    {
        var pdfPath = @"C:\Dev\CVMatch\test-cv.pdf";
        Assert.True(File.Exists(pdfPath), $"Test PDF bulunamadı: {pdfPath}");

        var extractor = new PdfPigPhotoExtractor(
            NullLogger<PdfPigPhotoExtractor>.Instance);

        var photo = extractor.TryExtractPhoto(File.ReadAllBytes(pdfPath));

        Assert.NotNull(photo);
        Assert.Equal(".jpg", photo!.Extension);
        Assert.True(photo.Width >= 200 && photo.Height >= 200);

        File.WriteAllBytes(
            $@"C:\Dev\CVMatch\test-cv-foto{photo.Extension}",
            photo.Bytes);
    }
}