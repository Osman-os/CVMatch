using CVMatch.Web.Data;
using CVMatch.Web.Models.Entities;
using CVMatch.Web.Models.Enums;
using CVMatch.Web.Models.ViewModels;
using CVMatch.Web.Services;
using System.Text.Json;
using CVMatch.Web.Models.Extraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Controllers;

public class CvController : Controller
{
    // Onaylanmayan taslaklar bu süre sonunda temizlenir
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromHours(24);
    
    // Aday bu süre boyunca başvurusunu düzenleyebilir
    private static readonly TimeSpan EditTokenLifetime = TimeSpan.FromDays(30);

    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICvProcessingService _processing;
    private readonly ILogger<CvController> _logger;

    public CvController(
        ApplicationDbContext db,
        IFileStorage storage,
        ICvProcessingService processing,
        ILogger<CvController> logger)
    {
        _db = db;
        _storage = storage;
        _processing = processing;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
        => View(new CvUploadViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(FileValidation.MaxPdfBytes + 1024)]
    public async Task<IActionResult> Upload(CvUploadViewModel model, CancellationToken ct)
    {
        var validation = FileValidation.ValidatePdf(model.CvFile);
        if (!validation.IsValid)
        {
            model.ErrorMessage = validation.ErrorMessage;
            return View(nameof(Index), model);
        }

        var file = model.CvFile!;

        string storedFileName;
        await using (var stream = file.OpenReadStream())
        {
            storedFileName = await _storage.SaveAsync(stream, ".pdf", ct);
        }

        var submission = new CvSubmission
        {
            Token = Guid.NewGuid(),
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            FileSizeBytes = file.Length,
            Status = SubmissionStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(DraftLifetime)
        };

        _db.CvSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("CV yüklendi. Token: {Token}", submission.Token);

        return RedirectToAction(nameof(Processing), new { token = submission.Token });
    }

    [HttpGet]
    public async Task<IActionResult> Processing(Guid token, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (submission is null)
            return NotFound();

        // İlk kez geliniyorsa işlemi başlat
        if (submission.Status == SubmissionStatus.Uploaded)
        {
            await _processing.ProcessAsync(submission.Id, ct);

            submission = await _db.CvSubmissions
                .AsNoTracking()
                .FirstAsync(x => x.Token == token, ct);
        }

        return submission.Status switch
        {
            SubmissionStatus.AwaitingReview =>
                RedirectToAction(nameof(Review), new { token }),

            SubmissionStatus.Failed =>
                View("ProcessingFailed", submission),

            _ => View(submission)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Review(Guid token, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (submission is null) return NotFound();

        if (submission.Status is not (SubmissionStatus.AwaitingReview or SubmissionStatus.Failed))
            return RedirectToAction(nameof(Processing), new { token });

        var vm = new CvReviewViewModel
        {
            Token = token,
            PreviewImageFileName = submission.PreviewImageFileName,
            PhotoFileName = submission.PhotoFileName,
            Cities = await GetCitiesAsync(ct)
        };

        if (!string.IsNullOrWhiteSpace(submission.ExtractedJson))
        {
            try
            {
                var data = JsonSerializer.Deserialize<ExtractedCvData>(submission.ExtractedJson);
                if (data is not null)
                {
                    ExtractionMapper.Apply(vm, data);

                    // Aday daha önce seçtiyse onu kullan, yoksa AI'ın tahminini eşleştir
                    vm.CityId = data.CityId ?? ExtractionMapper.MatchCity(data.City, vm.Cities);
                    vm.PreferredEmploymentType = data.PreferredEmploymentType;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Taslak JSON çözümlenemedi: {Token}", token);
            }
        }
        else
        {
            vm.IsManualEntry = true;
        }

        // En az birer boş satır göster
        if (vm.Educations.Count == 0)
            vm.Educations.Add(new EducationInputModel());

        if (vm.WorkExperiences.Count == 0)
            vm.WorkExperiences.Add(new WorkExperienceInputModel());

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(CvReviewViewModel model, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .FirstOrDefaultAsync(x => x.Token == model.Token, ct);

        if (submission is null) return NotFound();

        // Boş satırları at
        model.Educations = model.Educations
            .Where(e => !string.IsNullOrWhiteSpace(e.School))
            .ToList();

        model.WorkExperiences = model.WorkExperiences
            .Where(w => !string.IsNullOrWhiteSpace(w.CompanyName))
            .ToList();

        if (!ModelState.IsValid)
        {
            model.Cities = await GetCitiesAsync(ct);
            model.PreviewImageFileName = submission.PreviewImageFileName;
            model.PhotoFileName = submission.PhotoFileName;

            if (model.Educations.Count == 0)
                model.Educations.Add(new EducationInputModel());

            if (model.WorkExperiences.Count == 0)
                model.WorkExperiences.Add(new WorkExperienceInputModel());

            return View(model);
        }

        // Adayın düzenlediği hâli taslağa geri yaz
        submission.ExtractedJson = JsonSerializer.Serialize(ToExtractedData(model));
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Summary), new { token = model.Token });
    }

    [HttpGet]
    public async Task<IActionResult> Summary(Guid token, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (submission is null) return NotFound();

        // Onay verilmemişse buraya gelinmemeli
        if (submission.Status is not (SubmissionStatus.AwaitingReview or SubmissionStatus.Failed))
            return RedirectToAction(nameof(Processing), new { token });

        if (string.IsNullOrWhiteSpace(submission.ExtractedJson))
            return RedirectToAction(nameof(Review), new { token });

        ExtractedCvData? data;
        try
        {
            data = JsonSerializer.Deserialize<ExtractedCvData>(submission.ExtractedJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Özet için taslak JSON çözümlenemedi: {Token}", token);
            return RedirectToAction(nameof(Review), new { token });
        }

        if (data is null) return RedirectToAction(nameof(Review), new { token });

        var vm = new CvSummaryViewModel
        {
            Token = token,
            OriginalFileName = submission.OriginalFileName,
            HasPreview = !string.IsNullOrEmpty(submission.PreviewImageFileName),
            Skills = data.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            Consent = new CvConsentInputModel { Token = token }
        };

        ExtractionMapper.Apply(vm.Data, data);
        vm.Data.CityId = data.CityId;
        vm.Data.PreferredEmploymentType = data.PreferredEmploymentType;

        if (data.CityId is int cityId)
        {
            vm.CityName = await _db.Cities
                .AsNoTracking()
                .Where(c => c.Id == cityId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CvConsentInputModel consent, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .FirstOrDefaultAsync(x => x.Token == consent.Token, ct);

        if (submission is null) return NotFound();

        if (submission.Status == SubmissionStatus.Approved)
            return RedirectToAction(nameof(Summary), new { token = consent.Token });

        // Onay kutuları eksikse özet ekranını hatalarla tekrar göster
        if (!ModelState.IsValid)
            return await BuildSummaryViewAsync(submission, consent, ct);

        if (string.IsNullOrWhiteSpace(submission.ExtractedJson))
            return RedirectToAction(nameof(Review), new { token = consent.Token });

        var data = JsonSerializer.Deserialize<ExtractedCvData>(submission.ExtractedJson);
        if (data is null)
            return RedirectToAction(nameof(Review), new { token = consent.Token });

        var now = DateTime.UtcNow;
        var editToken = ApplicationHelpers.GenerateEditToken();

        var profile = new CandidateProfile
        {
            ApplicationReferenceNumber = await GenerateUniqueReferenceAsync(ct),
            FullName = data.FullName ?? "(belirtilmedi)",
            Email = data.Email ?? "(belirtilmedi)",
            PhoneNumber = data.PhoneNumber,
            Address = data.Address,
            PhotoFileName = submission.PhotoFileName,
            CityId = data.CityId,
            TotalExperienceMonths = data.TotalExperienceMonths ?? 0,
            PreferredEmploymentType = data.PreferredEmploymentType ?? EmploymentType.Internship,
            LinkedInUrl = data.LinkedInUrl,
            GitHubUrl = data.GitHubUrl,
            Status = ApplicationStatus.New,
            EditTokenHash = ApplicationHelpers.HashEditToken(editToken),
            EditTokenExpiresAt = now.Add(EditTokenLifetime),
            ConsentGivenAt = now,
            SubmittedAt = now
        };

        foreach (var e in data.Educations.Where(x => !string.IsNullOrWhiteSpace(x.School)))
        {
            profile.Educations.Add(new Education
            {
                School = e.School!,
                FieldOfStudy = e.FieldOfStudy,
                Level = Enum.TryParse<EducationLevel>(e.Level, true, out var lvl) ? lvl : null,
                StartDate = ParseIsoLike(e.StartDate),
                EndDate = e.IsCurrent ? null : ParseIsoLike(e.EndDate),
                IsCurrent = e.IsCurrent
            });
        }

        foreach (var w in data.WorkExperiences.Where(x => !string.IsNullOrWhiteSpace(x.CompanyName)))
        {
            profile.WorkExperiences.Add(new WorkExperience
            {
                CompanyName = w.CompanyName!,
                Position = w.Position,
                Description = w.Description,
                StartDate = ParseIsoLike(w.StartDate),
                EndDate = w.IsCurrent ? null : ParseIsoLike(w.EndDate),
                IsCurrent = w.IsCurrent
            });
        }

        await AttachSkillsAsync(profile, data.Skills, ct);

        _db.CandidateProfiles.Add(profile);

        submission.CandidateProfile = profile;
        submission.Status = SubmissionStatus.Approved;
        submission.ApprovedAt = now;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Başvuru tamamlandı. Referans: {Reference}",
            profile.ApplicationReferenceNumber);

        // Ham token yalnızca burada, bir kez gösterilir
        return View(nameof(Completed), new CvCompletedViewModel
        {
            ReferenceNumber = profile.ApplicationReferenceNumber,
            EditToken = editToken.ToString(),
            EditTokenExpiresAt = profile.EditTokenExpiresAt
        });
    }

    [HttpGet]
    public IActionResult Completed()
        => RedirectToAction(nameof(Index));

    [HttpGet]
    public async Task<IActionResult> Edit(string key, CancellationToken ct)
    {
        var profile = await BulEditProfiliAsync(key, ct);
        if (profile is null) return View("EditUnavailable");

        var vm = new CvEditViewModel
        {
            Key = key,
            ApplicationReferenceNumber = profile.ApplicationReferenceNumber,
            SubmittedAt = profile.SubmittedAt,
            EditTokenExpiresAt = profile.EditTokenExpiresAt,
            Data = new CvReviewViewModel
            {
                FullName = profile.FullName,
                Email = profile.Email,
                PhoneNumber = profile.PhoneNumber,
                Address = profile.Address,
                CityId = profile.CityId,
                LinkedInUrl = profile.LinkedInUrl,
                GitHubUrl = profile.GitHubUrl,
                PreferredEmploymentType = profile.PreferredEmploymentType,
                ExperienceYears = profile.TotalExperienceMonths / 12,
                ExperienceMonths = profile.TotalExperienceMonths % 12,
                SkillsCsv = string.Join(", ", profile.CandidateSkills
                    .OrderBy(cs => cs.Skill.Name)
                    .Select(cs => cs.Skill.Name)),
                Cities = await GetCitiesAsync(ct),
                Educations = profile.Educations
                    .OrderByDescending(e => e.StartDate)
                    .Select(e => new EducationInputModel
                    {
                        School = e.School,
                        FieldOfStudy = e.FieldOfStudy,
                        Level = e.Level,
                        StartDate = e.StartDate?.ToString("MM/yyyy"),
                        EndDate = e.EndDate?.ToString("MM/yyyy"),
                        IsCurrent = e.IsCurrent
                    })
                    .ToList(),
                WorkExperiences = profile.WorkExperiences
                    .OrderByDescending(w => w.StartDate)
                    .Select(w => new WorkExperienceInputModel
                    {
                        CompanyName = w.CompanyName,
                        Position = w.Position,
                        Description = w.Description,
                        StartDate = w.StartDate?.ToString("MM/yyyy"),
                        EndDate = w.EndDate?.ToString("MM/yyyy"),
                        IsCurrent = w.IsCurrent
                    })
                    .ToList()
            }
        };

        if (vm.Data.Educations.Count == 0)
            vm.Data.Educations.Add(new EducationInputModel());

        if (vm.Data.WorkExperiences.Count == 0)
            vm.Data.WorkExperiences.Add(new WorkExperienceInputModel());

        return View(vm);
    }

    /// <summary>
    /// Ham anahtarın özetini hesaplayıp süresi dolmamış profili bulur.
    /// </summary>
    private async Task<CandidateProfile?> BulEditProfiliAsync(string? key, CancellationToken ct)
    {
        var hash = ApplicationHelpers.TryHashEditKey(key);
        if (hash is null) return null;

        return await _db.CandidateProfiles
            .Include(x => x.Educations)
            .Include(x => x.WorkExperiences)
            .Include(x => x.CandidateSkills)
                .ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(
                x => x.EditTokenHash == hash && x.EditTokenExpiresAt > DateTime.UtcNow, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string key, CvReviewViewModel data, CancellationToken ct)
    {
        var profile = await BulEditProfiliAsync(key, ct);
        if (profile is null) return View("EditUnavailable");

        if (!ModelState.IsValid)
        {
            data.Cities = await GetCitiesAsync(ct);

            if (data.Educations.Count == 0)
                data.Educations.Add(new EducationInputModel());

            if (data.WorkExperiences.Count == 0)
                data.WorkExperiences.Add(new WorkExperienceInputModel());

            return View(new CvEditViewModel
            {
                Key = key,
                ApplicationReferenceNumber = profile.ApplicationReferenceNumber,
                SubmittedAt = profile.SubmittedAt,
                EditTokenExpiresAt = profile.EditTokenExpiresAt,
                Data = data
            });
        }

        profile.FullName = data.FullName!.Trim();
        profile.Email = data.Email!.Trim();
        profile.PhoneNumber = data.PhoneNumber?.Trim();
        profile.Address = string.IsNullOrWhiteSpace(data.Address) ? null : data.Address.Trim();
        profile.CityId = data.CityId;
        profile.LinkedInUrl = string.IsNullOrWhiteSpace(data.LinkedInUrl) ? null : data.LinkedInUrl.Trim();
        profile.GitHubUrl = string.IsNullOrWhiteSpace(data.GitHubUrl) ? null : data.GitHubUrl.Trim();
        profile.PreferredEmploymentType = data.PreferredEmploymentType!.Value;
        profile.TotalExperienceMonths = data.TotalExperienceMonths;
        profile.UpdatedAt = DateTime.UtcNow;

        // Alt kayıtlar baştan kurulur
        _db.Educations.RemoveRange(profile.Educations);
        profile.Educations.Clear();

        foreach (var e in data.Educations.Where(x => !string.IsNullOrWhiteSpace(x.School)))
        {
            profile.Educations.Add(new Education
            {
                School = e.School!.Trim(),
                FieldOfStudy = e.FieldOfStudy?.Trim(),
                Level = e.Level,
                StartDate = ParseIsoLike(ToIsoLike(e.StartDate)),
                EndDate = e.IsCurrent ? null : ParseIsoLike(ToIsoLike(e.EndDate)),
                IsCurrent = e.IsCurrent
            });
        }

        _db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        profile.WorkExperiences.Clear();

        foreach (var w in data.WorkExperiences.Where(x => !string.IsNullOrWhiteSpace(x.CompanyName)))
        {
            profile.WorkExperiences.Add(new WorkExperience
            {
                CompanyName = w.CompanyName!.Trim(),
                Position = w.Position?.Trim(),
                Description = w.Description?.Trim(),
                StartDate = ParseIsoLike(ToIsoLike(w.StartDate)),
                EndDate = w.IsCurrent ? null : ParseIsoLike(ToIsoLike(w.EndDate)),
                IsCurrent = w.IsCurrent
            });
        }

        _db.CandidateSkills.RemoveRange(profile.CandidateSkills);
        profile.CandidateSkills.Clear();

        var yetenekler = (data.SkillsCsv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await AttachSkillsAsync(profile, yetenekler, ct);

        await _db.SaveChangesAsync(ct);

        TempData["Bilgi"] = "Başvurunuz güncellendi.";
        return RedirectToAction(nameof(Edit), new { key });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApplication(string key, CancellationToken ct)
    {
        var profile = await BulEditProfiliAsync(key, ct);
        if (profile is null) return View("EditUnavailable");

        var submissions = await _db.CvSubmissions
            .Where(s => s.CandidateProfileId == profile.Id)
            .ToListAsync(ct);

        // Diskteki dosyalar da silinsin
        foreach (var s in submissions)
        {
            _storage.Delete(s.StoredFileName);

            if (!string.IsNullOrEmpty(s.PreviewImageFileName))
                _storage.Delete(s.PreviewImageFileName);

            if (!string.IsNullOrEmpty(s.PhotoFileName))
                _storage.Delete(s.PhotoFileName);
        }

        var notes = await _db.CandidateNotes
            .Where(n => n.CandidateProfileId == profile.Id)
            .ToListAsync(ct);

        _db.CandidateNotes.RemoveRange(notes);
        _db.CvSubmissions.RemoveRange(submissions);
        _db.CandidateSkills.RemoveRange(profile.CandidateSkills);
        _db.Educations.RemoveRange(profile.Educations);
        _db.WorkExperiences.RemoveRange(profile.WorkExperiences);
        _db.CandidateProfiles.Remove(profile);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Başvuru aday tarafından silindi: {Reference}",
            profile.ApplicationReferenceNumber);

        return View("ApplicationDeleted");
    }

    private async Task<string> GenerateUniqueReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = ApplicationHelpers.GenerateReferenceNumber();

            var exists = await _db.CandidateProfiles
                .AnyAsync(x => x.ApplicationReferenceNumber == candidate, ct);

            if (!exists) return candidate;
        }

        throw new InvalidOperationException("Benzersiz başvuru numarası üretilemedi.");
    }

    private async Task AttachSkillsAsync(
        CandidateProfile profile,
        IEnumerable<string> rawSkills,
        CancellationToken ct)
    {
        var names = rawSkills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0) return;

        // Sözlükteki yazımlar kazanır: "REACT" girilse de "React" kaydedilir
        var existing = await _db.Skills
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Name, s => s.Name, StringComparer.OrdinalIgnoreCase, ct);

        var added = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in names)
        {
            var name = ApplicationHelpers.NormalizeSkillName(raw, existing);

            var skill = await _db.Skills
                .FirstOrDefaultAsync(s => s.Name == name, ct);

            if (skill is null && !added.TryGetValue(name, out skill))
            {
                skill = new Skill { Name = name };
                _db.Skills.Add(skill);
                added[name] = skill;
            }

            profile.CandidateSkills.Add(new CandidateSkill
            {
                Skill = skill!,
                IsAiExtracted = true
            });
        }
    }

    private async Task<IActionResult> BuildSummaryViewAsync(
        CvSubmission submission,
        CvConsentInputModel consent,
        CancellationToken ct)
    {
        var data = string.IsNullOrWhiteSpace(submission.ExtractedJson)
            ? null
            : JsonSerializer.Deserialize<ExtractedCvData>(submission.ExtractedJson);

        if (data is null)
            return RedirectToAction(nameof(Review), new { token = consent.Token });

        var vm = new CvSummaryViewModel
        {
            Token = consent.Token,
            OriginalFileName = submission.OriginalFileName,
            HasPreview = !string.IsNullOrEmpty(submission.PreviewImageFileName),
            Skills = data.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            Consent = consent
        };

        ExtractionMapper.Apply(vm.Data, data);
        vm.Data.CityId = data.CityId;
        vm.Data.PreferredEmploymentType = data.PreferredEmploymentType;

        if (data.CityId is int cityId)
        {
            vm.CityName = await _db.Cities
                .AsNoTracking()
                .Where(c => c.Id == cityId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        return View(nameof(Summary), vm);
    }

    private static DateOnly? ParseIsoLike(string? isoLike)
    {
        if (string.IsNullOrWhiteSpace(isoLike)) return null;

        return DateOnly.TryParseExact(
            isoLike + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    [HttpGet]
    public async Task<IActionResult> File(Guid token, string type, CancellationToken ct)
    {
        var submission = await _db.CvSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (submission is null) return NotFound();

        var (fileName, contentType) = type switch
        {
            "preview" => (submission.PreviewImageFileName, "image/jpeg"),
            "photo" => (submission.PhotoFileName, "image/jpeg"),
            "pdf" => (submission.StoredFileName, "application/pdf"),
            _ => (null, null)
        };

        if (fileName is null || contentType is null) return NotFound();
        if (!_storage.Exists(fileName)) return NotFound();

        var stream = await _storage.OpenReadAsync(fileName, ct);
        return File(stream, contentType);
    }

    private async Task<List<CityOption>> GetCitiesAsync(CancellationToken ct)
        => await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityOption(c.Id, c.Name))
            .ToListAsync(ct);

    private static ExtractedCvData ToExtractedData(CvReviewViewModel vm)
        => new()
        {
            FullName = vm.FullName,
            Email = vm.Email,
            PhoneNumber = vm.PhoneNumber,
            Address = vm.Address,
            LinkedInUrl = vm.LinkedInUrl,
            CityId = vm.CityId,
            PreferredEmploymentType = vm.PreferredEmploymentType,
            TotalExperienceMonths = vm.TotalExperienceMonths,
            Educations = vm.Educations.Select(e => new ExtractedEducation
            {
                School = e.School,
                FieldOfStudy = e.FieldOfStudy,
                Level = e.Level?.ToString(),
                StartDate = ToIsoLike(e.StartDate),
                EndDate = e.IsCurrent ? null : ToIsoLike(e.EndDate),
                IsCurrent = e.IsCurrent
            }).ToList(),
            WorkExperiences = vm.WorkExperiences.Select(w => new ExtractedWorkExperience
            {
                CompanyName = w.CompanyName,
                Position = w.Position,
                Description = w.Description,
                StartDate = ToIsoLike(w.StartDate),
                EndDate = w.IsCurrent ? null : ToIsoLike(w.EndDate),
                IsCurrent = w.IsCurrent
            }).ToList(),
            Skills = (vm.SkillsCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList()
        };

    private static string? ToIsoLike(string? display)
        => ExtractionMapper.ParseDisplayDate(display)?.ToString("yyyy-MM");
}