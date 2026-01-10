using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImaginariumBackend.Models;

[Table("AlbumMedia")]
public class AlbumMedia
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AlbumId { get; set; }

    [Required]
    public Guid MediaId { get; set; }

    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int Order { get; set; }

    // Navigation properties
    [ForeignKey("AlbumId")]
    public Album? Album { get; set; }

    [ForeignKey("MediaId")]
    public Media? Media { get; set; }
}
