namespace CVMatch.Web.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int ToplamBasvuru { get; set; }
    public int YeniBasvuru { get; set; }
    public int SonYediGun { get; set; }
    public int AktifIlan { get; set; }
    public int TaslakIlan { get; set; }
    public int KapaliIlan { get; set; }

    public List<SonBasvuruSatiri> SonBasvurular { get; set; } = new();
}

public class SonBasvuruSatiri
{
    public int Id { get; set; }
    public string ApplicationReferenceNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? CityName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public CVMatch.Web.Models.Enums.ApplicationStatus Status { get; set; }
    public int TotalExperienceMonths { get; set; }

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
}