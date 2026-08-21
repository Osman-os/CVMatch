using System.ComponentModel.DataAnnotations;
using CVMatch.Web.Models.Enums;
using System.Globalization;

namespace CVMatch.Web.Models.ViewModels;

public class CvReviewViewModel : IValidatableObject
{
    public Guid Token { get; set; }

    public string? PreviewImageFileName { get; set; }
    public string? PhotoFileName { get; set; }

    // AI çıkarımı başarısız olduysa form boş gelir
    public bool IsManualEntry { get; set; }

    [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
    [StringLength(150)]
    [Display(Name = "Ad Soyad")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Başvuru türü seçmeniz gerekmektedir.")]
    [Display(Name = "Başvuru Türü")]
    public EmploymentType? PreferredEmploymentType { get; set; }

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
        ErrorMessage = "Geçerli bir e-posta adresi girin. Örnek: ad.soyad@ornek.com")]
    [StringLength(256)]
    [Display(Name = "E-posta Adresi")]
    public string? Email { get; set; }

    // 10-13 rakam içermeli; boşluk, +, parantez ve tire serbest, harf kabul edilmez
    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [RegularExpression(@"^(?=(?:\D*\d){10,13}\D*$)[0-9+\s().\-]+$",
        ErrorMessage = "Geçerli bir telefon numarası girin. Örnek: 0532 123 45 67")]
    [StringLength(30)]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Şehir")]
    public int? CityId { get; set; }

    [StringLength(500)]
    [Display(Name = "Açık Adres")]
    public string? Address { get; set; }

    [Range(0, 60, ErrorMessage = "Deneyim yılı 0 ile 60 arasında olmalıdır.")]
    [Display(Name = "Toplam Deneyim — Yıl")]
    public int ExperienceYears { get; set; }

    [Range(0, 11, ErrorMessage = "Deneyim ayı 0 ile 11 arasında olmalıdır.")]
    [Display(Name = "Toplam Deneyim — Ay")]
    public int ExperienceMonths { get; set; }

    [RegularExpression(@"^https://[^\s]+\.[^\s]+$",
        ErrorMessage = "Bağlantı https:// ile başlamalıdır. Örnek: https://linkedin.com/in/kullanici")]
    [StringLength(300)]
    [Display(Name = "LinkedIn Bağlantısı")]
    public string? LinkedInUrl { get; set; }

    [RegularExpression(@"^https://[^\s]+\.[^\s]+$",
        ErrorMessage = "Bağlantı https:// ile başlamalıdır. Örnek: https://github.com/kullanici")]
    [StringLength(300)]
    [Display(Name = "GitHub Bağlantısı")]
    public string? GitHubUrl { get; set; }

    public List<EducationInputModel> Educations { get; set; } = new();
    public List<WorkExperienceInputModel> WorkExperiences { get; set; } = new();

    [StringLength(5000, ErrorMessage = "Yetenek listesi çok uzun.")]
    public string? SkillsCsv { get; set; }

    // Dropdown için
    public List<CityOption> Cities { get; set; } = new();

    public int TotalExperienceMonths => ExperienceYears * 12 + ExperienceMonths;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var yetenekler = (SkillsCsv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (yetenekler.Count > 50)
        {
            yield return new ValidationResult(
                "En fazla 50 yetenek ekleyebilirsiniz.", new[] { nameof(SkillsCsv) });
        }

        var uzunYetenek = yetenekler.FirstOrDefault(y => y.Length > 100);
        if (uzunYetenek is not null)
        {
            yield return new ValidationResult(
                $"Yetenek adı en fazla 100 karakter olabilir: \"{uzunYetenek[..30]}...\"",
                new[] { nameof(SkillsCsv) });
        }

        if (Educations.Count > 20)
        {
            yield return new ValidationResult(
                "En fazla 20 eğitim kaydı ekleyebilirsiniz.", new[] { nameof(Educations) });
        }

        if (WorkExperiences.Count > 30)
        {
            yield return new ValidationResult(
                "En fazla 30 iş deneyimi ekleyebilirsiniz.", new[] { nameof(WorkExperiences) });
        }
    }
}

public record CityOption(int Id, string Name);

public class EducationInputModel : IValidatableObject
{
    [StringLength(200)]
    [Display(Name = "Okul")]
    public string? School { get; set; }

    [StringLength(150)]
    [Display(Name = "Bölüm")]
    public string? FieldOfStudy { get; set; }

    [Display(Name = "Eğitim Düzeyi")]
    public EducationLevel? Level { get; set; }

    // "MM/yyyy" biçiminde
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{4}$",
        ErrorMessage = "Tarihi aa/yyyy biçiminde girin. Örnek: 06/2019")]
    [Display(Name = "Başlangıç Tarihi")]
    public string? StartDate { get; set; }

    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{4}$",
        ErrorMessage = "Tarihi aa/yyyy biçiminde girin. Örnek: 06/2019")]
    [Display(Name = "Bitiş Tarihi")]
    public string? EndDate { get; set; }

    public bool IsCurrent { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
        => DateRules.Validate(StartDate, EndDate, IsCurrent, School);
}

public class WorkExperienceInputModel : IValidatableObject
{
    [StringLength(150)]
    [Display(Name = "Pozisyon")]
    public string? Position { get; set; }

    [StringLength(200)]
    [Display(Name = "Kurum")]
    public string? CompanyName { get; set; }

    [StringLength(1000)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{4}$",
        ErrorMessage = "Tarihi aa/yyyy biçiminde girin. Örnek: 06/2019")]
    [Display(Name = "Başlangıç Tarihi")]
    public string? StartDate { get; set; }

    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{4}$",
        ErrorMessage = "Tarihi aa/yyyy biçiminde girin. Örnek: 06/2019")]
    [Display(Name = "Bitiş Tarihi")]
    public string? EndDate { get; set; }

    public bool IsCurrent { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
        => DateRules.Validate(StartDate, EndDate, IsCurrent, CompanyName);
}

// Eğitim ve iş deneyimi satırları için ortak tarih kuralları.
internal static class DateRules
{
    public static IEnumerable<ValidationResult> Validate(
        string? startDate, string? endDate, bool isCurrent, string? satirAdi)
    {
        var etiket = string.IsNullOrWhiteSpace(satirAdi) ? "Satır" : satirAdi;

        // Boş satırlar zaten filtreleniyor
        if (string.IsNullOrWhiteSpace(satirAdi))
            yield break;

        var basla = Parse(startDate);
        var bitis = Parse(endDate);

        if (!isCurrent && string.IsNullOrWhiteSpace(endDate))
        {
            yield return new ValidationResult(
                $"{etiket}: Bitiş tarihi girin veya \"Devam Ediyor\" seçeneğini işaretleyin.");
        }

        if (basla is not null && bitis is not null && bitis < basla)
        {
            yield return new ValidationResult(
                $"{etiket}: Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        if (basla is not null && basla > DateTime.UtcNow)
        {
            yield return new ValidationResult(
                $"{etiket}: Başlangıç tarihi gelecekte olamaz.");
        }
    }

    private static DateTime? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateTime.TryParseExact(
            value, "MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d)
            ? d
            : null;
    }
}