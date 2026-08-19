using CVMatch.Web.Models.Enums;
using CVMatch.Web.Services;

namespace CVMatch.Tests;

/// <summary>
/// Taslak bağlantısının geçerlilik kuralını doğrular.
/// Onaylanmış veya süresi dolmuş taslakların dosyalarına erişilememelidir.
/// </summary>
public class DraftAccessTests
{
    [Fact]
    public void OnaylanmisBasvuru_TaslakBaglantisiGecersiz()
    {
        // Süresi dolmamış olsa bile onaylanmış başvuru erişime kapalı
        var gecerli = ApplicationHelpers.DraftIsValid(
            SubmissionStatus.Approved,
            DateTime.UtcNow.AddHours(12));

        Assert.False(gecerli);
    }

    [Fact]
    public void SuresiDolmusTaslak_Gecersiz()
    {
        var gecerli = ApplicationHelpers.DraftIsValid(
            SubmissionStatus.AwaitingReview,
            DateTime.UtcNow.AddMinutes(-1));

        Assert.False(gecerli);
    }

    [Fact]
    public void AktifTaslak_Gecerli()
    {
        var gecerli = ApplicationHelpers.DraftIsValid(
            SubmissionStatus.AwaitingReview,
            DateTime.UtcNow.AddHours(12));

        Assert.True(gecerli);
    }

    [Fact]
    public void IslemBasarisiz_SuresiVarsa_Gecerli()
    {
        // Çıkarım başarısız olsa da aday manuel giriş yapabilmeli
        var gecerli = ApplicationHelpers.DraftIsValid(
            SubmissionStatus.Failed,
            DateTime.UtcNow.AddHours(12));

        Assert.True(gecerli);
    }
}