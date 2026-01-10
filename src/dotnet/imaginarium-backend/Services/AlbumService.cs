using ImaginariumBackend.Data;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ImaginariumBackend.Services;

public class AlbumService : IAlbumService
{
    private readonly IAlbumRepository _albumRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;
    private readonly IShareRepository _shareRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMediaTagRepository _mediaTagRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlbumService> _logger;

    public AlbumService(
        IAlbumRepository albumRepository,
        IMediaRepository mediaRepository,
        IMediaService mediaService,
        IStorageService storageService,
        IShareRepository shareRepository,
        IGroupRepository groupRepository,
        IMediaTagRepository mediaTagRepository,
        ApplicationDbContext context,
        ILogger<AlbumService> logger)
    {
        _albumRepository = albumRepository;
        _mediaRepository = mediaRepository;
        _mediaService = mediaService;
        _storageService = storageService;
        _shareRepository = shareRepository;
        _groupRepository = groupRepository;
        _mediaTagRepository = mediaTagRepository;
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
        if (album == null)
            return null;

        // Sprawdź czy użytkownik jest właścicielem
        if (album.UserId == userId)
            return MapToDto(album);

        // Sprawdź czy album jest udostępniony użytkownikowi
        if (await HasAccessToAlbumAsync(id, userId))
            return MapToDto(album);

        return null;
    }

    public async Task<AlbumDetailResponseDto?> GetAlbumDetailByIdAsync(Guid id, Guid userId)
    {
        var album = await _albumRepository.GetByIdWithMediaAsync(id);
        if (album == null)
            return null;

        // Sprawdź czy użytkownik jest właścicielem
        if (album.UserId == userId)
            return await MapToDetailDtoAsync(album);

        // Sprawdź czy album jest udostępniony użytkownikowi
        if (await HasAccessToAlbumAsync(id, userId))
            return await MapToDetailDtoAsync(album);

        return null;
    }

    private async Task<bool> HasAccessToAlbumAsync(Guid albumId, Guid userId)
    {
        // Sprawdź udostępnienia bezpośrednio dla użytkownika
        var userShares = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        if (userShares.Any(s => s.AlbumId == albumId && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
            return true;

        // Sprawdź udostępnienia dla grup użytkownika
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            if (groupShares.Any(s => s.AlbumId == albumId && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
                return true;
        }

        // Sprawdź publiczne udostępnienia
        var publicShares = await _context.Shares
            .AnyAsync(s => s.AlbumId == albumId && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow));

        return publicShares;
    }

    private async Task<bool> HasEditPermissionToAlbumAsync(Guid albumId, Guid userId)
    {
        // Sprawdź udostępnienia bezpośrednio dla użytkownika z uprawnieniami Edit
        var userShares = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        if (userShares.Any(s => s.AlbumId == albumId && 
                               s.PermissionLevel == PermissionEnum.Edit && 
                               (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
            return true;

        // Sprawdź udostępnienia dla grup użytkownika z uprawnieniami Edit
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            if (groupShares.Any(s => s.AlbumId == albumId && 
                                   s.PermissionLevel == PermissionEnum.Edit && 
                                   (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow)))
                return true;
        }

        // Sprawdź publiczne udostępnienia z uprawnieniami Edit
        var publicShares = await _context.Shares
            .AnyAsync(s => s.AlbumId == albumId && 
                          s.IsPublic && 
                          s.PermissionLevel == PermissionEnum.Edit && 
                          (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow));

        return publicShares;
    }

    public async Task<List<AlbumResponseDto>> GetUserAlbumsAsync(Guid userId)
    {
        // Pobierz albumy użytkownika
        var userAlbums = await _albumRepository.GetByUserIdAsync(userId);
        var albumIds = new HashSet<Guid>(userAlbums.Select(a => a.Id));

        // Pobierz albumy udostępnione bezpośrednio użytkownikowi
        var sharesForUser = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        var sharedAlbumIds = sharesForUser
            .Where(s => s.AlbumId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.AlbumId!.Value)
            .ToList();

        // Pobierz albumy udostępnione grupom, w których użytkownik jest członkiem
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            var groupAlbumIds = groupShares
                .Where(s => s.AlbumId.HasValue && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
                .Select(s => s.AlbumId!.Value);
            sharedAlbumIds.AddRange(groupAlbumIds);
        }

        // Pobierz albumy udostępnione publicznie
        var publicShares = await _context.Shares
            .Where(s => s.AlbumId != null && s.IsPublic && (!s.ExpiresAt.HasValue || s.ExpiresAt.Value > DateTime.UtcNow))
            .Select(s => s.AlbumId!.Value)
            .ToListAsync();

        sharedAlbumIds.AddRange(publicShares);

        // Pobierz wszystkie udostępnione albumy (które nie są już w liście użytkownika)
        var uniqueSharedAlbumIds = sharedAlbumIds.Distinct().Where(id => !albumIds.Contains(id)).ToList();
        
        var sharedAlbums = new List<Album>();
        if (uniqueSharedAlbumIds.Any())
        {
            sharedAlbums = await _context.Albums
                .Include(a => a.CoverMedia)
                .Include(a => a.AlbumMedias)
                .Where(a => uniqueSharedAlbumIds.Contains(a.Id))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // Połącz albumy użytkownika z udostępnionymi
        var allAlbums = userAlbums.Concat(sharedAlbums).OrderByDescending(a => a.CreatedAt).ToList();
        
        return allAlbums.Select(MapToDto).ToList();
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
        if (album == null)
            return null;

        // Sprawdź czy użytkownik jest właścicielem
        bool isOwner = album.UserId == userId;
        
        // Jeśli nie jest właścicielem, sprawdź uprawnienia z udostępnienia
        if (!isOwner)
        {
            var hasEditPermission = await HasEditPermissionToAlbumAsync(albumId, userId);
            if (!hasEditPermission)
                return null;
        }

        if (files == null || files.Count == 0)
            return await MapToDetailDtoAsync(album);

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
        
        return await MapToDetailDtoAsync(album);
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

    private async Task<AlbumDetailResponseDto> MapToDetailDtoAsync(Album album)
    {
        var mediaList = new List<MediaResponseDto>();
        
        if (album.AlbumMedias != null)
        {
            var orderedMedia = album.AlbumMedias
                .OrderBy(am => am.Order)
                .Select(am => am.Media)
                .Where(m => m != null)
                .ToList();

            foreach (var media in orderedMedia)
            {
                if (media == null) continue;

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

                mediaList.Add(new MediaResponseDto
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
                });
            }
        }

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
