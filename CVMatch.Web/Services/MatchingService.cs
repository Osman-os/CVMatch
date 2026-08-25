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
        int jobPostingId,
        int asgariSkor = 1,
        string turFiltresi = "uyumlu",
        bool sadeceBasvuranlar = true,
        CancellationToken ct = default)
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
            AsgariSkor = asgariSkor,
            TurFiltresi = turFiltresi,
            SadeceBasvuranlar = sadeceBasvuranlar
        };

        vm.BasvuranSayisi = await _db.CandidateProfiles
            .AsNoTracking()
            .CountAsync(x => x.JobPostingId == jobPostingId, ct);

        if (aranan.Count == 0) return vm;

        var toplamPuan = aranan.Sum(y => y.Zorunlu ? ZorunluAgirlik : TercihAgirlik);

        var adayQuery = _db.CandidateProfiles.AsNoTracking();

        if (sadeceBasvuranlar)
            adayQuery = adayQuery.Where(x => x.JobPostingId == jobPostingId);

        var adaylar = await adayQuery
            .Select(x => new
            {
                x.Id,
                x.ApplicationReferenceNumber,
                x.FullName,
                x.Email,
                x.CityId,
                CityName = x.City != null ? x.City.Name : null,
                x.TotalExperienceMonths,
                x.PreferredEmploymentType,
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

            var eslesen = aranan
                .Where(y => sahipOlunan.Contains(y.SkillId))
                .Select(y => y.Name)
                .ToList();

            var eksik = aranan
                .Where(y => !sahipOlunan.Contains(y.SkillId))
                .Select(y => y.Name)
                .ToList();

            vm.Adaylar.Add(new EslesenAday
            {
                Id = aday.Id,
                ApplicationReferenceNumber = aday.ApplicationReferenceNumber,
                FullName = aday.FullName,
                Email = aday.Email,
                CityName = aday.CityName,
                AyniSehir = ilan.CityId.HasValue && aday.CityId == ilan.CityId,
                SehirKriteriYok = ilan.CityId is null,
                TotalExperienceMonths = aday.TotalExperienceMonths,
                DeneyimYeterli = deneyimYili >= ilan.MinExperienceYears,
                Status = aday.Status,
                EksikZorunluSayisi = eksikZorunlu,
                Skor = (int)Math.Round(kazanilan * 100.0 / toplamPuan),
                SahipOlunanSkillIds = sahipOlunan,
                TurUyumlu = aday.PreferredEmploymentType == ilan.EmploymentType,
                EslesenYetenekler = eslesen,
                EksikYetenekler = eksik
            });
        }

        // Eşit skorda daha deneyimli aday öne geçsin
        vm.Adaylar = vm.Adaylar
            .OrderByDescending(a => a.Skor)
            .ThenByDescending(a => a.TotalExperienceMonths)
            .ToList();

        // Özet sayılar filtrelerden önce hesaplanır
        var skorlu = vm.Adaylar.Where(a => a.Skor > 0).ToList();

        vm.ToplamAday = skorlu.Count;
        vm.YuksekUyumluSayisi = skorlu.Count(a => a.Skor >= 80);
        vm.OrtalamaUyum = skorlu.Count == 0
            ? 0
            : (int)Math.Round(skorlu.Average(a => a.Skor));

        vm.GizlenenSayisi = vm.Adaylar.Count(a => a.Skor < vm.AsgariSkor);
        vm.Adaylar = vm.Adaylar.Where(a => a.Skor >= vm.AsgariSkor).ToList();

        // Tür uyumu artık filtre, skoru etkilemiyor
        vm.TurUyumsuzSayisi = vm.Adaylar.Count(a => !a.TurUyumlu);

        if (!sadeceBasvuranlar)
        {
            vm.Adaylar = turFiltresi switch
            {
                "uyumlu" => vm.Adaylar.Where(a => a.TurUyumlu).ToList(),
                "uyumsuz" => vm.Adaylar.Where(a => !a.TurUyumlu).ToList(),
                _ => vm.Adaylar
            };
        }

        return vm;
    }
}