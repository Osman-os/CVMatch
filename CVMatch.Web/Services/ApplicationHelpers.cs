using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Services;

public static class ApplicationHelpers
{
    // Karışabilecek karakterler çıkarıldı: I, O, 0, 1
    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    // Örn: CVM-2026-7K4P9Q
    public static string GenerateReferenceNumber()
    {
        var suffix = new char[6];
        for (var i = 0; i < suffix.Length; i++)
            suffix[i] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];

        return $"CVM-{DateTime.UtcNow.Year}-{new string(suffix)}";
    }

    /// <summary>
    /// Adayın elindeki ham anahtarı doğrular ve karşılık gelen özeti döner.
    /// Anahtar geçersiz biçimdeyse null döner.
    /// </summary>
    public static string? TryHashEditKey(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey)) return null;
        if (!Guid.TryParse(rawKey, out var token)) return null;

        return HashEditToken(token);
    }

    /// <summary>
    /// Taslak bağlantısının hâlâ kullanılabilir olup olmadığını söyler.
    /// Onaylanmış başvurunun taslağı ve süresi dolmuş taslak geçersizdir.
    /// </summary>
    public static bool DraftIsValid(SubmissionStatus status, DateTime expiresAt)
        => status != SubmissionStatus.Approved && expiresAt > DateTime.UtcNow;

    /// <summary>
    /// Telefon numarasını yalnızca rakamlara indirger ve baştaki ülke kodunu atar.
    /// "+90 532 456 78 90" ve "0532 456 78 90" aynı sonucu verir.
    /// </summary>
    public static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length < 7) return null;

        // Son 10 hane karşılaştırma için yeterli
        return digits.Length > 10 ? digits[^10..] : digits;
    }
    
    // Adaya ham token gösterilir, veritabanında yalnızca özeti saklanır
    public static Guid GenerateEditToken() => Guid.NewGuid();

    public static string HashEditToken(Guid token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.ToString("N")));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Yetenek adını sözlükteki yazımla hizalar.
    /// Sözlükte yoksa ve giriş tamamı küçük/büyük harfse ilk harfi büyütür.
    /// </summary>
    public static string NormalizeSkillName(string raw, IReadOnlyDictionary<string, string> existingSkills)
    {
        var trimmed = raw.Trim();

        // Sözlükte varsa oradaki yazım kazanır
        if (existingSkills.TryGetValue(trimmed, out var canonical))
            return canonical;

        var isAllLower = trimmed == trimmed.ToLowerInvariant();
        var isAllUpper = trimmed == trimmed.ToUpperInvariant();

        // "JavaScript", "PostgreSQL" gibi karışık yazımlara dokunma
        if (!isAllLower && !isAllUpper)
            return trimmed;

        return CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase(trimmed.ToLowerInvariant());
    }
}