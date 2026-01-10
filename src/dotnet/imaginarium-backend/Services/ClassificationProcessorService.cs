using System.Text;
using System.Text.Json;
using StackExchange.Redis;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using ImaginariumBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace ImaginariumBackend.Services;

public class ClassificationProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ClassificationProcessorService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const string QueueKey = "classification:queue";

    public ClassificationProcessorService(
        IServiceProvider serviceProvider,
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClassificationProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClassificationProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessClassificationTaskAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przetwarzania zadania klasyfikacji");
            }

            // Czekaj przed następnym sprawdzeniem
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("ClassificationProcessorService stopped");
    }

    private async Task ProcessClassificationTaskAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var taskJson = await db.ListRightPopAsync(QueueKey);

        if (taskJson.IsNullOrEmpty)
            return;

        try
        {
            var task = JsonSerializer.Deserialize<ClassificationTask>(taskJson!);
            if (task == null)
            {
                _logger.LogWarning("Nie można zdeserializować zadania klasyfikacji");
                return;
            }

            _logger.LogInformation("Przetwarzanie klasyfikacji: MediaId={MediaId}", task.MediaId);

            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var mediaRepository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();
            var mediaTagRepository = scope.ServiceProvider.GetRequiredService<IMediaTagRepository>();

            // Sprawdź czy media istnieje i jest obrazem
            var media = await mediaRepository.GetByIdAsync(task.MediaId);
            if (media == null)
            {
                _logger.LogWarning("Media nie znalezione: MediaId={MediaId}", task.MediaId);
                return;
            }

            if (media.MediaType != MediaTypeEnum.Image)
            {
                _logger.LogInformation("Klasyfikacja pominięta - media nie jest obrazem: MediaId={MediaId}", task.MediaId);
                return;
            }

            // Wczytaj obraz z dysku
            if (!File.Exists(task.FilePath))
            {
                _logger.LogWarning("Plik nie istnieje: FilePath={FilePath}", task.FilePath);
                return;
            }

            var imageBytes = await File.ReadAllBytesAsync(task.FilePath, cancellationToken);
            var imageBase64 = Convert.ToBase64String(imageBytes);

            // Wyślij do mikroserwisu AI
            var aiServiceUrl = _configuration["AiClassification:BaseUrl"] ?? "http://ai-classification:8000";
            var classificationResult = await ClassifyImageAsync(aiServiceUrl, imageBase64, task.MediaId.ToString(), cancellationToken);

            if (classificationResult == null || !classificationResult.Success)
            {
                _logger.LogWarning("Klasyfikacja nie powiodła się: MediaId={MediaId}", task.MediaId);
                return;
            }

            // Zapisz wyniki klasyfikacji
            await SaveClassificationResultsAsync(
                media,
                classificationResult,
                tagRepository,
                mediaTagRepository);

            _logger.LogInformation("Klasyfikacja zakończona pomyślnie: MediaId={MediaId}, Category={Category}, Confidence={Confidence}",
                task.MediaId, classificationResult.Category, classificationResult.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas klasyfikacji obrazu");
        }
    }

    private async Task<ClassificationResponse?> ClassifyImageAsync(
        string baseUrl,
        string imageBase64,
        string mediaId,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // Dłuższy timeout dla klasyfikacji

            var requestBody = new
            {
                image_base64 = imageBase64,
                media_id = mediaId
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{baseUrl}/classify", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Błąd odpowiedzi z AI service: StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ClassificationResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas komunikacji z AI service");
            return null;
        }
    }

    private async Task SaveClassificationResultsAsync(
        Media media,
        ClassificationResponse classificationResult,
        ITagRepository tagRepository,
        IMediaTagRepository mediaTagRepository)
    {
        try
        {
            // Zapisz główną kategorię jako tag
            var mainTag = await GetOrCreateTagAsync(
                tagRepository,
                classificationResult.Category,
                classificationResult.Category,
                classificationResult.Confidence);

            // Sprawdź czy już istnieje MediaTag dla tego media i tagu
            if (!await mediaTagRepository.ExistsAsync(media.Id, mainTag.Id))
            {
                var mediaTag = new MediaTag
                {
                    MediaId = media.Id,
                    TagId = mainTag.Id,
                    AddedAt = DateTime.UtcNow,
                    Source = "AI-classification"
                };
                await mediaTagRepository.CreateAsync(mediaTag);
            }

            // Zapisz wszystkie predykcje jako tagi
            if (classificationResult.Predictions != null)
            {
                foreach (var prediction in classificationResult.Predictions)
                {
                    var tag = await GetOrCreateTagAsync(
                        tagRepository,
                        prediction.Class,
                        classificationResult.Category,
                        prediction.Confidence);

                    // Sprawdź czy już istnieje MediaTag
                    if (!await mediaTagRepository.ExistsAsync(media.Id, tag.Id))
                    {
                        var mediaTag = new MediaTag
                        {
                            MediaId = media.Id,
                            TagId = tag.Id,
                            AddedAt = DateTime.UtcNow,
                            Source = "AI-classification"
                        };
                        await mediaTagRepository.CreateAsync(mediaTag);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisywania wyników klasyfikacji: MediaId={MediaId}", media.Id);
            throw;
        }
    }

    private async Task<Tag> GetOrCreateTagAsync(
        ITagRepository tagRepository,
        string tagName,
        string category,
        float confidence)
    {
        var existingTag = await tagRepository.GetByNameAsync(tagName);
        if (existingTag != null)
        {
            // Aktualizuj confidence jeśli nowy jest wyższy
            if (confidence > existingTag.Confidence)
            {
                existingTag.Confidence = confidence;
                existingTag.Category = category;
                return await tagRepository.UpdateAsync(existingTag);
            }
            return existingTag;
        }

        // Utwórz nowy tag
        var newTag = new Tag
        {
            Name = tagName,
            Category = category,
            Confidence = confidence
        };

        return await tagRepository.CreateAsync(newTag);
    }
}

public class ClassificationResponse
{
    public bool Success { get; set; }
    public string? MediaId { get; set; }
    public string Category { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<Prediction>? Predictions { get; set; }
}

public class Prediction
{
    public string Class { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
