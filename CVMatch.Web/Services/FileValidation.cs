namespace CVMatch.Web.Services;

public record FileValidationResult(bool IsValid, string? ErrorMessage);

public static class FileValidation
{
    public const long MaxPdfBytes = 10 * 1024 * 1024;   // 10 MB

    public static FileValidationResult ValidatePdf(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return new(false, "Lütfen bir PDF dosyası seçin.");

        if (file.Length > MaxPdfBytes)
            return new(false, "Dosya boyutu 10 MB'ı aşamaz.");
        
        if (Path.GetFileName(file.FileName).Length > 260)
            return new(false, "Dosya adı çok uzun. Lütfen daha kısa bir adla yeniden deneyin.");

        if (!Path.GetExtension(file.FileName)
                 .Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return new(false, "Yalnızca PDF biçimindeki CV dosyalarını yükleyebilirsiniz.");

        // Uzantıya güvenmiyoruz, dosya imzasını kontrol ediyoruz
        if (!HasSignature(file, "%PDF"u8.ToArray()))
            return new(false, "Dosya geçerli bir PDF değil.");

        return new(true, null);
    }

    private static bool HasSignature(IFormFile file, byte[] signature)
    {
        using var stream = file.OpenReadStream();
        var buffer = new byte[signature.Length];

        var read = stream.Read(buffer, 0, buffer.Length);
        if (read < signature.Length) return false;

        return buffer.SequenceEqual(signature);
    }
}