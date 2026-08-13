namespace CVMatch.Web.Models.Entities;

public class WorkExperience
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string CompanyName { get; set; } = null!;
    public string? Position { get; set; }
    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}