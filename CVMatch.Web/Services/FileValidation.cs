namespace CVMatch.Web.Services;

public record FileValidationResult(bool IsValid, string? ErrorMessage);

public static class FileValidation
{
    public const long MaxPdfBytes = 10 * 1024 * 1024;   // 10 MB
    public const long MaxImageBytes = 2 * 1024 * 1024;  // 2 MB

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

    public static FileValidationResult ValidateImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return new(false, "Lütfen bir görsel seçin.");

        if (file.Length > MaxImageBytes)
            return new(false, "Fotoğraf boyutu 2 MB'ı aşamaz.");
        
        if (Path.GetFileName(file.FileName).Length > 260)
            return new(false, "Dosya adı çok uzun. Lütfen daha kısa bir adla yeniden deneyin.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            return new(false, "Yalnızca JPG veya PNG dosyası yükleyebilirsiniz.");

        var isJpeg = HasSignature(file, new byte[] { 0xFF, 0xD8, 0xFF });
        var isPng = HasSignature(file, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        if (!isJpeg && !isPng)
            return new(false, "Dosya geçerli bir görsel değil.");

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