using System.Text.Json;
using StackExchange.Redis;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Services;

public class ThumbnailQueueService : IThumbnailQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ThumbnailQueueService> _logger;
    private const string QueueKey = "thumbnail:queue";

    public ThumbnailQueueService(IConnectionMultiplexer redis, ILogger<ThumbnailQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task EnqueueThumbnailTaskAsync(Guid mediaId, string filePath, string mimeType, MediaTypeEnum mediaType)
    {
        try
        {
            var task = new ThumbnailTask
            {
                MediaId = mediaId,
                FilePath = filePath,
                MimeType = mimeType,
                MediaType = mediaType
            };

            var json = JsonSerializer.Serialize(task);
            var db = _redis.GetDatabase();
            await db.ListLeftPushAsync(QueueKey, json);

            _logger.LogInformation("Dodano zadanie generowania miniatury do kolejki: MediaId={MediaId}", mediaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania zadania do kolejki: MediaId={MediaId}", mediaId);
            throw;
        }
    }
}

public class ThumbnailTask
{
    public Guid MediaId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public MediaTypeEnum MediaType { get; set; }
}
