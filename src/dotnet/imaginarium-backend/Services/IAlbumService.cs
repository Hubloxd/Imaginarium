using ImaginariumBackend.DTOs;

namespace ImaginariumBackend.Services;

public interface IAlbumService
{
    Task<AlbumResponseDto> CreateAlbumAsync(Guid userId, CreateAlbumDto createDto, List<IFormFile> files);
    Task<AlbumResponseDto?> GetAlbumByIdAsync(Guid id, Guid userId);
    Task<AlbumDetailResponseDto?> GetAlbumDetailByIdAsync(Guid id, Guid userId);
    Task<List<AlbumResponseDto>> GetUserAlbumsAsync(Guid userId);
    Task<bool> DeleteAlbumAsync(Guid id, Guid userId);
    Task<bool> AddMediaToAlbumAsync(Guid albumId, Guid mediaId, Guid userId);
    Task<AlbumDetailResponseDto?> AddFilesToAlbumAsync(Guid albumId, Guid userId, List<IFormFile> files);
    Task<bool> RemoveMediaFromAlbumAsync(Guid albumId, Guid mediaId, Guid userId);
}
