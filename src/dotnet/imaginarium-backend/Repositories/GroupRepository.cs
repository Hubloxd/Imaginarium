using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Data;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly ApplicationDbContext _context;

    public GroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Group?> GetByIdAsync(Guid id)
    {
        return await _context.Groups
            .Include(g => g.CreatedByUser)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Group?> GetByIdWithMembersAsync(Guid id)
    {
        return await _context.Groups
            .Include(g => g.CreatedByUser)
            .Include(g => g.GroupMembers)
                .ThenInclude(gm => gm.User)
            .Include(g => g.GroupMembers)
                .ThenInclude(gm => gm.InvitedByUser)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Group>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Groups
            .Include(g => g.CreatedByUser)
            .Include(g => g.GroupMembers)
            .Where(g => g.GroupMembers.Any(gm => gm.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Group>> GetCreatedByUserIdAsync(Guid userId)
    {
        return await _context.Groups
            .Include(g => g.CreatedByUser)
            .Where(g => g.CreatedByUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Group> CreateAsync(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<Group> UpdateAsync(Group group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
            return false;

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Groups.AnyAsync(g => g.Id == id);
    }

    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User)
            .Include(gm => gm.InvitedByUser)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<List<GroupMember>> GetMembersAsync(Guid groupId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User)
            .Include(gm => gm.InvitedByUser)
            .Where(gm => gm.GroupId == groupId)
            .OrderBy(gm => gm.JoinedAt)
            .ToListAsync();
    }

    public async Task<GroupMember> AddMemberAsync(GroupMember member)
    {
        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        
        if (member == null)
            return false;

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid groupId, Guid userId, GroupRoleEnum role)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        
        if (member == null)
            return false;

        member.Role = role;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<GroupMember>> GetPendingInvitationsAsync(Guid userId)
    {
        // Zaproszenia to GroupMembers gdzie użytkownik został zaproszony (InvitedByUserId != null)
        // Dla uproszczenia, wszystkie GroupMembers z InvitedByUserId są traktowane jako zaproszenia
        // W przyszłości można dodać flagę IsAccepted lub sprawdzać datę JoinedAt
        return await _context.GroupMembers
            .Include(gm => gm.Group)
                .ThenInclude(g => g.CreatedByUser)
            .Include(gm => gm.InvitedByUser)
            .Where(gm => gm.UserId == userId && gm.InvitedByUserId != null)
            .OrderByDescending(gm => gm.JoinedAt)
            .ToListAsync();
    }
}
