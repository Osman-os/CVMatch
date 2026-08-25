using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
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

    // CV yükleme: her IP saatte 20 dosya
    options.AddPolicy("upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));

    // AI çıkarımı tetikleme: her IP saatte 30 istek
    options.AddPolicy("islem", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));

    // Identity Razor Pages (giriş dahil): kaba kuvvet denemelerine karşı
    options.AddPolicy("giris", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

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

// Örnek aday üretimi kapalı; veri seti elle yüklenen CV'lerden oluşuyor
//if (app.Environment.IsDevelopment())
//    await TestDataSeeder.SeedAsync(app.Services);

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

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Aday üyeliği yok; kayıt sayfası yalnızca yöneticiye açık
app.Use(async (context, next) =>
{
    // Yönetici ekleme kendi panelimizden yapılır; Identity'nin kayıt sayfası kapalı
    if (context.Request.Path.StartsWithSegments("/Identity/Account/Register"))
    {
        context.Response.Redirect("/Admin/Users");
        return;
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
