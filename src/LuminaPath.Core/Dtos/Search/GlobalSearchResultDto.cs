namespace LuminaPath.Core.Dtos;

public class GlobalSearchResultDto
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? MatchedText { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = "search-outline";
    public string? MediaKind { get; set; }
    public int? MediaId { get; set; }
    public int? LibraryEntryId { get; set; }
    public int? QuestId { get; set; }
}
