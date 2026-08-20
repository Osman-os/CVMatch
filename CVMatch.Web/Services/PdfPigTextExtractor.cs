using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CVMatch.Web.Services;

public class PdfPigTextExtractor : IPdfTextExtractor
{     
    private const int MinimumUsableLength = 100;

    private const int MaxPagesToRead = 30;

    public PdfTextResult Extract(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);

        var sb = new StringBuilder();
        var pageCount = document.NumberOfPages;

        foreach (var page in document.GetPages().Take(MaxPagesToRead))
        {
            var pageText = ContentOrderTextExtractor.GetText(page);

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText);
                sb.AppendLine();
            }
        }

        var text = Normalize(sb.ToString());
        var hasUsableText = text.Length >= MinimumUsableLength;

        return new PdfTextResult(text, pageCount, hasUsableText);
    }

    private static string Normalize(string raw)
    {
        var lines = raw
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .ToList();

        var sb = new StringBuilder();
        var previousWasEmpty = false;

        foreach (var line in lines)
        {
            var isEmpty = line.Length == 0;

            // Arka arkaya birden fazla boş satırı teke indir
            if (isEmpty && previousWasEmpty) continue;

            sb.AppendLine(line);
            previousWasEmpty = isEmpty;
        }

        return sb.ToString().Trim();
    }
}