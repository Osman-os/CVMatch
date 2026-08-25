using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.ViewModels;

public class CvUploadViewModel
{
    public IFormFile? CvFile { get; set; }
    public string? ErrorMessage { get; set; }
    public int JobPostingId { get; set; }

    // Yalnızca ekranda göstermek için; POST'tan gelen değere güvenilmez
    public string IlanBasligi { get; set; } = null!;
    public string? IlanSehri { get; set; }
    public EmploymentType IlanTuru { get; set; }
}