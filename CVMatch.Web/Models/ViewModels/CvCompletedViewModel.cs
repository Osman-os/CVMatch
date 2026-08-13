namespace CVMatch.Web.Models.ViewModels;

public class CvCompletedViewModel
{
    public string ReferenceNumber { get; set; } = null!;

    // Ham token yalnızca burada, bir kez gösterilir
    public string? EditToken { get; set; }

    public DateTime EditTokenExpiresAt { get; set; }
}