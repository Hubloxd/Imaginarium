using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public interface IMediaRepository
{
    Task<Media?> GetByIdAsync(Guid id);
    Task<List<Media>> GetByUserIdAsync(Guid userId);
    Task<List<Media>> GetByAlbumIdAsync(Guid albumId);
    Task<Media> CreateAsync(Media media);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
