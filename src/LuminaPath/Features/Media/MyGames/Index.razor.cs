using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Features.Media.MyGames.Components;
using LuminaPath.Infrastructure.Services.ModelServices;
using MudBlazor;

namespace LuminaPath.Features.Media.MyGames;

public partial class Index
{
    private readonly string _placeholderCover = "/Images/Placeholder.png";
    private List<MyGame> Games = new();
    private List<Game> DropdownGames = new();
    private MudDataGrid<MyGame> _table = default!;
    protected override string EntityLabel => "My Game";
    private string ViewIcon => IsGrid ? Icons.Material.Filled.ViewModule : Icons.Material.Filled.TableRows;

    protected override async Task OnInitializedAsync()
    {
        DropdownGames = await gameService.GetDropdownGames();
        await base.OnInitializedAsync();
    }

    public override async Task ReloadData()
    {
        if (IsGrid)
        {
            await _table.ReloadServerData();
        }
        else
        {
            Games = await GetData();
            StateHasChanged();
        }
    }

    private async Task SwitchView()
    {
        IsGrid = !IsGrid;
        await ReloadData();
    }

    private async Task<List<MyGame>> GetData()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return [];
        }

        var result = await ModelService.GetMyMedia(UserId);
        result = ApplyFilters(result).ToList();
        Games = result;
        return result;
    }

    private IEnumerable<MyGame> ApplyFilters(IEnumerable<MyGame> games)
    {
        if (!string.IsNullOrWhiteSpace(_filter.SearchString))
        {
            var search = _filter.SearchString.Trim();
            games = games.Where(myGame =>
                Contains(myGame.Game?.Name, search) ||
                Contains(myGame.Game?.Description, search) ||
                Contains(myGame.Game?.Source, search) ||
                myGame.Game?.Genres.Any(genre => Contains(genre, search)) == true ||
                myGame.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (_filter.From is not null)
        {
            games = games.Where(myGame => myGame.Game?.ReleaseDate >= _filter.From);
        }

        if (_filter.To is not null)
        {
            games = games.Where(myGame => myGame.Game?.ReleaseDate <= _filter.To);
        }

        if (_filter.Platform is not null)
        {
            games = games.Where(myGame => myGame.Game?.Platforms.HasFlag(_filter.Platform.Value) == true);
        }

        if (_filter.Status is not null)
        {
            games = games.Where(myGame => myGame.Status == _filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(_filter.Genre))
        {
            var genreFilter = _filter.Genre.Trim();
            games = games.Where(myGame => myGame.Game?.Genres.Any(genre => Contains(genre, genreFilter)) == true);
        }

        if (!string.IsNullOrWhiteSpace(_filter.Source))
        {
            var sourceFilter = _filter.Source.Trim();
            games = games.Where(myGame => Contains(myGame.Game?.Source, sourceFilter));
        }

        if (_filter.MinPlaytime is not null)
        {
            games = games.Where(myGame => myGame.Game?.Playtime >= _filter.MinPlaytime);
        }

        if (_filter.MaxPlaytime is not null)
        {
            games = games.Where(myGame => myGame.Game?.Playtime <= _filter.MaxPlaytime);
        }

        if (_filter.Priority is not null)
        {
            games = games.Where(myGame => myGame.Priority == _filter.Priority);
        }

        if (_filter.MinRating is not null)
        {
            games = games.Where(myGame => myGame.Rating >= _filter.MinRating);
        }

        if (_filter.MinPlayedHours is not null)
        {
            games = games.Where(myGame => TotalHours(myGame) >= _filter.MinPlayedHours);
        }

        if (_filter.OnlyWithRemainingHours)
        {
            games = games.Where(myGame => RemainingFor(myGame) > 0);
        }

        return games;
    }

    public override async Task<MyGame> GetById(int id)
    {
        return await ModelService.GetById(id, ModelService.Includes);
    }

    public override async Task Save(MyGame entity)
    {
        await ModelService.PostAsync(entity, User);
    }

    public override async Task Update(MyGame entity)
    {
        await ModelService.PutAsync(entity, User);
    }

    public override async Task DeleteMedia(MyGame entity)
    {
        await ModelService.DeleteAsync(entity.Id);
    }

    protected override string GetDeleteTitle(MyGame myGame) => $"Remove \"{myGame.Game?.Name ?? "this game"}\" from your library?";

    protected override string GetDeleteMessage(MyGame myGame)
    {
        return "Are you sure you want to remove this game from your personal library?";
    }

    protected override string? GetDeleteDetail(MyGame myGame)
    {
        return "This deletes your personal tracking data for this game, including status, priority, rating, played time, and notes. Linked quests and sessions are kept, but they will no longer point to this library entry. The shared game catalog record stays available.";
    }

    protected override string GetBulkDeleteTitle(int count) => $"Remove {count} selected game{(count == 1 ? string.Empty : "s")} from your library?";

    protected override string GetBulkDeleteMessage(int count) => $"Are you sure you want to remove {count} selected game{(count == 1 ? string.Empty : "s")} from your personal library?";

    protected override string? GetBulkDeleteDetail(int count)
    {
        return "This deletes the selected personal tracking entries. Shared catalog games remain available, and linked quests or sessions are detached instead of deleted.";
    }

    private string Cover(MyGame? myGame)
    {
        return string.IsNullOrWhiteSpace(myGame?.Game?.Image?.Url)
            ? _placeholderCover
            : NormalizeUrl(myGame.Game.Image.Url);
    }

    private static string NormalizeUrl(string url)
    {
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("/")
            ? url
            : $"/{url}";
    }

    private static string Summary(MyGame myGame)
    {
        var game = myGame.Game;
        if (game is null)
        {
            return "No game details";
        }

        if (string.IsNullOrWhiteSpace(game.Description))
        {
            return game.Genres.Count > 0 ? string.Join(" / ", game.Genres.Take(3)) : "No description yet";
        }

        return game.Description.Length > 110 ? $"{game.Description[..110]}..." : game.Description;
    }

    private static double ManualHours(MyGame myGame)
    {
        return myGame.TimeSpend ?? 0;
    }

    private static double ThirdPartyHours(MyGame myGame)
    {
        return myGame.MyGameInfo?.TrackedHours ?? 0;
    }

    private static double TotalHours(MyGame myGame)
    {
        return ManualHours(myGame) + ThirdPartyHours(myGame);
    }

    private static double RemainingFor(MyGame myGame)
    {
        var estimate = myGame.Game?.Playtime ?? 0;
        return Math.Max(0, estimate - TotalHours(myGame));
    }

    private static string FormatHours(double hours)
    {
        return hours <= 0 ? "0" : hours.ToString("0.##");
    }

    private static string Rating(MyGame myGame)
    {
        return myGame.Rating is null ? "Unrated" : $"{myGame.Rating}/10";
    }

    private static string DateText(DateTime? date)
    {
        return date?.ToString("dd.MM.yyyy") ?? "No date";
    }

    private static string DateRange(MyGame myGame)
    {
        if (myGame.StartDate is null && myGame.EndDate is null)
        {
            return "No dates";
        }

        return $"{DateText(myGame.StartDate)} - {DateText(myGame.EndDate)}";
    }

    private static string PlatformLabel(MyGame myGame)
    {
        return myGame.Game?.Platforms == 0 || myGame.Game?.Platforms is null
            ? "No platform"
            : myGame.Game.Platforms.ToString();
    }

    private static int ProgressPercent(MyGame myGame)
    {
        var estimate = myGame.Game?.Playtime ?? 0;
        if (estimate <= 0)
        {
            return TotalHours(myGame) > 0 ? 100 : 0;
        }

        return Math.Clamp((int)Math.Round(TotalHours(myGame) / estimate * 100), 0, 100);
    }

    private static string ProgressWidth(MyGame myGame)
    {
        return $"{ProgressPercent(myGame)}%";
    }

    private static string RemainingText(MyGame myGame)
    {
        var estimate = myGame.Game?.Playtime ?? 0;
        if (estimate <= 0)
        {
            return TotalHours(myGame) > 0 ? "No estimate" : "Not started";
        }

        var remaining = RemainingFor(myGame);
        return remaining <= 0 ? "Done" : $"{FormatHours(remaining)}h left";
    }

    private static string StatusClass(GameStatus status)
    {
        return status switch
        {
            GameStatus.Playing => "status-playing",
            GameStatus.MainGame => "status-main",
            GameStatus.Completed or GameStatus.StoryComplete => "status-complete",
            GameStatus.Planned => "status-planned",
            _ => "status-default"
        };
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    protected override DialogParameters<MyGameFormDialog> CreateDialogParameters(MyGame command, Func<MyGame, Task> OnSubmit)
    {
        return new DialogParameters<MyGameFormDialog>
            {
                { x=>x.Refresh , new Action(async () => await ReloadData()) },
                { x=>x.Model, command },
                { x=>x.GameSelect, DropdownGames },
                { x=>x.loading, loading },
                { x=>x.EventCallBack, OnSubmit }
            };
    }
}
