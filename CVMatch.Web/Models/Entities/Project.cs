namespace CVMatch.Web.Models.Entities;

public class Project
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public string? Technologies { get; set; }

    public string? Url { get; set; }
}