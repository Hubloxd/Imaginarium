using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImaginariumBackend.Models;

[Table("Media")]
public class Media
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    public MediaTypeEnum MediaType { get; set; }

    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? Duration { get; set; }

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ICollection<AlbumMedia> AlbumMedias { get; set; } = new List<AlbumMedia>();
    public ICollection<MediaTag> MediaTags { get; set; } = new List<MediaTag>();
}
