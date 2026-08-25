using CVMatch.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVMatch.Web.Models.ViewModels;

public class AdminCandidateListViewModel
{
    // Filtreler
    public string? Arama { get; set; }
    public int? CityId { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public ApplicationStatus? Status { get; set; }
    public List<int> SkillIds { get; set; } = new();
    public int? MinDeneyimYil { get; set; }

    // Sayfalama
    public int Sayfa { get; set; } = 1;
    public int ToplamKayit { get; set; }
    public int SayfaBoyutu { get; set; } = 20;

    public int ToplamSayfa =>
        ToplamKayit == 0 ? 1 : (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu);

    public bool OncekiVar => Sayfa > 1;
    public bool SonrakiVar => Sayfa < ToplamSayfa;

    // Dropdown kaynakları
    public List<SelectListItem> Cities { get; set; } = new();
    public List<SkillSecimi> TumYetenekler { get; set; } = new();

    public List<AdaySatiri> Adaylar { get; set; } = new();

    public bool FiltreVarMi =>
    !string.IsNullOrWhiteSpace(Arama) || CityId.HasValue
        || EmploymentType.HasValue || Status.HasValue || SkillIds.Count > 0
        || MinDeneyimYil.HasValue;
    
    public string? SecilenSehir { get; set; }
    public List<string> SecilenYetenekler { get; set; } = new();

    public int AktifFiltreSayisi
    {
        get
        {
            var n = 0;
            if (!string.IsNullOrWhiteSpace(Arama)) n++;
            if (CityId.HasValue) n++;
            if (EmploymentType.HasValue) n++;
            if (Status.HasValue) n++;
            if (MinDeneyimYil.HasValue) n++;
            n += SkillIds.Count;
            return n;
        }
    }
}

public class SkillSecimi
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Secili { get; set; }
}

public class AdaySatiri
{
    public int Id { get; set; }
    public string ApplicationReferenceNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? CityName { get; set; }
    public int TotalExperienceMonths { get; set; }
    public EmploymentType PreferredEmploymentType { get; set; }
    public string? IlanBasligi { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<string> Skills { get; set; } = new();

    public string DeneyimMetni
    {
        get
        {
            if (TotalExperienceMonths <= 0) return "—";
            var yil = TotalExperienceMonths / 12;
            var ay = TotalExperienceMonths % 12;
            if (yil == 0) return $"{ay} ay";
            if (ay == 0) return $"{yil} yıl";
            return $"{yil} yıl {ay} ay";
        }
    }
    public string BasHarfler
    {
        get
        {
            var parcalar = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parcalar.Length == 0) return "?";
            if (parcalar.Length == 1) return parcalar[0][..1].ToUpperInvariant();

            return (parcalar[0][..1] + parcalar[^1][..1]).ToUpperInvariant();
        }
    }
}