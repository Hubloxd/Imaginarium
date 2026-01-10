using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImaginariumBackend.Models;

[Table("Tags")]
public class Tag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [Required]
    public float Confidence { get; set; }

    // Navigation properties
    public ICollection<MediaTag> MediaTags { get; set; } = new List<MediaTag>();
}
