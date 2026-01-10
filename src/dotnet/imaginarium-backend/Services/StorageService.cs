using System.IO;

namespace ImaginariumBackend.Services;

public class StorageService : IStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<StorageService> _logger;

    public StorageService(IConfiguration configuration, ILogger<StorageService> logger)
    {
        _storagePath = configuration["Storage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
        _logger = logger;

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid userId)
    {
        var userFolder = Path.Combine(_storagePath, userId.ToString());
        if (!Directory.Exists(userFolder))
        {
            Directory.CreateDirectory(userFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(userFolder, uniqueFileName);

        using (var fileStreamOut = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOut);
        }

        return filePath;
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania pliku: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<Stream?> GetFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return Task.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
            }
            return Task.FromResult<Stream?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu pliku: {FilePath}", filePath);
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<string> GenerateThumbnailAsync(string filePath, string mimeType)
    {
        // TODO: Implementacja generowania miniatur (np. ImageSharp, SkiaSharp)
        // Na razie zwracamy tę samą ścieżkę
        return Task.FromResult(filePath);
    }

    public string GetMediaUrl(string filePath)
    {
        // Konwertuj ścieżkę bezwzględną na URL względny dla nginx
        // Przykład: /app/Storage/{userId}/{fileName} -> /media/{userId}/{fileName}
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        // Usuń prefix _storagePath i zamień na /media/
        if (filePath.StartsWith(_storagePath))
        {
            var relativePath = filePath.Substring(_storagePath.Length).Replace('\\', '/');
            if (relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);
            return $"/media/{relativePath}";
        }

        // Jeśli ścieżka już jest względna, zwróć ją
        return filePath;
    }

    public string GetThumbnailUrl(string? thumbnailPath)
    {
        if (string.IsNullOrEmpty(thumbnailPath))
            return string.Empty;

        return GetMediaUrl(thumbnailPath);
    }
}
