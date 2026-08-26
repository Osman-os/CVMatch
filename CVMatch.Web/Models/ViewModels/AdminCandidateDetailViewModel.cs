using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class AdminCandidateDetailViewModel
{
    public int Id { get; set; }
    public string ApplicationReferenceNumber { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? CityName { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }

    public int TotalExperienceMonths { get; set; }
    public EmploymentType PreferredEmploymentType { get; set; }
    public string? IlanBasligi { get; set; }
    public ApplicationStatus Status { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime EditTokenExpiresAt { get; set; }

    public List<string> Skills { get; set; } = new();
    public List<EgitimSatiri> Educations { get; set; } = new();
    public List<DeneyimSatiri> WorkExperiences { get; set; } = new();
    public List<ProjeSatiri> Projects { get; set; } = new();
    public List<NotSatiri> Notes { get; set; } = new();

    // En son yüklenen CV
    public int? SubmissionId { get; set; }
    public string? OriginalFileName { get; set; }
    public bool HasPreview { get; set; }
    public bool HasPhoto { get; set; }

    public bool EditTokenGecerliMi => EditTokenExpiresAt > DateTime.UtcNow;

    public string DeneyimMetni
    {
        get
        {
            if (TotalExperienceMonths <= 0) return "Belirtilmedi";
            var yil = TotalExperienceMonths / 12;
            var ay = TotalExperienceMonths % 12;
            if (yil == 0) return $"{ay} ay";
            if (ay == 0) return $"{yil} yıl";
            return $"{yil} yıl {ay} ay";
        }
    }
}

public class EgitimSatiri
{
    public string School { get; set; } = null!;
    public string? FieldOfStudy { get; set; }
    public EducationLevel? Level { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class DeneyimSatiri
{
    public string CompanyName { get; set; } = null!;
    public string? Position { get; set; }
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class ProjeSatiri
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Technologies { get; set; }
    public string? Url { get; set; }
}

public class NotSatiri
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public string? AuthorEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}