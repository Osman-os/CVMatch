namespace CVMatch.Web.Models.ViewModels;

public class CvUploadViewModel
{
    public IFormFile? CvFile { get; set; }
    public string? ErrorMessage { get; set; }
}