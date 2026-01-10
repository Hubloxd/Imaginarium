using System.Text.Json;
using System.Diagnostics;
using StackExchange.Redis;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using ImaginariumBackend.Data;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImaginariumBackend.Services;

public class ThumbnailProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ThumbnailProcessorService> _logger;
    private const string QueueKey = "thumbnail:queue";
    private const int MaxThumbnailSize = 400;

    public ThumbnailProcessorService(
        IServiceProvider serviceProvider,
        IConnectionMultiplexer redis,
        ILogger<ThumbnailProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ThumbnailProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessThumbnailTaskAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przetwarzania zadania miniatury");
            }

            // Czekaj przed następnym sprawdzeniem
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("ThumbnailProcessorService stopped");
    }

    private async Task ProcessThumbnailTaskAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var taskJson = await db.ListRightPopAsync(QueueKey);

        if (taskJson.IsNullOrEmpty)
            return;

        try
        {
            var task = JsonSerializer.Deserialize<ThumbnailTask>(taskJson!);
            if (task == null)
            {
                _logger.LogWarning("Nie można zdeserializować zadania miniatury");
                return;
            }

            _logger.LogInformation("Przetwarzanie miniatury: MediaId={MediaId}, Type={MediaType}", task.MediaId, task.MediaType);

            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var mediaRepository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string thumbnailPath;

            if (task.MediaType == MediaTypeEnum.Image)
            {
                thumbnailPath = await GenerateImageThumbnailAsync(configuration, task.FilePath, task.MediaId);
            }
            else
            {
                thumbnailPath = await GenerateVideoThumbnailAsync(configuration, task.FilePath, task.MediaId);
            }

            // Zaktualizuj media z ścieżką miniatury
            var media = await mediaRepository.GetByIdAsync(task.MediaId);
            if (media != null)
            {
                media.ThumbnailPath = thumbnailPath;
                await mediaRepository.UpdateAsync(media);

                // Zaktualizuj cover albumu jeśli to ostatnie dodane media
                await UpdateAlbumCoverAsync(context, media);
            }

            _logger.LogInformation("Miniatura wygenerowana pomyślnie: MediaId={MediaId}, Path={Path}", task.MediaId, thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania miniatury");
        }
    }

    private async Task<string> GenerateImageThumbnailAsync(IConfiguration configuration, string filePath, Guid mediaId)
    {
        try
        {
            using var image = await Image.LoadAsync(filePath);
            
            // Oblicz rozmiar miniatury zachowując proporcje
            var (width, height) = CalculateThumbnailSize(image.Width, image.Height, MaxThumbnailSize);
            
            // Utwórz miniaturę
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            // Zapisz miniaturę w katalogu Thumbnails w głównym katalogu Storage
            var storagePath = configuration["Storage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            var thumbnailDir = Path.Combine(storagePath, "Thumbnails");
            if (!Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            var thumbnailFileName = $"{mediaId}.jpg";
            var thumbnailPath = Path.Combine(thumbnailDir, thumbnailFileName);

            await image.SaveAsJpegAsync(thumbnailPath);

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania miniatury obrazu: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> GenerateVideoThumbnailAsync(IConfiguration configuration, string filePath, Guid mediaId)
    {
        try
        {
            // Użyj FFmpeg do wygenerowania miniatury z pierwszej klatki
            // W kontenerze Docker musisz mieć zainstalowany FFmpeg
            var storagePath = configuration["Storage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            var thumbnailDir = Path.Combine(storagePath, "Thumbnails");
            if (!Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            var thumbnailFileName = $"{mediaId}.jpg";
            var thumbnailPath = Path.Combine(thumbnailDir, thumbnailFileName);

            // Wywołaj FFmpeg
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\" -ss 00:00:01 -vframes 1 -vf \"scale={MaxThumbnailSize}:-1\" \"{thumbnailPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"FFmpeg error: {error}");
                }
            }

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania miniatury wideo: {FilePath}", filePath);
            // Jeśli FFmpeg nie jest dostępny, zwróć pustą ścieżkę
            return string.Empty;
        }
    }

    private (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxSize)
    {
        if (originalWidth <= maxSize && originalHeight <= maxSize)
            return (originalWidth, originalHeight);

        var ratio = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    private async Task UpdateAlbumCoverAsync(ApplicationDbContext context, Media media)
    {
        // Znajdź albumy zawierające to media i ustaw jako cover jeśli to ostatnie dodane media
        var albumMedias = await context.AlbumMedia
            .Where(am => am.MediaId == media.Id)
            .Include(am => am.Album)
            .ToListAsync();

        foreach (var albumMedia in albumMedias)
        {
            var album = albumMedia.Album;
            if (album != null)
            {
                // Sprawdź czy to ostatnie dodane media w albumie
                var lastMedia = await context.AlbumMedia
                    .Where(am => am.AlbumId == album.Id)
                    .OrderByDescending(am => am.AddedAt)
                    .FirstOrDefaultAsync();

                if (lastMedia != null && lastMedia.MediaId == media.Id)
                {
                    album.CoverMediaId = media.Id;
                    album.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
