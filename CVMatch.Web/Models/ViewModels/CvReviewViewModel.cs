using System.ComponentModel.DataAnnotations;
using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class CvReviewViewModel
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

    [Url(ErrorMessage = "Bağlantı https:// ile başlamalıdır.")]
    [StringLength(300)]
    [Display(Name = "LinkedIn Bağlantısı")]
    public string? LinkedInUrl { get; set; }

    [Url(ErrorMessage = "Bağlantı https:// ile başlamalıdır.")]
    [StringLength(300)]
    [Display(Name = "GitHub Bağlantısı")]
    public string? GitHubUrl { get; set; }

    public List<EducationInputModel> Educations { get; set; } = new();
    public List<WorkExperienceInputModel> WorkExperiences { get; set; } = new();

    // Virgülle ayrılmış yetenek listesi (gizli alanda taşınır)
    public string? SkillsCsv { get; set; }

    // Dropdown için
    public List<CityOption> Cities { get; set; } = new();

    public int TotalExperienceMonths => ExperienceYears * 12 + ExperienceMonths;
}

public record CityOption(int Id, string Name);

public class EducationInputModel
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
}

public class WorkExperienceInputModel
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
}