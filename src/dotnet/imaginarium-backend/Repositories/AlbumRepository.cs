using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Data;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Repositories;

public class AlbumRepository : IAlbumRepository
{
    private readonly ApplicationDbContext _context;

    public AlbumRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Album?> GetByIdAsync(Guid id)
    {
        return await _context.Albums
            .Include(a => a.CoverMedia)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Album?> GetByIdWithMediaAsync(Guid id)
    {
        return await _context.Albums
            .Include(a => a.CoverMedia)
            .Include(a => a.AlbumMedias)
                .ThenInclude(am => am.Media)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Album>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Albums
            .Include(a => a.CoverMedia)
            .Include(a => a.AlbumMedias)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Album> CreateAsync(Album album)
    {
        _context.Albums.Add(album);
        await _context.SaveChangesAsync();
        return album;
    }

    public async Task<Album> UpdateAsync(Album album)
    {
        album.UpdatedAt = DateTime.UtcNow;
        _context.Albums.Update(album);
        await _context.SaveChangesAsync();
        return album;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album == null)
            return false;

        _context.Albums.Remove(album);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Albums.AnyAsync(a => a.Id == id);
    }
}
