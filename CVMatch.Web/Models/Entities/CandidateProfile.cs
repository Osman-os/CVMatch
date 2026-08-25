using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.Entities;

public class CandidateProfile
{
    public int Id { get; set; }

    // Adaya gösterilen, gizli olmayan numara. Örn: CVM-2026-7K4P9Q
    public string ApplicationReferenceNumber { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }

    /// <summary>Mükerrer başvuru kontrolü için normalleştirilmiş telefon.</summary>
    public string? PhoneNormalized { get; set; }
    public string? Address { get; set; }
    
    // CV'den çıkarılan vesikalık; bulunamazsa null
    public string? PhotoFileName { get; set; }

    public int? CityId { get; set; }
    public City? City { get; set; }

    // Onay anında başvurudan kopyalanır
    public int? JobPostingId { get; set; }
    public JobPosting? JobPosting { get; set; }

    // Ay bazında saklanır, ekranda "1 yıl 2 ay" olarak gösterilir
    public int TotalExperienceMonths { get; set; }

    // Aday hangi tür pozisyon arıyor
    public EmploymentType PreferredEmploymentType { get; set; } = EmploymentType.Internship;

    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;

    // Ham token asla saklanmaz; adaya bir kez gösterilir
    public string EditTokenHash { get; set; } = null!;
    public DateTime EditTokenExpiresAt { get; set; }

    // KVKK açık rızası
    public DateTime ConsentGivenAt { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public ICollection<CvSubmission> CvSubmissions { get; set; } = new List<CvSubmission>();
    public ICollection<CandidateNote> Notes { get; set; } = new List<CandidateNote>();
}