namespace CVMatch.Web.Services;

public interface ICvProcessingService
{
    /// <summary>
    /// Yüklenen CV'yi işler: metin çıkarır, önizleme ve fotoğraf üretir,
    /// AI ile yapılandırılmış veri elde eder. Sonucu CvSubmission üzerine yazar.
    /// </summary>
    Task ProcessAsync(int submissionId, CancellationToken ct = default);
}