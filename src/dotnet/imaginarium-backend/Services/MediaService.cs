using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;

namespace ImaginariumBackend.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Media> UploadMediaAsync(Guid userId, IFormFile file)
    {
        // Określ typ media
        var mediaType = file.ContentType.StartsWith("image/") 
            ? MediaTypeEnum.Image 
            : MediaTypeEnum.Video;

        // Zapisz plik
        using var fileStream = file.OpenReadStream();
        var filePath = await _storageService.SaveFileAsync(fileStream, file.FileName, userId);

        // Generuj thumbnail (na razie używamy tej samej ścieżki)
        var thumbnailPath = await _storageService.GenerateThumbnailAsync(filePath, file.ContentType);

        // Utwórz encję Media
        var media = new Media
        {
            UserId = userId,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            MediaType = mediaType,
            MimeType = file.ContentType,
            UploadedAt = DateTime.UtcNow,
            ThumbnailPath = thumbnailPath
        };

        // TODO: Ekstrakcja metadanych (szerokość, wysokość, czas trwania dla video)
        // Można użyć biblioteki jak ImageSharp dla obrazów lub FFmpeg dla video

        return await _mediaRepository.CreateAsync(media);
    }

    public async Task<MediaResponseDto?> GetMediaByIdAsync(Guid id, Guid userId)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        if (media == null || media.UserId != userId)
            return null;

        return MapToDto(media);
    }

    public async Task<List<MediaResponseDto>> GetUserMediaAsync(Guid userId)
    {
        var mediaList = await _mediaRepository.GetByUserIdAsync(userId);
        return mediaList.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteMediaAsync(Guid id, Guid userId)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        if (media == null || media.UserId != userId)
            return false;

        // Usuń plik z dysku
        await _storageService.DeleteFileAsync(media.FilePath);
        if (!string.IsNullOrEmpty(media.ThumbnailPath))
        {
            await _storageService.DeleteFileAsync(media.ThumbnailPath);
        }

        return await _mediaRepository.DeleteAsync(id);
    }

    private MediaResponseDto MapToDto(Media media)
    {
        return new MediaResponseDto
        {
            Id = media.Id,
            FileName = media.FileName,
            FilePath = media.FilePath,
            MediaUrl = _storageService.GetMediaUrl(media.FilePath),
            FileSize = media.FileSize,
            MediaType = media.MediaType.ToString(),
            MimeType = media.MimeType,
            UploadedAt = media.UploadedAt,
            Width = media.Width,
            Height = media.Height,
            Duration = media.Duration,
            ThumbnailPath = media.ThumbnailPath,
            ThumbnailUrl = _storageService.GetThumbnailUrl(media.ThumbnailPath)
        };
    }
}
