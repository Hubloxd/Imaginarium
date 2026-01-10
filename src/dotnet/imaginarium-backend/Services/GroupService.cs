using ImaginariumBackend.DTOs;
using ImaginariumBackend.Models;
using ImaginariumBackend.Repositories;
using ImaginariumBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace ImaginariumBackend.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        ApplicationDbContext context,
        ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<GroupResponseDto> CreateGroupAsync(Guid userId, CreateGroupDto createDto)
    {
        var group = new Group
        {
            Name = createDto.Name,
            Description = createDto.Description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPrivate = createDto.IsPrivate
        };

        group = await _groupRepository.CreateAsync(group);

        // Dodaj twórcę jako admina grupy
        var creatorMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRoleEnum.Admin,
            JoinedAt = DateTime.UtcNow,
            InvitedByUserId = null
        };

        await _groupRepository.AddMemberAsync(creatorMember);

        // Załaduj grupę z członkami
        group = await _groupRepository.GetByIdWithMembersAsync(group.Id) ?? group;
        
        return MapToDto(group, userId);
    }

    public async Task<GroupResponseDto?> GetGroupByIdAsync(Guid id, Guid userId)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(id);
        if (group == null)
            return null;

        // Sprawdź czy użytkownik jest członkiem grupy
        var isMember = group.GroupMembers.Any(gm => gm.UserId == userId);
        if (!isMember && group.IsPrivate)
            return null;

        return MapToDto(group, userId);
    }

    public async Task<List<GroupResponseDto>> GetUserGroupsAsync(Guid userId)
    {
        var groups = await _groupRepository.GetByUserIdAsync(userId);
        return groups.Select(g => MapToDto(g, userId)).ToList();
    }

    public async Task<List<GroupMemberDto>> GetPendingInvitationsAsync(Guid userId)
    {
        var invitations = await _groupRepository.GetPendingInvitationsAsync(userId);
        return invitations.Select(MapMemberToDto).ToList();
    }

    public async Task<bool> DeleteGroupAsync(Guid id, Guid userId)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null)
            return false;

        // Tylko twórca może usunąć grupę
        if (group.CreatedByUserId != userId)
            return false;

        return await _groupRepository.DeleteAsync(id);
    }

    public async Task<bool> InviteUserAsync(Guid groupId, Guid inviterUserId, InviteUserDto inviteDto)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
        if (group == null)
            return false;

        // Sprawdź czy użytkownik zapraszający jest członkiem grupy
        var inviterMember = group.GroupMembers.FirstOrDefault(gm => gm.UserId == inviterUserId);
        if (inviterMember == null || inviterMember.Role == GroupRoleEnum.Viewer)
            return false; // Tylko Admin i Member mogą zapraszać

        // Znajdź użytkownika po emailu
        var user = await _userRepository.GetByEmailAsync(inviteDto.Email);
        if (user == null)
            return false;

        // Sprawdź czy użytkownik już jest członkiem
        var existingMember = await _groupRepository.GetMemberAsync(groupId, user.Id);
        if (existingMember != null)
            return false;

        // Utwórz zaproszenie (GroupMember z InvitedByUserId)
        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = user.Id,
            Role = inviteDto.Role,
            JoinedAt = DateTime.UtcNow, // Dla uproszczenia ustawiamy JoinedAt, można dodać flagę IsAccepted
            InvitedByUserId = inviterUserId
        };

        await _groupRepository.AddMemberAsync(member);
        return true;
    }

    public async Task<bool> AcceptInvitationAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null || member.InvitedByUserId == null)
            return false;

        // Zaproszenie już jest zaakceptowane (JoinedAt jest ustawione)
        // Można dodać flagę IsAccepted jeśli potrzebne
        return true;
    }

    public async Task<bool> RejectInvitationAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository.RemoveMemberAsync(groupId, userId);
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid memberUserId, Guid requesterUserId)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
        if (group == null)
            return false;

        // Sprawdź uprawnienia
        var requesterMember = group.GroupMembers.FirstOrDefault(gm => gm.UserId == requesterUserId);
        if (requesterMember == null || requesterMember.Role != GroupRoleEnum.Admin)
            return false;

        // Nie można usunąć samego siebie
        if (memberUserId == requesterUserId)
            return false;

        return await _groupRepository.RemoveMemberAsync(groupId, memberUserId);
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid groupId, Guid memberUserId, GroupRoleEnum role, Guid requesterUserId)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
        if (group == null)
            return false;

        // Tylko Admin może zmieniać role
        var requesterMember = group.GroupMembers.FirstOrDefault(gm => gm.UserId == requesterUserId);
        if (requesterMember == null || requesterMember.Role != GroupRoleEnum.Admin)
            return false;

        return await _groupRepository.UpdateMemberRoleAsync(groupId, memberUserId, role);
    }

    public async Task<bool> LeaveGroupAsync(Guid groupId, Guid userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            return false;

        // Nie można opuścić grupy jeśli jest się jej twórcą
        if (group.CreatedByUserId == userId)
            return false;

        return await _groupRepository.RemoveMemberAsync(groupId, userId);
    }

    public async Task<List<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid userId)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
        if (group == null)
            return new List<GroupMemberDto>();

        // Sprawdź czy użytkownik jest członkiem
        var isMember = group.GroupMembers.Any(gm => gm.UserId == userId);
        if (!isMember && group.IsPrivate)
            return new List<GroupMemberDto>();

        var members = await _groupRepository.GetMembersAsync(groupId);
        return members.Select(MapMemberToDto).ToList();
    }

    private GroupResponseDto MapToDto(Group group, Guid currentUserId)
    {
        var userMember = group.GroupMembers.FirstOrDefault(gm => gm.UserId == currentUserId);
        
        return new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedByUserId = group.CreatedByUserId,
            CreatedByUsername = group.CreatedByUser?.Username ?? string.Empty,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            IsPrivate = group.IsPrivate,
            MemberCount = group.GroupMembers?.Count ?? 0,
            UserRole = userMember?.Role
        };
    }

    private GroupMemberDto MapMemberToDto(GroupMember member)
    {
        return new GroupMemberDto
        {
            Id = member.Id,
            GroupId = member.GroupId,
            GroupName = member.Group?.Name ?? string.Empty,
            UserId = member.UserId,
            Username = member.User?.Username ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            InvitedByUserId = member.InvitedByUserId,
            InvitedByUsername = member.InvitedByUser?.Username
        };
    }
}
