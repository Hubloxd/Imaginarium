using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Data;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly ApplicationDbContext _context;

    public MediaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Media?> GetByIdAsync(Guid id)
    {
        return await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Media>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Media
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<Media>> GetByAlbumIdAsync(Guid albumId)
    {
        return await _context.Media
            .Where(m => m.AlbumMedias.Any(am => am.AlbumId == albumId))
            .OrderBy(m => m.AlbumMedias.First(am => am.AlbumId == albumId).Order)
            .ToListAsync();
    }

    public async Task<Media> CreateAsync(Media media)
    {
        _context.Media.Add(media);
        await _context.SaveChangesAsync();
        return media;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var media = await _context.Media.FindAsync(id);
        if (media == null)
            return false;

        _context.Media.Remove(media);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Media.AnyAsync(m => m.Id == id);
    }
}
