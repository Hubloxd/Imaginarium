using ImaginariumBackend.Models;

namespace ImaginariumBackend.DTOs;

public class CreateShareDto
{
    public Guid? MediaId { get; set; }
    public Guid? AlbumId { get; set; }
    public Guid? SharedWithUserId { get; set; }
    public string? SharedWithUserEmail { get; set; }
    public Guid? SharedWithGroupId { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public PermissionEnum PermissionLevel { get; set; } = PermissionEnum.View;
}
