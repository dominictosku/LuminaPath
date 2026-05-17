using LuminaPath.Core.Models;

namespace LuminaPath.Infrastructure.Services.Third_Party;

public interface IGameMetadataProvider
{
    string Name { get; }

    Task<GameMetadata?> SearchAsync(Game game, CancellationToken cancellationToken);
}

public sealed record GameMetadata(
    string Provider,
    DateTime? ReleaseDate,
    string? CoverUrl);
