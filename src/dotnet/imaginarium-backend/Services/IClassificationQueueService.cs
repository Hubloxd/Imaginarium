namespace ImaginariumBackend.Services;

public interface IClassificationQueueService
{
    Task EnqueueClassificationTaskAsync(Guid mediaId, string filePath);
}
