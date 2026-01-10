using System.Text.Json;
using StackExchange.Redis;

namespace ImaginariumBackend.Services;

public class ClassificationQueueService : IClassificationQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ClassificationQueueService> _logger;
    private const string QueueKey = "classification:queue";

    public ClassificationQueueService(IConnectionMultiplexer redis, ILogger<ClassificationQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task EnqueueClassificationTaskAsync(Guid mediaId, string filePath)
    {
        try
        {
            var task = new ClassificationTask
            {
                MediaId = mediaId,
                FilePath = filePath
            };

            var json = JsonSerializer.Serialize(task);
            var db = _redis.GetDatabase();
            await db.ListLeftPushAsync(QueueKey, json);

            _logger.LogInformation("Dodano zadanie klasyfikacji do kolejki: MediaId={MediaId}", mediaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania zadania klasyfikacji do kolejki: MediaId={MediaId}", mediaId);
            throw;
        }
    }
}

public class ClassificationTask
{
    public Guid MediaId { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
