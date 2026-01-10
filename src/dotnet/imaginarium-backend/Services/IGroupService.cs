using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Services;

public interface IGroupService
{
    Task<GroupResponseDto> CreateGroupAsync(Guid userId, CreateGroupDto createDto);
    Task<GroupResponseDto?> GetGroupByIdAsync(Guid id, Guid userId);
    Task<List<GroupResponseDto>> GetUserGroupsAsync(Guid userId);
    Task<List<GroupMemberDto>> GetPendingInvitationsAsync(Guid userId);
    Task<bool> DeleteGroupAsync(Guid id, Guid userId);
    Task<bool> InviteUserAsync(Guid groupId, Guid inviterUserId, InviteUserDto inviteDto);
    Task<bool> AcceptInvitationAsync(Guid groupId, Guid userId);
    Task<bool> RejectInvitationAsync(Guid groupId, Guid userId);
    Task<bool> RemoveMemberAsync(Guid groupId, Guid memberUserId, Guid requesterUserId);
    Task<bool> UpdateMemberRoleAsync(Guid groupId, Guid memberUserId, GroupRoleEnum role, Guid requesterUserId);
    Task<bool> LeaveGroupAsync(Guid groupId, Guid userId);
    Task<List<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid userId);
}
