namespace CVMatch.Web.Models.Entities;

public class JobPostingSkill
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    // Eşleştirme skorunda ağırlık: zorunlu yetenek daha çok puan getirir
    public bool IsRequired { get; set; }
}