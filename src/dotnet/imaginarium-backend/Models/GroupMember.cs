namespace ImaginariumBackend.Models;

public class GroupMember
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public Guid? InvitedByUserId { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? InvitedByUser { get; set; }
}
