using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Services;

public interface IMediaService
{
    Task<Media> UploadMediaAsync(Guid userId, IFormFile file);
    Task<MediaResponseDto?> GetMediaByIdAsync(Guid id, Guid userId);
    Task<List<MediaResponseDto>> GetUserMediaAsync(Guid userId);
    Task<bool> DeleteMediaAsync(Guid id, Guid userId);
}
