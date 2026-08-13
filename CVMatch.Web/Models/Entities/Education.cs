using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.Entities;

public class Education
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string School { get; set; } = null!;
    public string? FieldOfStudy { get; set; }
    public EducationLevel? Level { get; set; }

    // Ayın 1'i sabitlenir; ekranda "Eylül 2022" olarak gösterilir
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}