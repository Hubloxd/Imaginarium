using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Data;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public class ShareRepository : IShareRepository
{
    private readonly ApplicationDbContext _context;

    public ShareRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Share?> GetByIdAsync(Guid id)
    {
        return await _context.Shares
            .Include(s => s.Media)
            .Include(s => s.Album)
            .Include(s => s.SharedByUser)
            .Include(s => s.SharedWithUser)
            .Include(s => s.SharedWithGroup)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Share?> GetByTokenAsync(string token)
    {
        return await _context.Shares
            .Include(s => s.Media)
            .Include(s => s.Album)
            .Include(s => s.SharedByUser)
            .Include(s => s.SharedWithUser)
            .Include(s => s.SharedWithGroup)
            .FirstOrDefaultAsync(s => s.ShareToken == token);
    }

    public async Task<Share> CreateAsync(Share share)
    {
        _context.Shares.Add(share);
        await _context.SaveChangesAsync();
        return share;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var share = await _context.Shares.FindAsync(id);
        if (share == null)
            return false;

        _context.Shares.Remove(share);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Share>> GetByGroupIdAsync(Guid groupId)
    {
        return await _context.Shares
            .Include(s => s.Media)
            .Include(s => s.Album)
            .Include(s => s.SharedByUser)
            .Where(s => s.SharedWithGroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<Share>> GetByMediaIdAsync(Guid mediaId)
    {
        return await _context.Shares
            .Include(s => s.SharedByUser)
            .Include(s => s.SharedWithUser)
            .Include(s => s.SharedWithGroup)
            .Where(s => s.MediaId == mediaId)
            .ToListAsync();
    }

    public async Task<List<Share>> GetByAlbumIdAsync(Guid albumId)
    {
        return await _context.Shares
            .Include(s => s.SharedByUser)
            .Include(s => s.SharedWithUser)
            .Include(s => s.SharedWithGroup)
            .Where(s => s.AlbumId == albumId)
            .ToListAsync();
    }

    public async Task<List<Share>> GetBySharedWithUserIdAsync(Guid userId)
    {
        return await _context.Shares
            .Include(s => s.Media)
            .Include(s => s.Album)
            .Include(s => s.SharedByUser)
            .Where(s => s.SharedWithUserId == userId)
            .ToListAsync();
    }

    public async Task<List<Share>> GetBySharedByUserIdAsync(Guid userId)
    {
        return await _context.Shares
            .Include(s => s.Media)
            .Include(s => s.Album)
            .Include(s => s.SharedWithUser)
            .Include(s => s.SharedWithGroup)
            .Where(s => s.SharedByUserId == userId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid? mediaId, Guid? albumId, Guid? sharedWithUserId, Guid? sharedWithGroupId)
    {
        return await _context.Shares
            .AnyAsync(s => s.MediaId == mediaId && 
                          s.AlbumId == albumId && 
                          s.SharedWithUserId == sharedWithUserId && 
                          s.SharedWithGroupId == sharedWithGroupId);
    }
}
