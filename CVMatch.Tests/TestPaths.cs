namespace CVMatch.Tests;

/// <summary>
/// Test dosyalarının konumu; makineye bağlı sabit yol kullanılmaz.
/// </summary>
public static class TestPaths
{
    public static string TestCvPdf =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "test-cv.pdf");

    /// <summary>Test çıktıları geçici klasöre yazılır, repoya karışmaz.</summary>
    public static string CreateOutputDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "CVMatchTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}