namespace CVMatch.Web.Models.Entities;

public class CandidateSkill
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    // AI'dan mı geldi, aday elle mi girdi?
    public bool IsAiExtracted { get; set; }
}