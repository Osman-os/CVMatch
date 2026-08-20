namespace CVMatch.Web.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration config)
    {
        _rootPath = config["FileStorage:RootPath"]
            ?? throw new InvalidOperationException("FileStorage:RootPath yapılandırılmamış.");

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = BuildPath(storedFileName);

        try
        {
            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
            {
                await content.CopyToAsync(fs, ct);
            }
        }
        catch
        {
            // Yarım yazılan dosya diskte kalmasın; adı dışarı dönmediği için
            // başka hiçbir yerden temizlenemez
            try { System.IO.File.Delete(fullPath); } catch { /* yoksay */ }
            throw;
        }

        return storedFileName;
    }

    public async Task<byte[]> ReadAsync(string storedFileName, CancellationToken ct = default)
        => await File.ReadAllBytesAsync(BuildPath(storedFileName), ct);

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default)
        => Task.FromResult<Stream>(
            new FileStream(BuildPath(storedFileName), FileMode.Open, FileAccess.Read));

    public bool Exists(string storedFileName)
        => File.Exists(BuildPath(storedFileName));

    public void Delete(string storedFileName)
    {
        var fullPath = BuildPath(storedFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    // Path traversal koruması: dosya adı yalnızca ad olmalı, yol içermemeli
    private string BuildPath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(storedFileName));

        var safeName = Path.GetFileName(storedFileName);
        if (safeName != storedFileName)
            throw new ArgumentException("Geçersiz dosya adı.", nameof(storedFileName));

        return Path.Combine(_rootPath, safeName);
    }
}