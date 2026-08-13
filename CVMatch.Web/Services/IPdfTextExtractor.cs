namespace CVMatch.Web.Services;

public record PdfTextResult(string Text, int PageCount, bool HasUsableText);

public interface IPdfTextExtractor
{
    PdfTextResult Extract(byte[] pdfBytes);
}