using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class CvHomeViewModel
{
    public List<JobPostingCardViewModel> Ilanlar { get; set; } = new();
}

public class JobPostingCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CityName { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MinExperienceYears { get; set; }

    public List<string> ZorunluYetenekler { get; set; } = new();
    public List<string> TercihYetenekler { get; set; } = new();

    public string TurMetni =>
        EmploymentType == EmploymentType.Internship ? "Staj" : "Tam Zamanlı";

    public string DeneyimMetni =>
        MinExperienceYears <= 0 ? "Deneyim şartı yok" : $"En az {MinExperienceYears} yıl";
}