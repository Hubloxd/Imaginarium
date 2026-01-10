namespace ImaginariumBackend.Services;

public interface IStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid userId);
    Task<bool> DeleteFileAsync(string filePath);
    Task<Stream?> GetFileAsync(string filePath);
    string GetMediaUrl(string filePath);
    string GetThumbnailUrl(string? thumbnailPath);
}
