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
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Share> Shares { get; set; }

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

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // GroupMember configuration
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.InvitedByUserId);
            
            entity.HasOne(e => e.Group)
                  .WithMany(g => g.GroupMembers)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.InvitedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Share configuration
        modelBuilder.Entity<Share>(entity =>
        {
            entity.HasIndex(e => e.ShareToken).IsUnique();
            entity.HasIndex(e => e.MediaId);
            entity.HasIndex(e => e.AlbumId);
            entity.HasIndex(e => e.SharedByUserId);
            entity.HasIndex(e => e.SharedWithUserId);
            entity.HasIndex(e => e.SharedWithGroupId);
            
            entity.HasOne(e => e.Media)
                  .WithMany()
                  .HasForeignKey(e => e.MediaId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Album)
                  .WithMany()
                  .HasForeignKey(e => e.AlbumId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.SharedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.SharedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.SharedWithUser)
                  .WithMany()
                  .HasForeignKey(e => e.SharedWithUserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.SharedWithGroup)
                  .WithMany()
                  .HasForeignKey(e => e.SharedWithGroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
