namespace LuminaPath.Core.Dtos;

public sealed class GameNewsItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
}
