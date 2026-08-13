using System.ComponentModel.DataAnnotations;
using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class CvSummaryViewModel
{
    public Guid Token { get; set; }

    // Kontrol ekranında onaylanan veri, burada salt okunur gösterilir
    public CvReviewViewModel Data { get; set; } = new();

    public string? CityName { get; set; }
    public List<string> Skills { get; set; } = new();

    public string OriginalFileName { get; set; } = null!;
    public bool HasPreview { get; set; }

    public CvConsentInputModel Consent { get; set; } = new();

    // 14 ayı "1 yıl 2 ay" biçimine çevirir
    public string TotalExperienceText
    {
        get
        {
            var months = Data.TotalExperienceMonths;
            if (months <= 0) return "Belirtilmedi";

            var years = months / 12;
            var rest = months % 12;

            if (years == 0) return $"{rest} ay";
            if (rest == 0) return $"{years} yıl";
            return $"{years} yıl {rest} ay";
        }
    }

    public string EmploymentTypeText => Data.PreferredEmploymentType switch
    {
        EmploymentType.Internship => "Staj",
        EmploymentType.FullTime => "Tam Zamanlı",
        _ => "Belirtilmedi"
    };
}

public class CvConsentInputModel
{
    public Guid Token { get; set; }

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Aydınlatma metnini okuduğunuzu onaylamanız gerekiyor.")]
    public bool KvkkAydinlatmaOnayi { get; set; }

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Verilerinizin işlenmesi için açık rıza vermeniz gerekiyor.")]
    public bool AcikRizaOnayi { get; set; }

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Bilgilerin doğruluğunu beyan etmeniz gerekiyor.")]
    public bool DogrulukBeyani { get; set; }
}