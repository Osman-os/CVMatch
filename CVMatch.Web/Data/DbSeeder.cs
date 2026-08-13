using CVMatch.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        await SeedCitiesAsync(db);
        await SeedSkillsAsync(db);
        await SeedAdminAsync(roleManager, userManager, config);
    }

    private static async Task SeedCitiesAsync(ApplicationDbContext db)
    {
        if (await db.Cities.AnyAsync()) return;

        var names = new[]
        {
            "Adana","Adıyaman","Afyonkarahisar","Ağrı","Aksaray","Amasya","Ankara","Antalya",
            "Ardahan","Artvin","Aydın","Balıkesir","Bartın","Batman","Bayburt","Bilecik",
            "Bingöl","Bitlis","Bolu","Burdur","Bursa","Çanakkale","Çankırı","Çorum",
            "Denizli","Diyarbakır","Düzce","Edirne","Elazığ","Erzincan","Erzurum","Eskişehir",
            "Gaziantep","Giresun","Gümüşhane","Hakkari","Hatay","Iğdır","Isparta","İstanbul",
            "İzmir","Kahramanmaraş","Karabük","Karaman","Kars","Kastamonu","Kayseri","Kilis",
            "Kırıkkale","Kırklareli","Kırşehir","Kocaeli","Konya","Kütahya","Malatya","Manisa",
            "Mardin","Mersin","Muğla","Muş","Nevşehir","Niğde","Ordu","Osmaniye",
            "Rize","Sakarya","Samsun","Şanlıurfa","Siirt","Sinop","Sivas","Şırnak",
            "Tekirdağ","Tokat","Trabzon","Tunceli","Uşak","Van","Yalova","Yozgat","Zonguldak"
        };

        db.Cities.AddRange(names.Select(n => new City { Name = n }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedSkillsAsync(ApplicationDbContext db)
    {
        if (await db.Skills.AnyAsync()) return;

        var names = new[]
        {
            "C#", ".NET", "ASP.NET Core", "Entity Framework Core", "SQL Server", "PostgreSQL",
            "MongoDB", "JavaScript", "TypeScript", "React", "Angular", "Vue.js",
            "HTML", "CSS", "Bootstrap", "Tailwind CSS", "Node.js", "Python",
            "Java", "Spring Boot", "PHP", "Laravel", "Go", "Rust",
            "Docker", "Kubernetes", "Git", "CI/CD", "Azure", "AWS",
            "REST API", "GraphQL", "Redis", "RabbitMQ", "Linux", "Unit Testing",
            "Agile", "Scrum", "Figma", "UI/UX"
        };

        db.Skills.AddRange(names.Select(n => new Skill { Name = n }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IConfiguration config)
    {
        if (!await roleManager.RoleExistsAsync(AdminRole))
            await roleManager.CreateAsync(new IdentityRole(AdminRole));

        var email = config["SeedAdmin:Email"];
        var password = config["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, AdminRole);
    }
}