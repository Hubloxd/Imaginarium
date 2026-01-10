using ImaginariumBackend.DTOs;

namespace ImaginariumBackend.Services;

public interface IShareService
{
    Task<ShareResponseDto> CreateShareAsync(Guid userId, CreateShareDto createDto);
    Task<ShareResponseDto?> GetShareByTokenAsync(string token);
    Task<bool> ValidateAccessAsync(string token, Guid? userId);
    Task<bool> RevokeShareAsync(Guid shareId, Guid userId);
    Task<ShareResponseDto> ShareWithGroupAsync(Guid userId, CreateShareDto createDto);
    Task<List<ShareResponseDto>> GetGroupSharesAsync(Guid groupId);
    Task<List<ShareResponseDto>> GetSharesForUserAsync(Guid userId);
    Task<List<ShareResponseDto>> GetSharesByUserAsync(Guid userId);
}
