namespace CVMatch.Web.Services;

public interface IFileStorage
{
    /// <summary>Dosyayı kaydeder ve saklanan dosya adını döner.</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);

    Task<byte[]> ReadAsync(string storedFileName, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default);

    bool Exists(string storedFileName);

    void Delete(string storedFileName);
}