using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

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

    [HttpGet]
    public async Task<IActionResult> Candidates(
        string? arama,
        int? cityId,
        EmploymentType? employmentType,
        ApplicationStatus? status,
        List<int>? skillIds,
        int sayfa = 1,
        CancellationToken ct = default)
    {
        skillIds ??= new List<int>();
        if (sayfa < 1) sayfa = 1;

        var vm = new AdminCandidateListViewModel
        {
            Arama = arama,
            CityId = cityId,
            EmploymentType = employmentType,
            Status = status,
            SkillIds = skillIds,
            Sayfa = sayfa
        };

        var query = _db.CandidateProfiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var terim = arama.Trim();
            query = query.Where(x =>
                x.FullName.Contains(terim) ||
                x.Email.Contains(terim) ||
                x.ApplicationReferenceNumber.Contains(terim));
        }

        if (cityId.HasValue)
            query = query.Where(x => x.CityId == cityId.Value);

        if (employmentType.HasValue)
            query = query.Where(x => x.PreferredEmploymentType == employmentType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        // Seçilen yeteneklerin HEPSİNE sahip adaylar; fazlası serbest
        foreach (var skillId in skillIds)
        {
            var id = skillId;
            query = query.Where(x => x.CandidateSkills.Any(cs => cs.SkillId == id));
        }

        vm.ToplamKayit = await query.CountAsync(ct);

        vm.Adaylar = await query
            .OrderByDescending(x => x.SubmittedAt)
            .Skip((sayfa - 1) * vm.SayfaBoyutu)
            .Take(vm.SayfaBoyutu)
            .Select(x => new AdaySatiri
            {
                Id = x.Id,
                ApplicationReferenceNumber = x.ApplicationReferenceNumber,
                FullName = x.FullName,
                Email = x.Email,
                CityName = x.City != null ? x.City.Name : null,
                TotalExperienceMonths = x.TotalExperienceMonths,
                PreferredEmploymentType = x.PreferredEmploymentType,
                Status = x.Status,
                SubmittedAt = x.SubmittedAt,
                Skills = x.CandidateSkills
                    .OrderBy(cs => cs.Skill.Name)
                    .Select(cs => cs.Skill.Name)
                    .ToList()
            })
            .ToListAsync(ct);

        vm.Cities = await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == cityId
            })
            .ToListAsync(ct);

        vm.TumYetenekler = await _db.Skills
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SkillSecimi
            {
                Id = s.Id,
                Name = s.Name,
                Secili = skillIds.Contains(s.Id)
            })
            .ToListAsync(ct);

        return View(vm);
    }
}