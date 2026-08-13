namespace CVMatch.Web.Models.Entities;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    public ICollection<JobPostingSkill> JobPostingSkills { get; set; } = new List<JobPostingSkill>();
}