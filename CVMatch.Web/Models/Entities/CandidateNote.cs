namespace CVMatch.Web.Models.Entities;

public class CandidateNote
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Content { get; set; } = null!;

    // Notu yazan yönetici
    public string CreatedByUserId { get; set; } = null!;
    public string CreatedByEmail { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}