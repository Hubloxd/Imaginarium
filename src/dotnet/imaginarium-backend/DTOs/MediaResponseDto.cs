namespace ImaginariumBackend.DTOs;

public class MediaResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}
