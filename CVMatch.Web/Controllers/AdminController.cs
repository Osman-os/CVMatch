using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var yediGunOnce = DateTime.UtcNow.AddDays(-7);

        var vm = new AdminDashboardViewModel
        {
            ToplamBasvuru = await _db.CandidateProfiles.CountAsync(ct),

            YeniBasvuru = await _db.CandidateProfiles
                .CountAsync(x => x.Status == ApplicationStatus.New, ct),

            SonYediGun = await _db.CandidateProfiles
                .CountAsync(x => x.SubmittedAt >= yediGunOnce, ct),

            AktifIlan = await _db.JobPostings
                .CountAsync(x => x.Status == JobPostingStatus.Active, ct),

            SonBasvurular = await _db.CandidateProfiles
                .AsNoTracking()
                .OrderByDescending(x => x.SubmittedAt)
                .Take(10)
                .Select(x => new SonBasvuruSatiri
                {
                    Id = x.Id,
                    ApplicationReferenceNumber = x.ApplicationReferenceNumber,
                    FullName = x.FullName,
                    CityName = x.City != null ? x.City.Name : null,
                    SubmittedAt = x.SubmittedAt
                })
                .ToListAsync(ct)
        };

        return View(vm);
    }
}