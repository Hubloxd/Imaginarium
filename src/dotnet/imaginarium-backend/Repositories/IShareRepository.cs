using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public interface IShareRepository
{
    Task<Share?> GetByIdAsync(Guid id);
    Task<Share?> GetByTokenAsync(string token);
    Task<Share> CreateAsync(Share share);
    Task<bool> DeleteAsync(Guid id);
    Task<List<Share>> GetByGroupIdAsync(Guid groupId);
    Task<List<Share>> GetByMediaIdAsync(Guid mediaId);
    Task<List<Share>> GetByAlbumIdAsync(Guid albumId);
    Task<List<Share>> GetBySharedWithUserIdAsync(Guid userId);
    Task<List<Share>> GetBySharedByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid? mediaId, Guid? albumId, Guid? sharedWithUserId, Guid? sharedWithGroupId);
}
