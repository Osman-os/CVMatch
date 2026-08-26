using System.Globalization;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.Extraction;
using CVMatch.Web.Models.ViewModels;

namespace CVMatch.Web.Services;

public static class ExtractionMapper
{
    public static void Apply(CvReviewViewModel vm, ExtractedCvData data)
    {
        vm.FullName = data.FullName;
        vm.Email = data.Email;
        vm.PhoneNumber = data.PhoneNumber;
        vm.Address = data.Address;
        vm.LinkedInUrl = data.LinkedInUrl;
        vm.GitHubUrl = data.GitHubUrl;

        if (data.TotalExperienceMonths is int months && months > 0)
        {
            vm.ExperienceYears = months / 12;
            vm.ExperienceMonths = months % 12;
        }

        vm.Educations = data.Educations.Select(e => new EducationInputModel
        {
            School = e.School,
            FieldOfStudy = e.FieldOfStudy,
            Level = ParseLevel(e.Level),
            StartDate = ToDisplayDate(e.StartDate),
            EndDate = e.IsCurrent ? null : ToDisplayDate(e.EndDate),
            IsCurrent = e.IsCurrent
        }).ToList();

        vm.WorkExperiences = data.WorkExperiences.Select(w => new WorkExperienceInputModel
        {
            CompanyName = w.CompanyName,
            Position = w.Position,
            Description = w.Description,
            StartDate = ToDisplayDate(w.StartDate),
            EndDate = w.IsCurrent ? null : ToDisplayDate(w.EndDate),
            IsCurrent = w.IsCurrent
        }).ToList();

        vm.Projects = data.Projects.Select(p => new ProjectInputModel
        {
            Name = p.Name,
            Description = p.Description,
            Technologies = p.Technologies,
            Url = p.Url
        }).ToList();
        
        vm.UncertainFields = data.UncertainFields.ToList();

        vm.SkillsCsv = string.Join(",", data.Skills.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    // Şehir adını Cities tablosundaki kayıtla eşler
    public static int? MatchCity(string? cityName, IEnumerable<CityOption> cities)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var match = cities.FirstOrDefault(c =>
            string.Equals(c.Name, cityName.Trim(),
                StringComparison.InvariantCultureIgnoreCase));

        return match?.Id;
    }

    private static EducationLevel? ParseLevel(string? raw)
        => Enum.TryParse<EducationLevel>(raw, ignoreCase: true, out var level)
            ? level
            : null;

    // "2019-06" → "06/2019"
    private static string? ToDisplayDate(string? isoLike)
    {
        if (string.IsNullOrWhiteSpace(isoLike)) return null;

        return DateOnly.TryParseExact(
            isoLike + "-01", "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("MM/yyyy", CultureInfo.InvariantCulture)
            : null;
    }

    // "06/2019" → DateOnly(2019, 6, 1)
    public static DateOnly? ParseDisplayDate(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return null;

        return DateOnly.TryParseExact(
            "01/" + display.Trim(), "dd/MM/yyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}