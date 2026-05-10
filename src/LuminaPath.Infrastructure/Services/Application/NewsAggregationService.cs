using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Services.Third_Party;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class NewsAggregationService
{
    private readonly GameNewsService _gameNewsService;

    public NewsAggregationService(GameNewsService gameNewsService)
    {
        _gameNewsService = gameNewsService;
    }

    public Task<IReadOnlyList<GameNewsItemDto>?> GetGameNewsAsync(
        int gameId,
        bool refresh,
        CancellationToken cancellationToken)
    {
        return _gameNewsService.GetNewsAsync(gameId, refresh, cancellationToken);
    }
}
