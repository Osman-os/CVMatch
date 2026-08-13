namespace CVMatch.Web.Services;

public record ExtractedPhoto(byte[] Bytes, string Extension, int Width, int Height);

public interface IPdfPhotoExtractor
{
    /// <summary>
    /// PDF içindeki gömülü görsellerden aday fotoğrafı olmaya en uygun olanı döner.
    /// Uygun görsel bulunamazsa null döner.
    /// </summary>
    ExtractedPhoto? TryExtractPhoto(byte[] pdfBytes);
}