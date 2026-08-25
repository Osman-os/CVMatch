using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.Entities;

public class JobPosting
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public string? Description { get; set; }

    public int MinExperienceYears { get; set; }

    public int? CityId { get; set; }
    public City? City { get; set; }

    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<JobPostingSkill> JobPostingSkills { get; set; } = new List<JobPostingSkill>();
    public ICollection<CvSubmission> CvSubmissions { get; set; } = new List<CvSubmission>();
    public ICollection<CandidateProfile> Candidates { get; set; } = new List<CandidateProfile>();
}