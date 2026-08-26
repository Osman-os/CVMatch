using System.ComponentModel.DataAnnotations;
using CVMatch.Web.Models.ViewModels;

namespace CVMatch.Tests;
public class DateValidationTests
{
    private static List<ValidationResult> Dogrula(WorkExperienceInputModel model)
        => model.Validate(new ValidationContext(model)).ToList();

    [Fact]
    public void TarihsizDeneyim_Gecerli()
    {
        var sonuc = Dogrula(new WorkExperienceInputModel
        {
            CompanyName = "Varoğlu Fotoğrafçılık",
            Position = "Fotoğrafçı"
        });

        Assert.Empty(sonuc);
    }

    [Fact]
    public void BitisTarihiOlmayanDeneyim_Gecerli()
    {
        var sonuc = Dogrula(new WorkExperienceInputModel
        {
            CompanyName = "Nexora Yazılım",
            StartDate = "06/2024"
        });

        Assert.Empty(sonuc);
    }

    [Fact]
    public void BitisBaslangictanOnceyse_Reddedilir()
    {
        var sonuc = Dogrula(new WorkExperienceInputModel
        {
            CompanyName = "Nexora Yazılım",
            StartDate = "06/2024",
            EndDate = "01/2024"
        });

        Assert.NotEmpty(sonuc);
    }
}