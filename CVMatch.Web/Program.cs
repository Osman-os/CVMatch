using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CVMatch.Web.Data;
using CVMatch.Web.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cv}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
