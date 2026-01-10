using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public interface IAlbumRepository
{
    Task<Album?> GetByIdAsync(Guid id);
    Task<Album?> GetByIdWithMediaAsync(Guid id);
    Task<List<Album>> GetByUserIdAsync(Guid userId);
    Task<Album> CreateAsync(Album album);
    Task<Album> UpdateAsync(Album album);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
