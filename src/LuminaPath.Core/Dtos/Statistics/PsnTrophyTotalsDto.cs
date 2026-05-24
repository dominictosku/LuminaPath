namespace LuminaPath.Core.Dtos.Statistics;

/// <summary>
/// Per-user PlayStation Network trophy counts, grouped by tier.
/// Computed from <c>UserGameAchievement</c> joined with <c>GameAchievement.PsnTrophyType</c> —
/// i.e. only unlocked trophies originally synced from PSN are counted.
///
/// All four tier counts and the total are always present (zero when empty)
/// so the client can render the trophy panel without null-checking each field.
/// </summary>
public sealed record PsnTrophyTotalsDto(
    int Bronze,
    int Silver,
    int Gold,
    int Platinum,
    int Total);
