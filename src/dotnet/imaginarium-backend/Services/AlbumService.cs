using ImaginariumBackend.Data;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImaginariumBackend.Services;

public class AlbumService : IAlbumService
{
    private readonly IAlbumRepository _albumRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlbumService> _logger;

    public AlbumService(
        IAlbumRepository albumRepository,
        IMediaRepository mediaRepository,
        IMediaService mediaService,
        IStorageService storageService,
        ApplicationDbContext context,
        ILogger<AlbumService> logger)
    {
        _albumRepository = albumRepository;
        _mediaRepository = mediaRepository;
        _mediaService = mediaService;
        _storageService = storageService;
        _context = context;
        _logger = logger;
    }

    public async Task<AlbumResponseDto> CreateAlbumAsync(Guid userId, CreateAlbumDto createDto, List<IFormFile> files)
    {
        // Utwórz album
        var album = new Album
        {
            UserId = userId,
            Name = createDto.Name,
            Description = createDto.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        album = await _albumRepository.CreateAsync(album);

        // Upload plików i dodaj do albumu
        if (files != null && files.Count > 0)
        {
            var order = 0;
            foreach (var file in files)
            {
                var media = await _mediaService.UploadMediaAsync(userId, file);
                
                // Dodaj media do albumu
                var albumMedia = new AlbumMedia
                {
                    AlbumId = album.Id,
                    MediaId = media.Id,
                    AddedAt = DateTime.UtcNow,
                    Order = order++
                };

                _context.AlbumMedia.Add(albumMedia);

                // Ustaw pierwsze media jako cover jeśli nie ma covera
                if (album.CoverMediaId == null)
                {
                    album.CoverMediaId = media.Id;
                }
            }

            await _context.SaveChangesAsync();
            await _albumRepository.UpdateAsync(album);
        }

        // Załaduj album ponownie z AlbumMedias, aby mieć poprawny MediaCount
        album = await _albumRepository.GetByIdWithMediaAsync(album.Id) ?? album;
        
        return MapToDto(album);
    }

    public async Task<AlbumResponseDto?> GetAlbumByIdAsync(Guid id, Guid userId)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(id);
        if (album == null || album.UserId != userId)
            return null;

        return MapToDto(album);
    }

    public async Task<AlbumDetailResponseDto?> GetAlbumDetailByIdAsync(Guid id, Guid userId)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(id);
        if (album == null || album.UserId != userId)
            return null;

