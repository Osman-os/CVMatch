namespace CVMatch.Web.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int ToplamBasvuru { get; set; }
    public int YeniBasvuru { get; set; }
    public int SonYediGun { get; set; }
    public int AktifIlan { get; set; }

    public List<SonBasvuruSatiri> SonBasvurular { get; set; } = new();
}

public class SonBasvuruSatiri
{
    public int Id { get; set; }
    public string ApplicationReferenceNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? CityName { get; set; }
    public DateTime SubmittedAt { get; set; }
}