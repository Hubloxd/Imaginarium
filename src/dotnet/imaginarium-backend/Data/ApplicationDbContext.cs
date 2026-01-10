using Microsoft.EntityFrameworkCore;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Album> Albums { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<AlbumMedia> AlbumMedia { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Album configuration
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.CoverMedia)
                  .WithMany()
                  .HasForeignKey(e => e.CoverMediaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Media configuration
        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MediaType);
        });

        // AlbumMedia configuration (many-to-many)
        modelBuilder.Entity<AlbumMedia>(entity =>
        {
            entity.HasIndex(e => new { e.AlbumId, e.MediaId }).IsUnique();
            entity.HasIndex(e => e.AlbumId);
            entity.HasIndex(e => e.MediaId);
        });
    }
}
