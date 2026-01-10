namespace ImaginariumBackend.Models;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPrivate { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public List<GroupMember> GroupMembers { get; set; } = new();
}
