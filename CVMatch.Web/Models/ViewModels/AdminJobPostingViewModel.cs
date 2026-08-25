using System.ComponentModel.DataAnnotations;
using CVMatch.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVMatch.Web.Models.ViewModels;

public class JobPostingListViewModel
{
    public List<IlanSatiri> Ilanlar { get; set; } = new();

    public string? Arama { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public JobPostingStatus? Status { get; set; }
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 20;
    public int ToplamKayit { get; set; }

    public int ToplamSayfa =>
        ToplamKayit == 0 ? 1 : (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu);

    public bool OncekiVar => Sayfa > 1;
    public bool SonrakiVar => Sayfa < ToplamSayfa;

    public bool KapalilariGoster { get; set; }

    public bool FiltreVarMi =>
        !string.IsNullOrWhiteSpace(Arama) || EmploymentType.HasValue || Status.HasValue;
}

public class IlanSatiri
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? CityName { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public JobPostingStatus Status { get; set; }
    public int MinExperienceYears { get; set; }
    public int SkillCount { get; set; }
    public int BasvuranSayisi { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobPostingEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(200, ErrorMessage = "İlan başlığı en fazla 200 karakter olabilir.")]
    [Display(Name = "İlan Başlığı")]
    public string Title { get; set; } = null!;

    [Display(Name = "Çalışma Türü")]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    [StringLength(4000, ErrorMessage = "Açıklama en fazla 4000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Range(0, 40, ErrorMessage = "Deneyim 0 ile 40 yıl arasında olmalıdır.")]
    [Display(Name = "Asgari Deneyim (yıl)")]
    public int MinExperienceYears { get; set; }

    [Display(Name = "Şehir")]
    public int? CityId { get; set; }

    [Display(Name = "Durum")]
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;

    // Formdan gelen seçimler
    public List<int> ZorunluSkillIds { get; set; } = new();
    public List<int> TercihSkillIds { get; set; } = new();

    // Dropdown ve seçim listeleri
    public List<SelectListItem> Cities { get; set; } = new();
    public List<YetenekSecenegi> TumYetenekler { get; set; } = new();

    public bool YeniMi => Id == 0;
}

public class YetenekSecenegi
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Zorunlu { get; set; }
    public bool Tercih { get; set; }
}