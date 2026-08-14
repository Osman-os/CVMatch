namespace CVMatch.Web.Models.ViewModels;

public class CvEditViewModel
{
    // URL'deki ham anahtar; formda gizli alan olarak taşınır
    public string Key { get; set; } = null!;

    public string ApplicationReferenceNumber { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public DateTime EditTokenExpiresAt { get; set; }

    public CvReviewViewModel Data { get; set; } = new();
}