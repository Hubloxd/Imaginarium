using System.ComponentModel.DataAnnotations;

namespace ImaginariumBackend.DTOs;

public class CreateAlbumDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
