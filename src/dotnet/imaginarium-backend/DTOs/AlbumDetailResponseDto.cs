namespace ImaginariumBackend.DTOs;

public class AlbumDetailResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CoverMediaId { get; set; }
    public string? CoverThumbnailUrl { get; set; }
    public int MediaCount { get; set; }
    public List<MediaResponseDto> Media { get; set; } = new();
}
