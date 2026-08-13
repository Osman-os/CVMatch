using CVMatch.Web.Models.Extraction;

namespace CVMatch.Web.Services;

public record CvExtractionResult(
    bool Success,
    ExtractedCvData? Data,
    string? RawJson,
    string? ErrorMessage);

public interface ICvExtractionService
{
    Task<CvExtractionResult> ExtractAsync(string cvText, CancellationToken ct = default);
}