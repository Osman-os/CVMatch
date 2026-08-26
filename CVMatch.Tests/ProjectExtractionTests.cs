using CVMatch.Web.Models.Extraction;
using CVMatch.Web.Models.ViewModels;
using CVMatch.Web.Services;

namespace CVMatch.Tests;

public class ProjectExtractionTests
{
    [Fact]
    public void Projeler_FormaAktarilir()
    {
        var data = new ExtractedCvData
        {
            Projects =
            {
                new ExtractedProject
                {
                    Name = "Bütçe Takip Uygulaması",
                    Technologies = "React, TypeScript",
                    Url = "github.com/kullanici/butce",
                    Description = "Harcamaları kategoriye göre grafikleştiren uygulama."
                }
            }
        };

        var vm = new CvReviewViewModel();
        ExtractionMapper.Apply(vm, data);

        var proje = Assert.Single(vm.Projects);
        Assert.Equal("Bütçe Takip Uygulaması", proje.Name);
        Assert.Equal("React, TypeScript", proje.Technologies);
        Assert.Equal("github.com/kullanici/butce", proje.Url);
    }

    [Fact]
    public void Projeler_IsDeneyimineKarismaz()
    {
        var data = new ExtractedCvData
        {
            Projects = { new ExtractedProject { Name = "Kütüphane Otomasyonu" } },
            WorkExperiences =
            {
                new ExtractedWorkExperience { CompanyName = "Kodlab Dijital Ajans" }
            }
        };

        var vm = new CvReviewViewModel();
        ExtractionMapper.Apply(vm, data);

        Assert.Single(vm.Projects);
        Assert.Single(vm.WorkExperiences);
        Assert.DoesNotContain(vm.WorkExperiences, w => w.CompanyName == "Kütüphane Otomasyonu");
    }

    [Fact]
    public void BosProjeListesi_HataVermez()
    {
        var data = new ExtractedCvData { Projects = null! };
        data.Normalize();

        Assert.NotNull(data.Projects);
        Assert.Empty(data.Projects);
    }
}