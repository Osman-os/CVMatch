namespace CVMatch.Web.Models.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<CandidateProfile> Candidates { get; set; } = new List<CandidateProfile>();
    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}