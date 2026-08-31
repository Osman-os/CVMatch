using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CVMatch.Web.Data;
using CVMatch.Web.Services;
using System.Threading.RateLimiting;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);

var kultur = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 8)),
        mysql =>
        {
            // Birden fazla koleksiyon birleştiğinde satır çarpımını önler
            mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

            // Uzak sunucuya bağlantı ara sıra kopabiliyor
            mysql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        }));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Tokens.AuthenticatorIssuer = "Yesilmavi IK";
});
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CVMatch.Web.Services.IEmailSender, CVMatch.Web.Services.SmtpEmailSender>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    CVMatch.Web.Services.IdentityEmailSender>();
builder.Services.AddRazorPages(options =>
{
    // Aday üyeliği yok; kayıt sayfası yalnızca yöneticiye açık
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Register", "AdminOnly");
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/RegisterConfirmation", "AdminOnly");
});
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddSingleton<IPdfPhotoExtractor, PdfPigPhotoExtractor>();

builder.Services.AddHttpClient<ICvExtractionService, ClaudeCvExtractionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

#pragma warning disable CA1416 // Uygulama Windows üzerinde çalışıyor
builder.Services.AddScoped<ICvProcessingService, CvProcessingService>();
#pragma warning restore CA1416

builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddHostedService<DraftCleanupService>();

// ---------- İstek sınırlama ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Sınırlar appsettings üzerinden ayarlanabilir; değer yoksa varsayılan kullanılır
    int Sinir(string ad, int varsayilan) =>
        int.TryParse(builder.Configuration[$"RateLimit:{ad}:PermitLimit"], out var v) && v > 0
            ? v
            : varsayilan;

    int Pencere(string ad, int varsayilan) =>
        int.TryParse(builder.Configuration[$"RateLimit:{ad}:WindowMinutes"], out var v) && v > 0
            ? v
            : varsayilan;

    void PolitikaEkle(string ad, int varsayilanSinir, int varsayilanPencere)
    {
        var sinir = Sinir(ad, varsayilanSinir);
        var pencere = Pencere(ad, varsayilanPencere);

        options.AddPolicy(ad, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = sinir,
                    Window = TimeSpan.FromMinutes(pencere),
                    QueueLimit = 0
                }));
    }

    // CV yükleme: her IP saatte 20 dosya
    PolitikaEkle("upload", 20, 60);

    // AI çıkarımı tetikleme: her IP saatte 30 istek
    PolitikaEkle("islem", 30, 60);

    // Identity Razor Pages (giriş dahil): kaba kuvvet denemelerine karşı
    PolitikaEkle("giris", 20, 15);

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";

        await context.HttpContext.Response.WriteAsync(
            "<h3>Çok fazla istek gönderdiniz</h3>" +
            "<p>Lütfen bir süre bekleyip yeniden deneyin.</p>", ct);
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(DbSeeder.AdminRole));
});


var app = builder.Build();

// Depolama klasörünü açılışta hazırla ve yapılandırmayı doğrula
app.Services.GetRequiredService<IFileStorage>();

await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

// Hız sınırı yalnızca kimlik doğrulama ve CV işleme uç noktalarında uygulanır;
// panelde gezinmek veya 2FA kurmak sayaca girmez
var sinirliYollar = new[]
{
    "/Identity/Account/Login",
    "/Identity/Account/LoginWith2fa",
    "/Identity/Account/LoginWithRecoveryCode",
    "/Identity/Account/ForgotPassword",
    "/Cv/Upload",
    "/Cv/Start"
};

app.UseWhen(
    ctx => sinirliYollar.Any(y => ctx.Request.Path.StartsWithSegments(y)),
    dal => dal.UseRateLimiter());

app.UseAuthentication();
app.UseAuthorization();

var zorunlu2faMuafYollar = new[]
{
    "/Identity/Account/Manage/EnableAuthenticator",
    "/Identity/Account/Manage/TwoFactorAuthentication",
    "/Identity/Account/Manage/ShowRecoveryCodes",
    "/Identity/Account/Manage/GenerateRecoveryCodes",
    "/Identity/Account/Logout",
    "/Identity/Account/Login"
};

app.Use(async (context, next) =>
{
    var yol = context.Request.Path;

    var korumaliAlan =
        yol.StartsWithSegments("/Admin") ||
        yol.StartsWithSegments("/JobPostings");

    if (korumaliAlan
        && context.User.Identity?.IsAuthenticated == true
        && !zorunlu2faMuafYollar.Any(m => yol.StartsWithSegments(m)))
    {
        var userManager = context.RequestServices
            .GetRequiredService<UserManager<IdentityUser>>();

        var kullanici = await userManager.GetUserAsync(context.User);

        if (kullanici is not null && !await userManager.GetTwoFactorEnabledAsync(kullanici))
        {
            context.Response.Redirect("/Identity/Account/Manage/EnableAuthenticator");
            return;
        }
    }

    await next();
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/yonetim", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect(
            context.User.Identity?.IsAuthenticated == true
                ? "/Admin"
                : "/Identity/Account/Login");
        return;
    }

    await next();
});

var kapaliIdentitySayfalari = new[]
{
    "/Identity/Account/Register",
    "/Identity/Account/RegisterConfirmation",
    "/Identity/Account/ConfirmEmail",
    "/Identity/Account/ConfirmEmailChange",
    "/Identity/Account/ResendEmailConfirmation",
    "/Identity/Account/ExternalLogin"
};

var acikManageSayfalari = new[]
{
    "/Identity/Account/Manage/TwoFactorAuthentication",
    "/Identity/Account/Manage/EnableAuthenticator",
    "/Identity/Account/Manage/ResetAuthenticator",
    "/Identity/Account/Manage/GenerateRecoveryCodes",
    "/Identity/Account/Manage/ShowRecoveryCodes",
    "/Identity/Account/Manage/Disable2fa"
};

app.Use(async (context, next) =>
{
    var yol = context.Request.Path;

    var acik = acikManageSayfalari.Any(a => yol.StartsWithSegments(a));

    if (!acik)
    {
        var kapali = kapaliIdentitySayfalari.Any(k => yol.StartsWithSegments(k))
            || yol.StartsWithSegments("/Identity/Account/Manage");

        if (kapali)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }

    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cv}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().RequireRateLimiting("giris");

app.Run();
