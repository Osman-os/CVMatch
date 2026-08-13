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