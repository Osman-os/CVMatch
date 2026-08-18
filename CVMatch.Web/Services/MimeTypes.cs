namespace CVMatch.Web.Services;

public static class MimeTypes
{
    /// <summary>Saklanan dosya adının uzantısına göre içerik türünü döner.</summary>
    public static string FromFileName(string? fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}