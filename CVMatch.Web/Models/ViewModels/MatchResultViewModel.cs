using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class MatchResultViewModel
{
    public int JobPostingId { get; set; }
    public string Title { get; set; } = null!;
    // "tumu" | "uyumlu" | "uyumsuz"
    public string TurFiltresi { get; set; } = "tumu";

    public int TurUyumsuzSayisi { get; set; }
    public int ToplamAday { get; set; }
    public int OrtalamaUyum { get; set; }
    public int YuksekUyumluSayisi { get; set; }
    public int? CityId { get; set; }
    public string? CityName { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public int MinExperienceYears { get; set; }
    public JobPostingStatus Status { get; set; }

    public List<ArananYetenek> Aranan { get; set; } = new();
    public List<EslesenAday> Adaylar { get; set; } = new();

    // Asgari uyum eşiği; altındakiler gizlenir
    public int AsgariSkor { get; set; } = 1;

    public int GizlenenSayisi { get; set; }

    public bool SadeceBasvuranlar { get; set; } = true;
    public int BasvuranSayisi { get; set; }

    public int ZorunluSayisi => Aranan.Count(x => x.Zorunlu);
    public int TercihSayisi => Aranan.Count(x => !x.Zorunlu);

    public bool YetenekTanimliMi => Aranan.Count > 0;
}

public class ArananYetenek
{
    public int SkillId { get; set; }
    public string Name { get; set; } = null!;
    public bool Zorunlu { get; set; }
}

public class EslesenAday
{
    /// <summary>İlanda şehir tercihi yoksa true.</summary>
    public bool SehirKriteriYok { get; set; }
    public int Id { get; set; }
    public string ApplicationReferenceNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string? CityName { get; set; }
    public bool AyniSehir { get; set; }

    public int TotalExperienceMonths { get; set; }
    public bool DeneyimYeterli { get; set; }

    public ApplicationStatus Status { get; set; }

    public int Skor { get; set; }
    public int EksikZorunluSayisi { get; set; }

    public HashSet<int> SahipOlunanSkillIds { get; set; } = new();

    public bool Bilir(int skillId) => SahipOlunanSkillIds.Contains(skillId);
    public bool TurUyumlu { get; set; }

    public List<string> EslesenYetenekler { get; set; } = new();
    public List<string> EksikYetenekler { get; set; } = new();

    public string DeneyimMetni
    {
        get
        {
            if (TotalExperienceMonths <= 0) return "Deneyimsiz";
            var yil = TotalExperienceMonths / 12;
            var ay = TotalExperienceMonths % 12;
            if (yil == 0) return $"{ay} ay";
            if (ay == 0) return $"{yil} yıl";
            return $"{yil} yıl {ay} ay";
        }
    }

    // Skor çubuğunun rengi
    public string SkorRengi => Skor switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-warning",
        _ => "bg-danger"
    };

    public string SkorSinifi => Skor switch
    {
        >= 80 => "cvm-score-high",
        >= 60 => "cvm-score-mid",
        _ => "cvm-score-low"
    };
}