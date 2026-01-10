using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Data;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public class MediaTagRepository : IMediaTagRepository
{
    private readonly ApplicationDbContext _context;

    public MediaTagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaTag?> GetByIdAsync(Guid id)
    {
        return await _context.MediaTags
            .Include(mt => mt.Media)
            .Include(mt => mt.Tag)
            .FirstOrDefaultAsync(mt => mt.Id == id);
    }

    public async Task<List<MediaTag>> GetByMediaIdAsync(Guid mediaId)
    {
        return await _context.MediaTags
            .Include(mt => mt.Tag)
            .Where(mt => mt.MediaId == mediaId)
            .OrderByDescending(mt => mt.AddedAt)
            .ToListAsync();
    }

    public async Task<List<MediaTag>> GetByTagIdAsync(Guid tagId)
    {
        return await _context.MediaTags
            .Include(mt => mt.Media)
            .Where(mt => mt.TagId == tagId)
            .OrderByDescending(mt => mt.AddedAt)
            .ToListAsync();
    }

    public async Task<MediaTag> CreateAsync(MediaTag mediaTag)
    {
        _context.MediaTags.Add(mediaTag);
        await _context.SaveChangesAsync();
        return mediaTag;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var mediaTag = await _context.MediaTags.FindAsync(id);
        if (mediaTag == null)
            return false;

        _context.MediaTags.Remove(mediaTag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid mediaId, Guid tagId)
    {
        return await _context.MediaTags
            .AnyAsync(mt => mt.MediaId == mediaId && mt.TagId == tagId);
    }
}
