using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImaginariumBackend.Models;

[Table("MediaTags")]
public class MediaTag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MediaId { get; set; }

    [Required]
    public Guid TagId { get; set; }

    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? Source { get; set; } // "AI-classification", "manual", etc.

    // Navigation properties
    [ForeignKey("MediaId")]
    public Media? Media { get; set; }

    [ForeignKey("TagId")]
    public Tag? Tag { get; set; }
}
