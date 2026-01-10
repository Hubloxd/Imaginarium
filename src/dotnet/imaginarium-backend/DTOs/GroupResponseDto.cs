using ImaginariumBackend.Models;

namespace ImaginariumBackend.DTOs;

public class GroupResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPrivate { get; set; }
    public int MemberCount { get; set; }
    public GroupRoleEnum? UserRole { get; set; }
}

public class GroupMemberDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public GroupRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public Guid? InvitedByUserId { get; set; }
    public string? InvitedByUsername { get; set; }
}

public class InviteUserDto
{
    public string Email { get; set; } = string.Empty;
    public GroupRoleEnum Role { get; set; } = GroupRoleEnum.Member;
}
