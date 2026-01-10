using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id);
    Task<Group?> GetByIdWithMembersAsync(Guid id);
    Task<List<Group>> GetByUserIdAsync(Guid userId);
    Task<List<Group>> GetCreatedByUserIdAsync(Guid userId);
    Task<Group> CreateAsync(Group group);
    Task<Group> UpdateAsync(Group group);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);
    Task<List<GroupMember>> GetMembersAsync(Guid groupId);
    Task<GroupMember> AddMemberAsync(GroupMember member);
    Task<bool> RemoveMemberAsync(Guid groupId, Guid userId);
    Task<bool> UpdateMemberRoleAsync(Guid groupId, Guid userId, GroupRoleEnum role);
    Task<List<GroupMember>> GetPendingInvitationsAsync(Guid userId);
}
