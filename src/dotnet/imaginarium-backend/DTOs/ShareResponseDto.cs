using ImaginariumBackend.Models;

namespace ImaginariumBackend.DTOs;

public class ShareResponseDto
{
    public Guid Id { get; set; }
    public Guid? MediaId { get; set; }
    public Guid? AlbumId { get; set; }
    public Guid SharedByUserId { get; set; }
    public string SharedByUsername { get; set; } = string.Empty;
    public Guid? SharedWithUserId { get; set; }
    public string? SharedWithUsername { get; set; }
    public Guid? SharedWithGroupId { get; set; }
    public string? SharedWithGroupName { get; set; }
    public string ShareToken { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public PermissionEnum PermissionLevel { get; set; }
}
