using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public interface IMediaTagRepository
{
    Task<MediaTag?> GetByIdAsync(Guid id);
    Task<List<MediaTag>> GetByMediaIdAsync(Guid mediaId);
    Task<List<MediaTag>> GetByTagIdAsync(Guid tagId);
    Task<MediaTag> CreateAsync(MediaTag mediaTag);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid mediaId, Guid tagId);
}
