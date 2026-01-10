using ImaginariumBackend.Data;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImaginariumBackend.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IThumbnailQueueService _thumbnailQueueService;
    private readonly IClassificationQueueService _classificationQueueService;
    private readonly IShareRepository _shareRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IMediaTagRepository _mediaTagRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IThumbnailQueueService thumbnailQueueService,
        IClassificationQueueService classificationQueueService,
        IShareRepository shareRepository,
        IGroupRepository groupRepository,
        IAlbumRepository albumRepository,
        IMediaTagRepository mediaTagRepository,
        ApplicationDbContext context,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _thumbnailQueueService = thumbnailQueueService;
        _classificationQueueService = classificationQueueService;
        _shareRepository = shareRepository;
        _groupRepository = groupRepository;
        _albumRepository = albumRepository;
        _mediaTagRepository = mediaTagRepository;
        _context = context;
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

        // Utwórz encję Media (thumbnail będzie wygenerowany asynchronicznie)
        var media = new Media
        {
            UserId = userId,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            MediaType = mediaType,
            MimeType = file.ContentType,
            UploadedAt = DateTime.UtcNow,
            ThumbnailPath = null // Będzie ustawione po wygenerowaniu miniatury
        };

        media = await _mediaRepository.CreateAsync(media);

        // Dodaj zadanie generowania miniatury do kolejki
        await _thumbnailQueueService.EnqueueThumbnailTaskAsync(media.Id, filePath, file.ContentType, mediaType);

        // Dodaj zadanie klasyfikacji do kolejki (tylko dla obrazów)
        if (mediaType == MediaTypeEnum.Image)
        {
            await _classificationQueueService.EnqueueClassificationTaskAsync(media.Id, filePath);
        }

        // TODO: Ekstrakcja metadanych (szerokość, wysokość, czas trwania dla video)
        // Można użyć biblioteki jak ImageSharp dla obrazów lub FFmpeg dla video

        return media;
    }

    public async Task<MediaResponseDto?> GetMediaByIdAsync(Guid id, Guid userId)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        if (media == null)
            return null;

        // Sprawdź czy użytkownik jest właścicielem
        if (media.UserId == userId)
            return await MapToDtoAsync(media);

        // Sprawdź czy media jest udostępnione użytkownikowi
        if (await HasAccessToMediaAsync(id, userId))
            return await MapToDtoAsync(media);

        return null;
    }

    private async Task<bool> HasAccessToMediaAsync(Guid mediaId, Guid userId)
    {
        // Pobierz wszystkie grupy użytkownika raz na początku
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        var userGroupIds = new HashSet<Guid>(userGroups.Select(g => g.Id));

        // Sprawdź udostępnienia bezpośrednio dla użytkownika
        var userShares = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        if (userShares.Any(s => s.MediaId == mediaId && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
            return true;

        // Sprawdź czy media jest w udostępnionym albumie
        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media != null)
        {
            var albumMedias = await _context.AlbumMedia
                .Where(am => am.MediaId == mediaId)
                .Select(am => am.AlbumId)
                .ToListAsync();

            foreach (var albumId in albumMedias)
            {
                var albumShares = await _shareRepository.GetByAlbumIdAsync(albumId);
                if (albumShares.Any(s => (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow) &&
                    (s.SharedWithUserId == userId || 
                     (s.SharedWithGroupId.HasValue && userGroupIds.Contains(s.SharedWithGroupId.Value)) ||
                     s.IsPublic)))
                    return true;
            }
        }

        // Sprawdź udostępnienia dla grup użytkownika
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            if (groupShares.Any(s => s.MediaId == mediaId && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
                return true;
        }

        // Sprawdź publiczne udostępnienia
        var publicShares = await _context.Shares
            .AnyAsync(s => s.MediaId == mediaId && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow));

        return publicShares;
    }

    public async Task<List<MediaResponseDto>> GetUserMediaAsync(Guid userId)
    {
        // Pobierz media użytkownika
        var userMedia = await _mediaRepository.GetByUserIdAsync(userId);
        var mediaIds = new HashSet<Guid>(userMedia.Select(m => m.Id));

        // Pobierz media z albumów użytkownika
        var userAlbums = await _albumRepository.GetByUserIdAsync(userId);
        var albumMediaIds = new List<Guid>();
        foreach (var album in userAlbums)
        {
            var albumMedia = await _mediaRepository.GetByAlbumIdAsync(album.Id);
            albumMediaIds.AddRange(albumMedia.Select(m => m.Id));
        }
        mediaIds.UnionWith(albumMediaIds);

        // Pobierz media udostępnione bezpośrednio użytkownikowi
        var sharesForUser = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        var sharedMediaIds = sharesForUser
            .Where(s => s.MediaId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.MediaId!.Value)
            .ToList();

        // Pobierz media z udostępnionych albumów
        var sharedAlbums = await GetSharedAlbumsForUserAsync(userId);
        foreach (var album in sharedAlbums)
        {
            var albumMedia = await _mediaRepository.GetByAlbumIdAsync(album.Id);
            sharedMediaIds.AddRange(albumMedia.Select(m => m.Id));
        }

        // Pobierz media udostępnione grupom, w których użytkownik jest członkiem
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            var groupMediaIds = groupShares
                .Where(s => s.MediaId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
                .Select(s => s.MediaId!.Value);
            sharedMediaIds.AddRange(groupMediaIds);
        }

        // Pobierz media z albumów udostępnionych grupom
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            var groupAlbumIds = groupShares
                .Where(s => s.AlbumId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
                .Select(s => s.AlbumId!.Value)
                .Distinct()
                .ToList();
            
            foreach (var albumId in groupAlbumIds)
            {
                var albumMedia = await _mediaRepository.GetByAlbumIdAsync(albumId);
                sharedMediaIds.AddRange(albumMedia.Select(m => m.Id));
            }
        }

        // Pobierz media udostępnione publicznie
        var publicMediaShares = await _context.Shares
            .Where(s => s.MediaId != null && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.MediaId!.Value)
            .ToListAsync();

        sharedMediaIds.AddRange(publicMediaShares);

        // Pobierz media z publicznych albumów
        var publicAlbumShares = await _context.Shares
            .Where(s => s.AlbumId != null && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.AlbumId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var albumId in publicAlbumShares)
        {
            var albumMedia = await _mediaRepository.GetByAlbumIdAsync(albumId);
            sharedMediaIds.AddRange(albumMedia.Select(m => m.Id));
        }

        // Pobierz wszystkie udostępnione media (które nie są już w liście użytkownika)
        var uniqueSharedMediaIds = sharedMediaIds.Distinct().Where(id => !mediaIds.Contains(id)).ToList();
        
        var sharedMedia = new List<Media>();
        if (uniqueSharedMediaIds.Any())
        {
            sharedMedia = await _context.Media
                .Where(m => uniqueSharedMediaIds.Contains(m.Id))
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        // Połącz media użytkownika z udostępnionymi
        var allMedia = userMedia.Concat(sharedMedia).OrderByDescending(m => m.UploadedAt).ToList();
        
        // Mapuj do DTO z tagami
        var result = new List<MediaResponseDto>();
        foreach (var media in allMedia)
        {
            result.Add(await MapToDtoAsync(media));
        }
        
        return result;
    }

    private async Task<List<Album>> GetSharedAlbumsForUserAsync(Guid userId)
    {
        var sharedAlbumIds = new List<Guid>();

        // Albumy udostępnione bezpośrednio użytkownikowi
        var sharesForUser = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        sharedAlbumIds.AddRange(sharesForUser
            .Where(s => s.AlbumId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.AlbumId!.Value));

        // Albumy udostępnione grupom użytkownika
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            sharedAlbumIds.AddRange(groupShares
                .Where(s => s.AlbumId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
                .Select(s => s.AlbumId!.Value));
        }

        // Publiczne albumy
        var publicAlbums = await _context.Shares
            .Where(s => s.AlbumId != null && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.AlbumId!.Value)
            .Distinct()
            .ToListAsync();

        sharedAlbumIds.AddRange(publicAlbums);

        var uniqueAlbumIds = sharedAlbumIds.Distinct().ToList();
        
        if (!uniqueAlbumIds.Any())
            return new List<Album>();

        return await _context.Albums
            .Where(a => uniqueAlbumIds.Contains(a.Id))
            .ToListAsync();
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

    private async Task<MediaResponseDto> MapToDtoAsync(Media media)
    {
        // Pobierz tagi dla tego media
        var mediaTags = await _mediaTagRepository.GetByMediaIdAsync(media.Id);
        var tags = mediaTags.Select(mt => new DTOs.TagDto
        {
            Id = mt.Tag!.Id,
            Name = mt.Tag.Name,
            Category = mt.Tag.Category,
            Confidence = mt.Tag.Confidence,
            Source = mt.Source
        }).ToList();

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
            ThumbnailUrl = _storageService.GetThumbnailUrl(media.ThumbnailPath),
            Tags = tags
        };
    }

    private MediaResponseDto MapToDto(Media media)
    {
        // Synchronous version for backward compatibility (will be updated to async where needed)
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
            ThumbnailUrl = _storageService.GetThumbnailUrl(media.ThumbnailPath),
            Tags = new List<DTOs.TagDto>() // Empty for now, will be populated in async methods
        };
    }
}
