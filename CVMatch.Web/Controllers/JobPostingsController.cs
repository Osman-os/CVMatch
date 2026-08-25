using CVMatch.Web.Data;
using CVMatch.Web.Models.Entities;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.ViewModels;
using CVMatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class JobPostingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMatchingService _matching;

    public JobPostingsController(ApplicationDbContext db, IMatchingService matching)
    {
        _db = db;
        _matching = matching;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? arama,
        EmploymentType? employmentType,
        JobPostingStatus? status,
        bool kapalilariGoster = false,
        int sayfa = 1,
        CancellationToken ct = default)
    {
        if (sayfa < 1) sayfa = 1;

        var query = _db.JobPostings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var terim = arama.Trim();
            query = query.Where(x => x.Title.Contains(terim));
        }

        if (employmentType.HasValue)
            query = query.Where(x => x.EmploymentType == employmentType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        // Durum filtresi seçilmemişse kapalı ilanlar varsayılan olarak gizlenir
        else if (!kapalilariGoster)
            query = query.Where(x => x.Status != JobPostingStatus.Closed);

        var vm = new JobPostingListViewModel
        {
            Arama = arama,
            EmploymentType = employmentType,
            Status = status,
            KapalilariGoster = kapalilariGoster,
            Sayfa = sayfa
        };

        vm.ToplamKayit = await query.CountAsync(ct);

        // Elle yazılan veya eskimiş bağlantıdaki aşırı sayfa numarası boş liste üretmesin
        if (sayfa > vm.ToplamSayfa)
        {
            sayfa = vm.ToplamSayfa;
            vm.Sayfa = sayfa;
        }

        vm.Ilanlar = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((sayfa - 1) * vm.SayfaBoyutu)
            .Take(vm.SayfaBoyutu)
            .Select(x => new IlanSatiri
            {
                Id = x.Id,
                Title = x.Title,
                CityName = x.City != null ? x.City.Name : null,
                EmploymentType = x.EmploymentType,
                Status = x.Status,
                MinExperienceYears = x.MinExperienceYears,
                SkillCount = x.JobPostingSkills.Count,
                BasvuranSayisi = x.Candidates.Count,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new JobPostingEditViewModel();
        await ListeleriDoldurAsync(vm, ct);
        return View(nameof(Edit), vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var ilan = await _db.JobPostings
            .AsNoTracking()
            .Include(x => x.JobPostingSkills)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (ilan is null) return NotFound();

        var vm = new JobPostingEditViewModel
        {
            Id = ilan.Id,
            Title = ilan.Title,
            EmploymentType = ilan.EmploymentType,
            Description = ilan.Description,
            MinExperienceYears = ilan.MinExperienceYears,
            CityId = ilan.CityId,
            Status = ilan.Status,
            ZorunluSkillIds = ilan.JobPostingSkills.Where(s => s.IsRequired).Select(s => s.SkillId).ToList(),
            TercihSkillIds = ilan.JobPostingSkills.Where(s => !s.IsRequired).Select(s => s.SkillId).ToList()
        };

        await ListeleriDoldurAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(JobPostingEditViewModel vm, CancellationToken ct)
    {
        // Aynı yetenek hem zorunlu hem tercih olamaz
        vm.TercihSkillIds = vm.TercihSkillIds.Except(vm.ZorunluSkillIds).ToList();

        // Elle gönderilen isteklerde tanımsız enum veya olmayan FK gelebilir
        if (!Enum.IsDefined(vm.EmploymentType))
            ModelState.AddModelError(nameof(vm.EmploymentType), "Geçersiz çalışma türü.");

        if (!Enum.IsDefined(vm.Status))
            ModelState.AddModelError(nameof(vm.Status), "Geçersiz ilan durumu.");

        if (vm.CityId.HasValue &&
            !await _db.Cities.AnyAsync(c => c.Id == vm.CityId.Value, ct))
            ModelState.AddModelError(nameof(vm.CityId), "Geçersiz şehir seçimi.");

        var gecerliSkillIds = await _db.Skills.Select(s => s.Id).ToListAsync(ct);

        // Tekrarlanan Id'ler composite key ihlaline yol açar
        vm.ZorunluSkillIds = vm.ZorunluSkillIds
            .Where(gecerliSkillIds.Contains).Distinct().ToList();

        vm.TercihSkillIds = vm.TercihSkillIds
            .Where(gecerliSkillIds.Contains).Distinct().ToList();

        if (!ModelState.IsValid)
        {
            await ListeleriDoldurAsync(vm, ct);
            return View(nameof(Edit), vm);
        }

        JobPosting ilan;

        if (vm.Id == 0)
        {
            ilan = new JobPosting { CreatedAt = DateTime.UtcNow };
            _db.JobPostings.Add(ilan);
        }
        else
        {
            var mevcut = await _db.JobPostings
                .Include(x => x.JobPostingSkills)
                .FirstOrDefaultAsync(x => x.Id == vm.Id, ct);

            if (mevcut is null) return NotFound();

            ilan = mevcut;
            ilan.UpdatedAt = DateTime.UtcNow;

            // Yetenek seçimi baştan kurulur
            _db.JobPostingSkills.RemoveRange(ilan.JobPostingSkills);
            ilan.JobPostingSkills.Clear();
        }

        ilan.Title = vm.Title.Trim();
        ilan.EmploymentType = vm.EmploymentType;
        ilan.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
        ilan.MinExperienceYears = vm.MinExperienceYears;
        ilan.CityId = vm.CityId;
        ilan.Status = vm.Status;

        foreach (var skillId in vm.ZorunluSkillIds)
            ilan.JobPostingSkills.Add(new JobPostingSkill { SkillId = skillId, IsRequired = true });

        foreach (var skillId in vm.TercihSkillIds)
            ilan.JobPostingSkills.Add(new JobPostingSkill { SkillId = skillId, IsRequired = false });

        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = vm.Id == 0 ? "İlan oluşturuldu." : "İlan güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, JobPostingStatus status, CancellationToken ct)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var ilan = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (ilan is null) return NotFound();

        ilan.Status = status;
        ilan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = "İlan durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Match(
        int id, int? asgariSkor, string? turFiltresi, bool? sadeceBasvuranlar,
        CancellationToken ct)
    {
        var vm = await _matching.MatchAsync(
            id, asgariSkor ?? 1, turFiltresi ?? "uyumlu", sadeceBasvuranlar ?? true, ct);
        if (vm is null) return NotFound();

        // Eşleştirme yalnızca yayındaki ilanlar için yapılır
        if (vm.Status != JobPostingStatus.Active)
        {
            TempData["Hata"] =
                $"\"{vm.Title}\" ilanı yayında değil. Eşleştirme yapabilmek için ilanı yayınlayın.";

            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    private async Task ListeleriDoldurAsync(JobPostingEditViewModel vm, CancellationToken ct)
    {
        vm.Cities = await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == vm.CityId
            })
            .ToListAsync(ct);

        vm.TumYetenekler = await _db.Skills
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new YetenekSecenegi
            {
                Id = s.Id,
                Name = s.Name,
                Zorunlu = vm.ZorunluSkillIds.Contains(s.Id),
                Tercih = vm.TercihSkillIds.Contains(s.Id)
            })
            .ToListAsync(ct);
    }
}