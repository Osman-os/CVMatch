using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.Entities;

public class CvSubmission
{
    public DateTime? ReviewedAt { get; set; }
    public int Id { get; set; }

    // Aday onaylayana kadar null
    public int? CandidateProfileId { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }

    // Adayın inceleme ekranına dönebilmesi için URL'de taşınan anahtar
    public Guid Token { get; set; }

    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string? PreviewImageFileName { get; set; }
    public string? PhotoFileName { get; set; }

    public string? ExtractedText { get; set; }

    // AI'dan dönen yapılandırılmış taslak — onaya kadar burada durur
    public string? ExtractedJson { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Uploaded;
    public string? ErrorMessage { get; set; }

    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Onaylanmayan taslakların temizlenmesi için
    public DateTime ExpiresAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}