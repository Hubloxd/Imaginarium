using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using ImaginariumBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace ImaginariumBackend.Services;

public class ShareService : IShareService
{
    private readonly IShareRepository _shareRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShareService> _logger;

    public ShareService(
        IShareRepository shareRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IMediaRepository mediaRepository,
        IAlbumRepository albumRepository,
        ApplicationDbContext context,
        ILogger<ShareService> logger)
    {
        _shareRepository = shareRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _mediaRepository = mediaRepository;
        _albumRepository = albumRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<ShareResponseDto> CreateShareAsync(Guid userId, CreateShareDto createDto)
    {
        // Walidacja: musi być MediaId LUB AlbumId
        if (createDto.MediaId == null && createDto.AlbumId == null)
            throw new ArgumentException("Musi być podane MediaId lub AlbumId");

        if (createDto.MediaId != null && createDto.AlbumId != null)
            throw new ArgumentException("Nie można udostępnić jednocześnie Media i Album");

        // Sprawdź czy użytkownik jest właścicielem
        if (createDto.MediaId != null)
        {
            var media = await _mediaRepository.GetByIdAsync(createDto.MediaId.Value);
            if (media == null || media.UserId != userId)
                throw new UnauthorizedAccessException("Nie masz uprawnień do udostępnienia tego media");
        }

        if (createDto.AlbumId != null)
        {
            var album = await _albumRepository.GetByIdAsync(createDto.AlbumId.Value);
            if (album == null || album.UserId != userId)
                throw new UnauthorizedAccessException("Nie masz uprawnień do udostępnienia tego albumu");
        }

        // Konwertuj email na userId jeśli podano email
        Guid? sharedWithUserId = createDto.SharedWithUserId;
        if (sharedWithUserId == null && !string.IsNullOrEmpty(createDto.SharedWithUserEmail))
        {
            var user = await _userRepository.GetByEmailAsync(createDto.SharedWithUserEmail);
            if (user == null)
                throw new ArgumentException("Użytkownik o podanym emailu nie istnieje");
            sharedWithUserId = user.Id;
        }

        // Sprawdź czy już istnieje takie udostępnienie
        var exists = await _shareRepository.ExistsAsync(
            createDto.MediaId, 
            createDto.AlbumId, 
            sharedWithUserId, 
            createDto.SharedWithGroupId);
        
        if (exists)
            throw new InvalidOperationException("To udostępnienie już istnieje");

        // Generuj token
        var shareToken = Guid.NewGuid().ToString("N");

        var share = new Share
        {
            MediaId = createDto.MediaId,
            AlbumId = createDto.AlbumId,
            SharedByUserId = userId,
            SharedWithUserId = sharedWithUserId,
            SharedWithGroupId = createDto.SharedWithGroupId,
            ShareToken = shareToken,
            IsPublic = createDto.IsPublic,
            ExpiresAt = createDto.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            PermissionLevel = createDto.PermissionLevel
        };

        share = await _shareRepository.CreateAsync(share);
        share = await _shareRepository.GetByIdAsync(share.Id) ?? share;

        return MapToDto(share);
    }

    public async Task<ShareResponseDto?> GetShareByTokenAsync(string token)
    {
        var share = await _shareRepository.GetByTokenAsync(token);
        if (share == null)
            return null;

        // Sprawdź czy nie wygasło
        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return MapToDto(share);
    }

    public async Task<bool> ValidateAccessAsync(string token, Guid? userId)
    {
        var share = await _shareRepository.GetByTokenAsync(token);
        if (share == null)
            return false;

        // Sprawdź wygaśnięcie
        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        // Jeśli publiczne, dostęp dla wszystkich
        if (share.IsPublic)
            return true;

        // Sprawdź czy użytkownik jest odbiorcą
        if (userId.HasValue)
        {
            if (share.SharedWithUserId == userId.Value)
                return true;

            // Sprawdź czy użytkownik jest w grupie
            if (share.SharedWithGroupId.HasValue)
            {
                var member = await _groupRepository.GetMemberAsync(share.SharedWithGroupId.Value, userId.Value);
                if (member != null)
                    return true;
            }
        }

        return false;
    }

    public async Task<bool> RevokeShareAsync(Guid shareId, Guid userId)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null)
            return false;

        // Tylko właściciel może cofnąć udostępnienie
        if (share.SharedByUserId != userId)
            return false;

        return await _shareRepository.DeleteAsync(shareId);
    }

    public async Task<ShareResponseDto> ShareWithGroupAsync(Guid userId, CreateShareDto createDto)
    {
        // To jest alias dla CreateShareAsync, ale możemy dodać dodatkową walidację
        if (createDto.SharedWithGroupId == null)
            throw new ArgumentException("SharedWithGroupId jest wymagane dla ShareWithGroup");

        // Sprawdź czy użytkownik jest członkiem grupy
        var member = await _groupRepository.GetMemberAsync(createDto.SharedWithGroupId.Value, userId);
        if (member == null)
            throw new UnauthorizedAccessException("Nie jesteś członkiem tej grupy");

        return await CreateShareAsync(userId, createDto);
    }

    public async Task<List<ShareResponseDto>> GetGroupSharesAsync(Guid groupId)
    {
        var shares = await _shareRepository.GetByGroupIdAsync(groupId);
        return shares.Select(MapToDto).ToList();
    }

    public async Task<List<ShareResponseDto>> GetSharesForUserAsync(Guid userId)
    {
        // Udostępnienia dla użytkownika (gdzie jest odbiorcą)
        var shares = await _shareRepository.GetBySharedWithUserIdAsync(userId);
        
        // Dodaj udostępnienia dla grup, w których użytkownik jest członkiem
        var userGroups = await _groupRepository.GetByUserIdAsync(userId);
        foreach (var group in userGroups)
        {
            var groupShares = await _shareRepository.GetByGroupIdAsync(group.Id);
            shares.AddRange(groupShares);
        }

        return shares.Select(MapToDto).ToList();
    }

    public async Task<List<ShareResponseDto>> GetSharesByUserAsync(Guid userId)
    {
        // Udostępnienia utworzone przez użytkownika
        var shares = await _shareRepository.GetBySharedByUserIdAsync(userId);
        return shares.Select(MapToDto).ToList();
    }

    private ShareResponseDto MapToDto(Share share)
    {
        return new ShareResponseDto
        {
            Id = share.Id,
            MediaId = share.MediaId,
            AlbumId = share.AlbumId,
            SharedByUserId = share.SharedByUserId,
            SharedByUsername = share.SharedByUser?.Username ?? string.Empty,
            SharedWithUserId = share.SharedWithUserId,
            SharedWithUsername = share.SharedWithUser?.Username,
            SharedWithGroupId = share.SharedWithGroupId,
            SharedWithGroupName = share.SharedWithGroup?.Name,
            ShareToken = share.ShareToken,
            IsPublic = share.IsPublic,
            ExpiresAt = share.ExpiresAt,
            CreatedAt = share.CreatedAt,
            PermissionLevel = share.PermissionLevel
        };
    }
}
