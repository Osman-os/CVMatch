using CVMatch.Web.Models.ViewModels;

namespace CVMatch.Web.Services;

public interface IMatchingService
{
    Task<MatchResultViewModel?> MatchAsync(
        int jobPostingId,
        int asgariSkor = 1,
        string turFiltresi = "uyumlu",
        bool sadeceBasvuranlar = true,
        CancellationToken ct = default);
}