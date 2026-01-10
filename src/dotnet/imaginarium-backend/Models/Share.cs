namespace ImaginariumBackend.Models;

public class Share
{
    public Guid Id { get; set; }
    public Guid? MediaId { get; set; }
    public Guid? AlbumId { get; set; }
    public Guid SharedByUserId { get; set; }
    public Guid? SharedWithUserId { get; set; }
    public Guid? SharedWithGroupId { get; set; }
    public string ShareToken { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public PermissionEnum PermissionLevel { get; set; }

    // Navigation properties
    public Media? Media { get; set; }
    public Album? Album { get; set; }
    public User SharedByUser { get; set; } = null!;
    public User? SharedWithUser { get; set; }
    public Group? SharedWithGroup { get; set; }
}
