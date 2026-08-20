using System.Text.Json.Serialization;
using CVMatch.Web.Models.Enums;

namespace CVMatch.Web.Models.Extraction;

public class ExtractedCvData
{
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    // AI doldurmaz — aday kontrol ekranında seçer
    [JsonPropertyName("cityId")]
    public int? CityId { get; set; }

    // AI doldurmaz — aday kontrol ekranında seçer
    [JsonPropertyName("preferredEmploymentType")]
    public EmploymentType? PreferredEmploymentType { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("linkedInUrl")]
    public string? LinkedInUrl { get; set; }

    [JsonPropertyName("gitHubUrl")]
    public string? GitHubUrl { get; set; }

    [JsonPropertyName("totalExperienceMonths")]
    public int? TotalExperienceMonths { get; set; }

    [JsonPropertyName("educations")]
    public List<ExtractedEducation> Educations { get; set; } = new();

    [JsonPropertyName("workExperiences")]
    public List<ExtractedWorkExperience> WorkExperiences { get; set; } = new();

    [JsonPropertyName("skills")]
    public List<string> Skills { get; set; } = new();
    public void Normalize()
    {
        Educations ??= new();
        WorkExperiences ??= new();
        Skills ??= new();

        Educations.RemoveAll(e => e is null);
        WorkExperiences.RemoveAll(w => w is null);
        Skills.RemoveAll(string.IsNullOrWhiteSpace);
    }
}

public class ExtractedEducation
{
    [JsonPropertyName("school")]
    public string? School { get; set; }

    [JsonPropertyName("fieldOfStudy")]
    public string? FieldOfStudy { get; set; }

    // CV'nin dili ne olursa olsun yalnızca şu sabit değerlerden biri gelir:
    // HighSchool, AssociateDegree, BachelorDegree, MasterDegree, Doctorate
    [JsonPropertyName("level")]
    public string? Level { get; set; }

    // "yyyy-MM" biçiminde; ay bilinmiyorsa "yyyy-01"
    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; }
}

public class ExtractedWorkExperience
{
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; }
}