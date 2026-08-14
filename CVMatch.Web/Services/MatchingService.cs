using CVMatch.Web.Data;
using CVMatch.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Services;

public class MatchingService : IMatchingService
{
    private const int ZorunluAgirlik = 2;
    private const int TercihAgirlik = 1;

    private readonly ApplicationDbContext _db;

    public MatchingService(ApplicationDbContext db) => _db = db;

    public async Task<MatchResultViewModel?> MatchAsync(
        int jobPostingId, int asgariSkor = 1, CancellationToken ct = default)
    {
        var ilan = await _db.JobPostings
            .AsNoTracking()
            .Include(x => x.City)
            .Include(x => x.JobPostingSkills)
                .ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(x => x.Id == jobPostingId, ct);

        if (ilan is null) return null;

        var aranan = ilan.JobPostingSkills
            .OrderByDescending(s => s.IsRequired)
            .ThenBy(s => s.Skill.Name)
            .Select(s => new ArananYetenek
            {
                SkillId = s.SkillId,
                Name = s.Skill.Name,
                Zorunlu = s.IsRequired
            })
            .ToList();

        var vm = new MatchResultViewModel
        {
            JobPostingId = ilan.Id,
            Title = ilan.Title,
            CityId = ilan.CityId,
            CityName = ilan.City?.Name,
            EmploymentType = ilan.EmploymentType,
            MinExperienceYears = ilan.MinExperienceYears,
            Status = ilan.Status,
            Aranan = aranan,
            AsgariSkor = asgariSkor
        };

        if (aranan.Count == 0) return vm;

        var toplamPuan = aranan.Sum(y => y.Zorunlu ? ZorunluAgirlik : TercihAgirlik);

        // Çalışma türü filtre olarak uygulanır, skoru etkilemez
        var adaylar = await _db.CandidateProfiles
            .AsNoTracking()
            .Where(x => x.PreferredEmploymentType == ilan.EmploymentType)
            .Select(x => new
            {
                x.Id,
                x.ApplicationReferenceNumber,
                x.FullName,
                x.Email,
                x.CityId,
                CityName = x.City != null ? x.City.Name : null,
                x.TotalExperienceMonths,
                x.Status,
                SkillIds = x.CandidateSkills.Select(cs => cs.SkillId).ToList()
            })
            .ToListAsync(ct);

        foreach (var aday in adaylar)
        {
            var sahipOlunan = new HashSet<int>(aday.SkillIds);

            var kazanilan = aranan
                .Where(y => sahipOlunan.Contains(y.SkillId))
                .Sum(y => y.Zorunlu ? ZorunluAgirlik : TercihAgirlik);

            var eksikZorunlu = aranan
                .Count(y => y.Zorunlu && !sahipOlunan.Contains(y.SkillId));

            var deneyimYili = aday.TotalExperienceMonths / 12;

            vm.Adaylar.Add(new EslesenAday
            {
                Id = aday.Id,
                ApplicationReferenceNumber = aday.ApplicationReferenceNumber,
                FullName = aday.FullName,
                Email = aday.Email,
                CityName = aday.CityName,
                AyniSehir = ilan.CityId.HasValue && aday.CityId == ilan.CityId,
                TotalExperienceMonths = aday.TotalExperienceMonths,
                DeneyimYeterli = deneyimYili >= ilan.MinExperienceYears,
                Status = aday.Status,
                EksikZorunluSayisi = eksikZorunlu,
                Skor = (int)Math.Round(kazanilan * 100.0 / toplamPuan),
                SahipOlunanSkillIds = sahipOlunan
            });
        }

        // Eşit skorda daha deneyimli aday öne geçsin
        vm.Adaylar = vm.Adaylar
            .OrderByDescending(a => a.Skor)
            .ThenByDescending(a => a.TotalExperienceMonths)
            .ToList();

        vm.GizlenenSayisi = vm.Adaylar.Count(a => a.Skor < vm.AsgariSkor);
        vm.Adaylar = vm.Adaylar.Where(a => a.Skor >= vm.AsgariSkor).ToList();

        return vm;
    }
}