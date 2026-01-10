using ImaginariumBackend.Models;

namespace ImaginariumBackend.Services;

public interface IThumbnailQueueService
{
    Task EnqueueThumbnailTaskAsync(Guid mediaId, string filePath, string mimeType, MediaTypeEnum mediaType);
}
