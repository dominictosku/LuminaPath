namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class AchievementSyncResult
{
    public int GamesScanned { get; set; }
    public int DefinitionsAdded { get; set; }
    public int DefinitionsUpdated { get; set; }
    public int UnlocksAdded { get; set; }
    public List<string> Warnings { get; set; } = new();

    public int TotalDefinitionsTouched => DefinitionsAdded + DefinitionsUpdated;
}