        return MapToDetailDto(album);
    }

    public async Task<List<AlbumResponseDto>> GetUserAlbumsAsync(Guid userId)
    {
        var albums = await _albumRepository.GetByUserIdAsync(userId);
        return albums.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteAlbumAsync(Guid id, Guid userId)
    {
        var album = await _albumRepository.GetByIdAsync(id);
        if (album == null || album.UserId != userId)
            return false;

        return await _albumRepository.DeleteAsync(id);
    }

    public async Task<bool> AddMediaToAlbumAsync(Guid albumId, Guid mediaId, Guid userId)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(albumId);
        if (album == null || album.UserId != userId)
            return false;

        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media == null || media.UserId != userId)
            return false;

        // Sprawdź czy media już jest w albumie
        var exists = await _context.AlbumMedia
            .AnyAsync(am => am.AlbumId == albumId && am.MediaId == mediaId);
        if (exists)
            return false;

        var maxOrder = album.AlbumMedias.Any() 
            ? album.AlbumMedias.Max(am => am.Order) 
            : -1;

        var albumMedia = new AlbumMedia
        {
            AlbumId = albumId,
            MediaId = mediaId,
            AddedAt = DateTime.UtcNow,
            Order = maxOrder + 1
        };

        _context.AlbumMedia.Add(albumMedia);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AlbumDetailResponseDto?> AddFilesToAlbumAsync(Guid albumId, Guid userId, List<IFormFile> files)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(albumId);
        if (album == null || album.UserId != userId)
            return null;

        if (files == null || files.Count == 0)
            return MapToDetailDto(album);

        var maxOrder = album.AlbumMedias.Any() 
            ? album.AlbumMedias.Max(am => am.Order) 
            : -1;

        var order = maxOrder + 1;
        foreach (var file in files)
        {
            var media = await _mediaService.UploadMediaAsync(userId, file);
            
            // Dodaj media do albumu
            var albumMedia = new AlbumMedia
            {
                AlbumId = albumId,
                MediaId = media.Id,
                AddedAt = DateTime.UtcNow,
                Order = order++
            };

            _context.AlbumMedia.Add(albumMedia);

            // Ustaw pierwsze media jako cover jeśli nie ma covera
            if (album.CoverMediaId == null)
            {
                album.CoverMediaId = media.Id;
            }
        }

        await _context.SaveChangesAsync();
        await _albumRepository.UpdateAsync(album);

        // Załaduj album ponownie z AlbumMedias, aby mieć poprawny MediaCount
        album = await _albumRepository.GetByIdWithMediaAsync(album.Id) ?? album;
        
        return MapToDetailDto(album);
    }

    public async Task<bool> RemoveMediaFromAlbumAsync(Guid albumId, Guid mediaId, Guid userId)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(albumId);
        if (album == null || album.UserId != userId)
            return false;

        var albumMedia = await _context.AlbumMedia
            .FirstOrDefaultAsync(am => am.AlbumId == albumId && am.MediaId == mediaId);
        if (albumMedia == null)
            return false;

        _context.AlbumMedia.Remove(albumMedia);

        // Jeśli usuwane media było coverem, ustaw nowy cover
        if (album.CoverMediaId == mediaId)
        {
            var remainingMedia = await _context.AlbumMedia
                .Where(am => am.AlbumId == albumId && am.MediaId != mediaId)
                .FirstOrDefaultAsync();
            album.CoverMediaId = remainingMedia?.MediaId;
        }

        await _albumRepository.UpdateAsync(album);
        await _context.SaveChangesAsync();
        return true;
    }

    private AlbumResponseDto MapToDto(Album album)
    {
        return new AlbumResponseDto
        {
            Id = album.Id,
            Name = album.Name,
            Description = album.Description,
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt,
            CoverMediaId = album.CoverMediaId,
            CoverThumbnailUrl = _storageService.GetThumbnailUrl(album.CoverMedia?.ThumbnailPath),
            MediaCount = album.AlbumMedias?.Count ?? 0
        };
    }

    private AlbumDetailResponseDto MapToDetailDto(Album album)
    {
        var mediaList = album.AlbumMedias?
            .OrderBy(am => am.Order)
            .Select(am => am.Media)
            .Where(m => m != null)
            .Select(m => new MediaResponseDto
            {
                Id = m!.Id,
                FileName = m.FileName,
                FilePath = m.FilePath,
                MediaUrl = _storageService.GetMediaUrl(m.FilePath),
                FileSize = m.FileSize,
                MediaType = m.MediaType.ToString(),
                MimeType = m.MimeType,
                UploadedAt = m.UploadedAt,
                Width = m.Width,
                Height = m.Height,
                Duration = m.Duration,
                ThumbnailPath = m.ThumbnailPath,
                ThumbnailUrl = _storageService.GetThumbnailUrl(m.ThumbnailPath)
            })
            .ToList() ?? new List<MediaResponseDto>();

        return new AlbumDetailResponseDto
        {
            Id = album.Id,
            Name = album.Name,
            Description = album.Description,
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt,
            CoverMediaId = album.CoverMediaId,
            CoverThumbnailUrl = _storageService.GetThumbnailUrl(album.CoverMedia?.ThumbnailPath),
            MediaCount = album.AlbumMedias?.Count ?? 0,
            Media = mediaList
        };
    }
}
