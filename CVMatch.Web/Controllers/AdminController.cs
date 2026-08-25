using CVMatch.Web.Data;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.ViewModels;
using CVMatch.Web.Models.Entities;
using CVMatch.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CVMatch.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IFileStorage _fileStorage;

    public AdminController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IFileStorage fileStorage)
    {
        _db = db;
        _userManager = userManager;
        _fileStorage = fileStorage;
    }

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

            TaslakIlan = await _db.JobPostings
                .CountAsync(x => x.Status == JobPostingStatus.Draft, ct),

            KapaliIlan = await _db.JobPostings
                .CountAsync(x => x.Status == JobPostingStatus.Closed, ct),

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
                    SubmittedAt = x.SubmittedAt,
                    Status = x.Status,
                    TotalExperienceMonths = x.TotalExperienceMonths
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
        int? minDeneyimYil,
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
            MinDeneyimYil = minDeneyimYil,
            Sayfa = sayfa
        };

        var query = _db.CandidateProfiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var terim = arama.Trim();

            if (terim.Contains('@'))
            {
                query = query.Where(x => x.Email.Contains(terim));
            }
            else if (terim.Length >= 3)
            {
                // 3+ karakterde başvuru numarası da taranır
                var bosluklu = " " + terim;
                query = query.Where(x =>
                    x.FullName.StartsWith(terim) ||
                    x.FullName.Contains(bosluklu) ||
                    x.ApplicationReferenceNumber.Contains(terim));
            }
            else
            {
                // Kısa terimlerde yalnızca ad; numarada tek harf çok fazla eşleşme üretir
                var bosluklu = " " + terim;
                query = query.Where(x =>
                    x.FullName.StartsWith(terim) ||
                    x.FullName.Contains(bosluklu));
            }
        }

        if (cityId.HasValue)
            query = query.Where(x => x.CityId == cityId.Value);

        if (employmentType.HasValue)
            query = query.Where(x => x.PreferredEmploymentType == employmentType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (minDeneyimYil.HasValue)
        {
            var asgariAy = minDeneyimYil.Value * 12;
            query = query.Where(x => x.TotalExperienceMonths >= asgariAy);
        }

        // Seçilen yeteneklerin HEPSİNE sahip adaylar; fazlası serbest
        foreach (var skillId in skillIds)
        {
            var id = skillId;
            query = query.Where(x => x.CandidateSkills.Any(cs => cs.SkillId == id));
        }

        var temsilciIdler = query
            .GroupBy(x => x.Email)
            .Select(g => g.Max(x => x.Id));

        vm.ToplamKayit = await temsilciIdler.CountAsync(ct);

        if (sayfa > vm.ToplamSayfa)
        {
            sayfa = vm.ToplamSayfa;
            vm.Sayfa = sayfa;
        }

        vm.Adaylar = await _db.CandidateProfiles
            .AsNoTracking()
            .Where(x => temsilciIdler.Contains(x.Id))
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
    
        var epostalar = vm.Adaylar.Select(a => a.Email).ToList();

        var basvurular = await query
            .Where(x => epostalar.Contains(x.Email))
            .Select(x => new
            {
                x.Email,
                x.Id,
                x.SubmittedAt,
                x.PreferredEmploymentType,
                Baslik = x.JobPosting != null ? x.JobPosting.Title : null
            })
            .ToListAsync(ct);

        foreach (var aday in vm.Adaylar)
        {
            aday.Basvurular = basvurular
                .Where(b => string.Equals(b.Email, aday.Email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.SubmittedAt)
                .Select(b => new BasvuruOzeti
                {
                    CandidateId = b.Id,
                    IlanBasligi = b.Baslik,
                    Tur = b.PreferredEmploymentType
                })
                .ToList();
        }    

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

        if (cityId.HasValue)
        {
            vm.SecilenSehir = await _db.Cities
                .Where(c => c.Id == cityId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        vm.SecilenYetenekler = vm.TumYetenekler
            .Where(y => y.Secili)
            .Select(y => y.Name)
            .ToList();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Candidate(int id, CancellationToken ct)
    {
        var vm = await _db.CandidateProfiles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminCandidateDetailViewModel
            {
                Id = x.Id,
                ApplicationReferenceNumber = x.ApplicationReferenceNumber,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                CityName = x.City != null ? x.City.Name : null,
                IlanBasligi = x.JobPosting != null ? x.JobPosting.Title : null,
                LinkedInUrl = x.LinkedInUrl,
                GitHubUrl = x.GitHubUrl,
                TotalExperienceMonths = x.TotalExperienceMonths,
                PreferredEmploymentType = x.PreferredEmploymentType,
                Status = x.Status,
                SubmittedAt = x.SubmittedAt,
                UpdatedAt = x.UpdatedAt,
                EditTokenExpiresAt = x.EditTokenExpiresAt,

                Skills = x.CandidateSkills
                    .OrderBy(cs => cs.Skill.Name)
                    .Select(cs => cs.Skill.Name)
                    .ToList(),

                Educations = x.Educations
                    .OrderByDescending(e => e.StartDate)
                    .Select(e => new EgitimSatiri
                    {
                        School = e.School,
                        FieldOfStudy = e.FieldOfStudy,
                        Level = e.Level,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        IsCurrent = e.IsCurrent
                    })
                    .ToList(),

                WorkExperiences = x.WorkExperiences
                    .OrderByDescending(w => w.StartDate)
                    .Select(w => new DeneyimSatiri
                    {
                        CompanyName = w.CompanyName,
                        Position = w.Position,
                        Description = w.Description,
                        StartDate = w.StartDate,
                        EndDate = w.EndDate,
                        IsCurrent = w.IsCurrent
                    })
                    .ToList(),

                Notes = x.Notes
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotSatiri
                    {
                        Id = n.Id,
                        Content = n.Content,
                        AuthorEmail = n.CreatedByEmail,
                        CreatedAt = n.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (vm is null) return NotFound();

        // En son yüklenen CV
        var submission = await _db.CvSubmissions
            .AsNoTracking()
            .Where(s => s.CandidateProfileId == id)
            .OrderByDescending(s => s.UploadedAt)
            .FirstOrDefaultAsync(ct);

        if (submission is not null)
        {
            vm.SubmissionId = submission.Id;
            vm.OriginalFileName = submission.OriginalFileName;
            vm.HasPreview = !string.IsNullOrEmpty(submission.PreviewImageFileName);
            vm.HasPhoto = !string.IsNullOrEmpty(submission.PhotoFileName);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, ApplicationStatus status, CancellationToken ct)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (profile is null) return NotFound();

        profile.Status = status;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = "Başvuru durumu güncellendi.";
        return RedirectToAction(nameof(Candidate), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Hata"] = "Not boş olamaz.";
            return RedirectToAction(nameof(Candidate), new { id });
        }

        if (content.Trim().Length > 2000)
        {
            TempData["Hata"] = "Not en fazla 2000 karakter olabilir.";
            return RedirectToAction(nameof(Candidate), new { id });
        }

        var varMi = await _db.CandidateProfiles.AnyAsync(x => x.Id == id, ct);
        if (!varMi) return NotFound();

        _db.CandidateNotes.Add(new CandidateNote
        {
            CandidateProfileId = id,
            Content = content.Trim(),
            CreatedByUserId = _userManager.GetUserId(User)!,
            CreatedByEmail = User.Identity?.Name ?? "(bilinmiyor)",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = "Not eklendi.";
        return RedirectToAction(nameof(Candidate), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> CandidateFile(int id, string type, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.PhotoFileName })
            .FirstOrDefaultAsync(ct);

        if (profile is null) return NotFound();

        // Vesikalık profilde saklanır, diğerleri son yüklemede
        if (type == "photo")
        {
            if (string.IsNullOrEmpty(profile.PhotoFileName)) return NotFound();
            return await DosyaDondurAsync(
                profile.PhotoFileName,
                MimeTypes.FromFileName(profile.PhotoFileName),
                ct);
        }

        var submission = await _db.CvSubmissions
            .AsNoTracking()
            .Where(s => s.CandidateProfileId == id)
            .OrderByDescending(s => s.UploadedAt)
            .FirstOrDefaultAsync(ct);

        if (submission is null) return NotFound();

        return type switch
        {
            "preview" when !string.IsNullOrEmpty(submission.PreviewImageFileName)
                => await DosyaDondurAsync(submission.PreviewImageFileName, MimeTypes.FromFileName(submission.PreviewImageFileName), ct),

            "pdf" => await DosyaDondurAsync(
                submission.StoredFileName, "application/pdf", ct, submission.OriginalFileName),

            _ => NotFound()
        };
    }

    [HttpGet]
    public async Task<IActionResult> Skills(CancellationToken ct)
    {
        return View(new AdminSkillListViewModel
        {
            Yetenekler = await YetenekleriGetirAsync(ct)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSkill(YeniYetenekInputModel yeni, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Skills), new AdminSkillListViewModel
            {
                Yetenekler = await YetenekleriGetirAsync(ct),
                Yeni = yeni
            });
        }

        var ad = yeni.Name!.Trim();

        if (string.IsNullOrWhiteSpace(ad))
        {
            ModelState.AddModelError("Yeni.Name", "Yetenek adı boş olamaz.");

            return View(nameof(Skills), new AdminSkillListViewModel
            {
                Yetenekler = await YetenekleriGetirAsync(ct),
                Yeni = yeni
            });
        }

        // Büyük/küçük harf farkı yeni kayıt saymaz
        if (await _db.Skills.AnyAsync(x => x.Name.ToLower() == ad.ToLower(), ct))
        {
            ModelState.AddModelError("Yeni.Name", "Bu yetenek zaten kayıtlı.");

            return View(nameof(Skills), new AdminSkillListViewModel
            {
                Yetenekler = await YetenekleriGetirAsync(ct),
                Yeni = yeni
            });
        }

        _db.Skills.Add(new Skill { Name = ad });
        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = $"\"{ad}\" yetenek listesine eklendi.";
        return RedirectToAction(nameof(Skills));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSkill(int id, CancellationToken ct)
    {
        var yetenek = await _db.Skills
            .Include(x => x.CandidateSkills)
            .Include(x => x.JobPostingSkills)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (yetenek is null) return NotFound();

        if (yetenek.CandidateSkills.Count > 0 || yetenek.JobPostingSkills.Count > 0)
        {
            TempData["Hata"] = "Aday veya ilanlarda kullanılan yetenek silinemez.";
            return RedirectToAction(nameof(Skills));
        }

        _db.Skills.Remove(yetenek);
        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = $"\"{yetenek.Name}\" kaldırıldı.";
        return RedirectToAction(nameof(Skills));
    }

    private async Task<List<YetenekSatiri>> YetenekleriGetirAsync(CancellationToken ct)
        => await _db.Skills
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new YetenekSatiri
            {
                Id = x.Id,
                Name = x.Name,
                AdaySayisi = x.CandidateSkills.Count,
                IlanSayisi = x.JobPostingSkills.Count
            })
            .ToListAsync(ct);

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var vm = new AdminUserListViewModel
        {
            Yoneticiler = await YoneticileriGetirAsync(ct)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(YeniYoneticiInputModel yeni, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Users), new AdminUserListViewModel
            {
                Yoneticiler = await YoneticileriGetirAsync(ct),
                Yeni = yeni
            });
        }

        var email = yeni.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(
                "Yeni.Email",
                "Bu e-posta adresi zaten kayıtlı.");

            return View(nameof(Users), new AdminUserListViewModel
            {
                Yoneticiler = await YoneticileriGetirAsync(ct),
                Yeni = yeni
            });
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            // Panelden eklenen yönetici doğrulama e-postası beklemez
            EmailConfirmed = true
        };

        var sonuc = await _userManager.CreateAsync(user, yeni.Password);

        if (!sonuc.Succeeded)
        {
            foreach (var hata in sonuc.Errors)
                ModelState.AddModelError(string.Empty, hata.Description);

            return View(nameof(Users), new AdminUserListViewModel
            {
                Yoneticiler = await YoneticileriGetirAsync(ct),
                Yeni = yeni
            });
        }

        var rolSonucu = await _userManager.AddToRoleAsync(user, DbSeeder.AdminRole);

        if (!rolSonucu.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            foreach (var hata in rolSonucu.Errors)
                ModelState.AddModelError(string.Empty, hata.Description);

            return View(nameof(Users), new AdminUserListViewModel
            {
                Yoneticiler = await YoneticileriGetirAsync(ct),
                Yeni = yeni
            });
        }

        TempData["Bilgi"] = $"{email} yönetici olarak eklendi.";
        return RedirectToAction(nameof(Users));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(string id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (!await _userManager.IsInRoleAsync(user, DbSeeder.AdminRole)) return NotFound();

        // Kendini kaldıramaz
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Hata"] = "Kendi hesabınızı kaldıramazsınız.";
            return RedirectToAction(nameof(Users));
        }

        var yoneticiler = await _userManager.GetUsersInRoleAsync(DbSeeder.AdminRole);

        // Sistemde en az bir yönetici kalmalı
        if (yoneticiler.Count <= 1)
        {
            TempData["Hata"] = "Sistemdeki son yöneticiyi kaldıramazsınız.";
            return RedirectToAction(nameof(Users));
        }

        var sonuc = await _userManager.DeleteAsync(user);

        if (!sonuc.Succeeded)
        {
            TempData["Hata"] = "Yönetici hesabı kaldırılamadı.";
            return RedirectToAction(nameof(Users));
        }

        TempData["Bilgi"] = $"{user.Email} kaldırıldı.";
        return RedirectToAction(nameof(Users));
    }

    private async Task<List<YoneticiSatiri>> YoneticileriGetirAsync(CancellationToken ct)
    {
        var mevcutId = _userManager.GetUserId(User);
        var kullanicilar = await _userManager.GetUsersInRoleAsync(DbSeeder.AdminRole);

        return kullanicilar
            .OrderBy(u => u.Email)
            .Select(u => new YoneticiSatiri
            {
                Id = u.Id,
                Email = u.Email ?? "(e-posta yok)",
                KendisiMi = u.Id == mevcutId
            })
            .ToList();
    }

    private async Task<IActionResult> DosyaDondurAsync(
        string storedFileName,
        string contentType,
        CancellationToken ct,
        string? indirmeAdi = null)
    {
        if (!_fileStorage.Exists(storedFileName))
            return NotFound();

        var stream = await _fileStorage.OpenReadAsync(storedFileName, ct);

        // indirmeAdi verilirse tarayıcı indirme adını bilir, verilmezse gömülü gösterir
        return File(stream, contentType, indirmeAdi);
    }
}