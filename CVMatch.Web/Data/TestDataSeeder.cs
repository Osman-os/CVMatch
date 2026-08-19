using CVMatch.Web.Models.Entities;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Data;

/// <summary>
/// Yalnızca geliştirme ortamı için kurgusal aday verisi.
/// Test kayıtları @ornek.test uzantılı e-postayla işaretlenir, kolayca silinebilir.
/// </summary>
public static class TestDataSeeder
{
    private const string TestDomain = "@ornek.test";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await db.CandidateProfiles.AnyAsync(x => x.Email.EndsWith(TestDomain)))
            return;

        var cities = await db.Cities.ToDictionaryAsync(c => c.Name, c => c.Id);
        var skills = await db.Skills.ToDictionaryAsync(s => s.Name, s => s.Id);

        var kayitlar = new[]
        {
            ("Ahmet Yıldırım", "Ankara", 0, EmploymentType.Internship, ApplicationStatus.New,
                new[] { "C#", ".NET", "SQL Server", "Git" }),
            ("Zeynep Korkmaz", "İstanbul", 36, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "JavaScript", "React", "TypeScript", "CSS", "HTML" }),
            ("Burak Şahin", "İzmir", 14, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "C#", "ASP.NET Core", "Entity Framework Core", "SQL Server", "Docker" }),
            ("Elif Aydın", "Samsun", 0, EmploymentType.Internship, ApplicationStatus.New,
                new[] { "Python", "Git", "Linux" }),
            ("Mert Doğan", "Bursa", 60, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "Java", "Spring Boot", "PostgreSQL", "Kubernetes", "REST API" }),
            ("Selin Kaya", "Ankara", 8, EmploymentType.Internship, ApplicationStatus.New,
                new[] { "C#", "SQL Server", "Git", "Unit Testing" }),
            ("Onur Çelik", "İstanbul", 24, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "Node.js", "JavaScript", "MongoDB", "REST API", "Docker" }),
            ("Ayşe Demir", "Antalya", 0, EmploymentType.Internship, ApplicationStatus.New,
                new[] { "HTML", "CSS", "Figma", "UI/UX" }),
            ("Kaan Arslan", "Kocaeli", 18, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "C#", ".NET", "ASP.NET Core", "Azure", "CI/CD" }),
            ("Deniz Yalçın", "İzmir", 6, EmploymentType.Internship, ApplicationStatus.New,
                new[] { "Python", "SQL Server", "Git" }),
            ("Ece Türkmen", "Eskişehir", 30, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "React", "TypeScript", "Tailwind CSS", "Git", "Agile" }),
            ("Emre Polat", "Samsun", 48, EmploymentType.FullTime, ApplicationStatus.New,
                new[] { "C#", "ASP.NET Core", "SQL Server", "Redis", "RabbitMQ", "Docker" })
        };

        var now = DateTime.UtcNow;
        var rastgele = new Random(42);

        foreach (var (ad, sehir, ay, tur, durum, yetenekler) in kayitlar)
        {
            var gunOnce = rastgele.Next(0, 25);
            var telefon = $"05{rastgele.Next(10, 99)} {rastgele.Next(100, 999)} {rastgele.Next(10, 99)} {rastgele.Next(10, 99)}";
            var profile = new CandidateProfile
            {
                ApplicationReferenceNumber = ApplicationHelpers.GenerateReferenceNumber(),
                FullName = ad,
                Email = ad.ToLowerInvariant()
                    .Replace(" ", ".")
                    .Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g")
                    .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c") + TestDomain,
                PhoneNumber = telefon,
                PhoneNormalized = ApplicationHelpers.NormalizePhone(telefon),
                CityId = cities.TryGetValue(sehir, out var cityId) ? cityId : null,
                TotalExperienceMonths = ay,
                PreferredEmploymentType = tur,
                Status = durum,
                EditTokenHash = ApplicationHelpers.HashEditToken(Guid.NewGuid()),
                EditTokenExpiresAt = now.AddDays(30),
                ConsentGivenAt = now.AddDays(-gunOnce),
                SubmittedAt = now.AddDays(-gunOnce)
            };

            foreach (var yetenek in yetenekler)
            {
                if (skills.TryGetValue(yetenek, out var skillId))
                {
                    profile.CandidateSkills.Add(new CandidateSkill { SkillId = skillId });
                }
            }

            db.CandidateProfiles.Add(profile);
        }

        await db.SaveChangesAsync();
    }
}