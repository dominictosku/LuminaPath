using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Imports;

public sealed partial class GameImportPipeline
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public GameImportPipeline(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<GameImportPreviewResult> PreviewAsync(
        LuminaUser user,
        IEnumerable<GameImportItem> items,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await LoadGames(context)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new GameImportPreviewResult();
        var sequence = 1;
        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            sequence++;
            var rowNumber = item.RowNumber ?? sequence;
            var preview = new GameImportPreviewItem
            {
                RowNumber = rowNumber,
                Name = item.Name,
                Source = item.Source,
                ExternalId = item.ExternalId ?? string.Empty,
            };

            try
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    preview.Error = "Name is required.";
                    result.Errors.Add($"Row {rowNumber}: {preview.Error}");
                    result.Rows.Add(preview);
                    continue;
                }

                var importKey = GetImportKey(item);
                if (!seenRows.Add(importKey))
                {
                    preview.GameAction = "Duplicate";
                    preview.LibraryAction = "Skip";
                    preview.ChangeType = "Duplicate";
                    result.DuplicateRows++;
                    result.RowsDetected++;
                    result.Rows.Add(preview);
                    continue;
                }

                var game = FindGame(games, item);
                if (game is null)
                {
                    preview.GameAction = "Create";
                    preview.LibraryAction = "Create";
                    preview.ChangeType = "New";
                    result.CreatedGames++;
                    result.CreatedMyGames++;
                }
                else
                {
                    preview.GameAction = "Update";
                    preview.ChangeType = "Updated";
                    result.UpdatedGames++;

                    if (game.MyGames?.Any(myGame => myGame.LuminaUserId == user.Id) == true)
                    {
                        preview.LibraryAction = "Update";
                        result.UpdatedMyGames++;
                    }
                    else
                    {
                        preview.LibraryAction = "Create";
                        result.CreatedMyGames++;
                    }
                }

                result.RowsDetected++;
            }
            catch (Exception ex)
            {
                preview.Error = ex.Message;
                preview.ChangeType = "Error";
                result.Errors.Add($"Row {rowNumber}: {ex.Message}");
            }

            result.Rows.Add(preview);
        }

        return result;
    }

    public async Task<GameImportResult> ImportAsync(
        LuminaUser user,
        IEnumerable<GameImportItem> items,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await LoadGames(context).ToListAsync(cancellationToken);
        var names = new HashSet<string>(games.Select(game => game.Name), StringComparer.OrdinalIgnoreCase);
        var result = new GameImportResult();
        var sequence = 1;
        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            sequence++;
            var rowNumber = item.RowNumber ?? sequence;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            try
            {
                if (!seenRows.Add(GetImportKey(item)))
                {
                    result.Errors.Add($"Row {rowNumber}: duplicate import row skipped.");
                    continue;
                }

                var game = FindGame(games, item);
                if (game is null)
                {
                    game = CreateGame(item, names);
                    games.Add(game);
                    context.Games.Add(game);
                    result.CreatedGames++;
                }
                else
                {
                    ApplyGameValues(game, item);
                    result.UpdatedGames++;
                }

                EnsureExternalId(game, item);
                game.MyGames ??= new List<MyGame>();
                var myGame = game.MyGames.FirstOrDefault(entry => entry.LuminaUserId == user.Id);
                if (myGame is null)
                {
                    myGame = new MyGame
                    {
                        Game = game,
                        LuminaUserId = user.Id
                    };
                    game.MyGames.Add(myGame);
                    context.MyGames.Add(myGame);
                    result.CreatedMyGames++;
                }
                else
                {
                    result.UpdatedMyGames++;
                }

                ApplyMyGameValues(myGame, item);
                result.RowsImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            result.Errors.Add($"Database save failed: {ex.InnerException?.Message ?? ex.Message}");
        }

        return result;
    }

}
