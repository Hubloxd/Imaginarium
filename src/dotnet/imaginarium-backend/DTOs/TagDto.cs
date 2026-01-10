namespace ImaginariumBackend.DTOs;

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public float Confidence { get; set; }
    public string? Source { get; set; }
}
