using CVMatch.Web.Models.ViewModels;

namespace CVMatch.Web.Services;

public interface IMatchingService
{
    /// <summary>
    /// İlana uygun adayları yetenek uyumuna göre skorlayıp sıralar.
    /// Skor veritabanında saklanmaz, her çağrıda hesaplanır.
    /// </summary>
    Task<MatchResultViewModel?> MatchAsync(int jobPostingId, int asgariSkor = 1, CancellationToken ct = default);
}