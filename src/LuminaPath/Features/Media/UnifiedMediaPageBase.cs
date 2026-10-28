using LuminaPath.Components.Dialogs;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Features.Auth;
using LuminaPath.Features.Media.Animes.Components;
using LuminaPath.Features.Media.Animes.ViewModel;
using LuminaPath.Features.Media.Games.Components;
using LuminaPath.Features.Media.Games.ViewModel;
using LuminaPath.Features.Media.Models;
using LuminaPath.Features.Media.Movies.Components;
using LuminaPath.Features.Media.Movies.ViewModel;
using LuminaPath.Features.Media.MyAnimes.Components;
using LuminaPath.Features.Media.MyGames.Components;
using LuminaPath.Features.Media.MyMovies.Components;
using LuminaPath.Features.Media.MySeries.Components;
using LuminaPath.Features.Media.Series.Components;
using LuminaPath.Features.Media.Series.ViewModel;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace LuminaPath.Features.Media;

public abstract class UnifiedMediaPageBase : BaseAuth
{
    [Inject] protected GameService GameService { get; set; } = default!;
    [Inject] protected MyGameService MyGameService { get; set; } = default!;
    [Inject] protected MovieService MovieService { get; set; } = default!;
    [Inject] protected MyMovieService MyMovieService { get; set; } = default!;
    [Inject] protected SeriesService SeriesService { get; set; } = default!;
    [Inject] protected MySeriesService MySeriesService { get; set; } = default!;
    [Inject] protected AnimeService AnimeService { get; set; } = default!;
    [Inject] protected MyAnimeService MyAnimeService { get; set; } = default!;
    [Inject] protected DocumentService DocumentService { get; set; } = default!;
    [Inject] protected GameMetadataRefreshService GameMetadataRefreshService { get; set; } = default!;
    [Inject] protected IObjectMapper Mapper { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    protected UnifiedMediaItem? ActiveDetailItem { get; set; }
    protected bool IsDetailBusy { get; set; }

    protected abstract Task OnMediaDataMutatedAsync();

    protected void OpenDetail(UnifiedMediaItem item)
    {
        ActiveDetailItem = item;
    }

    protected void CloseDetail()
    {
        ActiveDetailItem = null;
    }

    protected async Task HandleAddToLibraryAsync(UnifiedMediaItem item)
    {
        if (User is null || string.IsNullOrWhiteSpace(UserId))
        {
            Snackbar.Add("Please sign in to add items to your library.", Severity.Info);
            return;
        }

        IsDetailBusy = true;
        try
        {
            switch (item.Kind)
            {
                case "game":
                {
                    var myGame = new MyGame
                    {
                        GameId = item.MediaId,
                        LuminaUserId = UserId,
                        Status = GameStatus.Planned,
                        Priority = 3,
                        StartDate = DateTime.UtcNow
                    };
                    var res = await MyGameService.PostAsync(myGame, User);
                    res.Match(
                        saved =>
                        {
                            item.UserMediaId = saved.Id;
                            item.GameStatus = saved.Status;
                            item.MyGameEntity = saved;
                            Snackbar.Add($"Added \"{item.Title}\" to your library", Severity.Success);
                            return 0;
                        },
                        err =>
                        {
                            Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                            return 0;
                        });
                    break;
                }
                case "movie":
                {
                    var myMovie = new MyMovie
                    {
                        MovieId = item.MediaId,
                        LuminaUserId = UserId,
                        Status = MediaStatus.Planned,
                        Priority = 2,
                        StartDate = DateTime.UtcNow
                    };
                    var res = await MyMovieService.PostAsync(myMovie, User);
                    res.Match(
                        saved =>
                        {
                            item.UserMediaId = saved.Id;
                            item.MediaStatus = saved.Status;
                            item.MyMovieEntity = saved;
                            Snackbar.Add($"Added \"{item.Title}\" to your library", Severity.Success);
                            return 0;
                        },
                        err =>
                        {
                            Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                            return 0;
                        });
                    break;
                }
                case "series":
                {
                    var mySeries = new Core.Models.MySeries
                    {
                        SeriesId = item.MediaId,
                        LuminaUserId = UserId,
                        Status = MediaStatus.Planned,
                        Priority = 2,
                        CurrentEpisode = 0,
                        StartDate = DateTime.UtcNow
                    };
                    var res = await MySeriesService.PostAsync(mySeries, User);
                    res.Match(
                        saved =>
                        {
                            item.UserMediaId = saved.Id;
                            item.MediaStatus = saved.Status;
                            item.MySeriesEntity = saved;
                            Snackbar.Add($"Added \"{item.Title}\" to your library", Severity.Success);
                            return 0;
                        },
                        err =>
                        {
                            Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                            return 0;
                        });
                    break;
                }
                case "anime":
                {
                    var myAnime = new MyAnime
                    {
                        AnimeId = item.MediaId,
                        LuminaUserId = UserId,
                        Status = MediaStatus.Planned,
                        Priority = 2,
                        CurrentEpisode = 0,
                        StartDate = DateTime.UtcNow
                    };
                    var res = await MyAnimeService.PostAsync(myAnime, User);
                    res.Match(
                        saved =>
                        {
                            item.UserMediaId = saved.Id;
                            item.MediaStatus = saved.Status;
                            item.MyAnimeEntity = saved;
                            Snackbar.Add($"Added \"{item.Title}\" to your library", Severity.Success);
                            return 0;
                        },
                        err =>
                        {
                            Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                            return 0;
                        });
                    break;
                }
            }

            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleRemoveFromLibraryAsync(UnifiedMediaItem item)
    {
        if (User is null || item.UserMediaId is null)
        {
            return;
        }

        if (!await ConfirmActionAsync($"Remove \"{item.Title}\" from your personal library?"))
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var id = item.UserMediaId.Value;
            switch (item.Kind)
            {
                case "game":
                    await MyGameService.DeleteAsync(id, User);
                    break;
                case "movie":
                    await MyMovieService.DeleteAsync(id, User);
                    break;
                case "series":
                    await MySeriesService.DeleteAsync(id, User);
                    break;
                case "anime":
                    await MyAnimeService.DeleteAsync(id, User);
                    break;
            }

            item.UserMediaId = null;
            ActiveDetailItem = null;
            Snackbar.Add($"Removed \"{item.Title}\" from your library", Severity.Info);
            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleUpdateGameStatusAsync((UnifiedMediaItem Item, GameStatus Status) args)
    {
        if (User is null || args.Item.UserMediaId is null || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var entity = await MyGameService.GetById(args.Item.UserMediaId.Value, UserId, MyGameService.Includes);
            entity.Status = args.Status;
            entity.Game = null;
            var res = await MyGameService.PutAsync(entity, User);
            res.Match(
                _ =>
                {
                    args.Item.GameStatus = args.Status;
                    Snackbar.Add("Status updated", Severity.Success);
                    return 0;
                },
                err =>
                {
                    Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                    return 0;
                });
            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleUpdateMediaStatusAsync((UnifiedMediaItem Item, MediaStatus Status) args)
    {
        if (User is null || args.Item.UserMediaId is null || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var id = args.Item.UserMediaId.Value;
            switch (args.Item.Kind)
            {
                case "movie":
                {
                    var entity = await MyMovieService.GetById(id, UserId, MyMovieService.Includes);
                    entity.Status = args.Status;
                    entity.Movie = null;
                    await MyMovieService.PutAsync(entity, User);
                    break;
                }
                case "series":
                {
                    var entity = await MySeriesService.GetById(id, UserId, MySeriesService.Includes);
                    entity.Status = args.Status;
                    await MySeriesService.PutAsync(entity, User);
                    break;
                }
                case "anime":
                {
                    var entity = await MyAnimeService.GetById(id, UserId, MyAnimeService.Includes);
                    entity.Status = args.Status;
                    await MyAnimeService.PutAsync(entity, User);
                    break;
                }
            }

            args.Item.MediaStatus = args.Status;
            Snackbar.Add("Status updated", Severity.Success);
            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleUpdateRatingAsync((UnifiedMediaItem Item, short? Rating) args)
    {
        if (User is null || args.Item.UserMediaId is null || string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var id = args.Item.UserMediaId.Value;
            switch (args.Item.Kind)
            {
                case "game":
                {
                    var entity = await MyGameService.GetById(id, UserId, MyGameService.Includes);
                    entity.Rating = args.Rating;
                    entity.Game = null;
                    await MyGameService.PutAsync(entity, User);
                    break;
                }
                case "movie":
                {
                    var entity = await MyMovieService.GetById(id, UserId, MyMovieService.Includes);
                    entity.Rating = args.Rating;
                    entity.Movie = null;
                    await MyMovieService.PutAsync(entity, User);
                    break;
                }
                case "series":
                {
                    var entity = await MySeriesService.GetById(id, UserId, MySeriesService.Includes);
                    entity.Rating = args.Rating;
                    await MySeriesService.PutAsync(entity, User);
                    break;
                }
                case "anime":
                {
                    var entity = await MyAnimeService.GetById(id, UserId, MyAnimeService.Includes);
                    entity.Rating = args.Rating;
                    await MyAnimeService.PutAsync(entity, User);
                    break;
                }
            }

            args.Item.Rating = args.Rating;
            Snackbar.Add("Rating saved", Severity.Success);
            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleLogSessionAsync((UnifiedMediaItem Item, double Amount) args)
    {
        if (User is null || args.Item.UserMediaId is null || string.IsNullOrWhiteSpace(UserId) || args.Amount <= 0)
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var id = args.Item.UserMediaId.Value;
            switch (args.Item.Kind)
            {
                case "game":
                {
                    // Strictly preserve separation between manual playtime (TimeSpend) and imported third-party playtime (MyGameInfo.TrackedHours)
                    var entity = await MyGameService.GetById(id, UserId, MyGameService.Includes);
                    entity.TimeSpend = Math.Round(Math.Max(0, entity.TimeSpend ?? 0) + args.Amount, 2);
                    if (entity.Status is GameStatus.Planned or GameStatus.OnHold)
                    {
                        entity.Status = GameStatus.Playing;
                        args.Item.GameStatus = GameStatus.Playing;
                    }
                    entity.Game = null;
                    await MyGameService.PutAsync(entity, User);
                    args.Item.ManualHours = entity.TimeSpend ?? 0;
                    Snackbar.Add($"Logged +{args.Amount:0.#}h manual playtime", Severity.Success);
                    break;
                }
                case "movie":
                {
                    var entity = await MyMovieService.GetById(id, UserId, MyMovieService.Includes);
                    entity.TimeSpend = Math.Round(Math.Max(0, entity.TimeSpend ?? 0) + args.Amount, 2);
                    entity.CurrentWatchTimeMinutes = (int)Math.Round((entity.TimeSpend ?? 0) * 60);
                    entity.Movie = null;
                    await MyMovieService.PutAsync(entity, User);
                    args.Item.ManualHours = entity.TimeSpend ?? 0;
                    args.Item.CurrentWatchTimeMinutes = entity.CurrentWatchTimeMinutes;
                    Snackbar.Add($"Logged +{args.Amount:0.#}h watch time", Severity.Success);
                    break;
                }
                case "series":
                {
                    var entity = await MySeriesService.GetById(id, UserId, MySeriesService.Includes);
                    var series = entity.Series;
                    var addEp = Math.Max(1, (int)Math.Round(args.Amount));
                    entity.CurrentEpisode = (entity.CurrentEpisode ?? 0) + addEp;
                    if (series?.EpisodeCount is > 0 && entity.CurrentEpisode >= series.EpisodeCount)
                    {
                        entity.CurrentEpisode = series.EpisodeCount;
                        entity.Status = MediaStatus.Completed;
                        args.Item.MediaStatus = MediaStatus.Completed;
                    }
                    else if (entity.Status == MediaStatus.Planned)
                    {
                        entity.Status = MediaStatus.Watching;
                        args.Item.MediaStatus = MediaStatus.Watching;
                    }
                    entity.RecalculateWatchTime(series);
                    await MySeriesService.PutAsync(entity, User);
                    args.Item.CurrentEpisode = entity.CurrentEpisode;
                    args.Item.ManualHours = Math.Round(entity.TimeSpend ?? 0, 1);
                    Snackbar.Add($"Updated to episode {entity.CurrentEpisode}", Severity.Success);
                    break;
                }
                case "anime":
                {
                    var entity = await MyAnimeService.GetById(id, UserId, MyAnimeService.Includes);
                    var anime = entity.Anime;
                    var addEp = Math.Max(1, (int)Math.Round(args.Amount));
                    entity.CurrentEpisode = (entity.CurrentEpisode ?? 0) + addEp;
                    if (anime?.EpisodeCount is > 0 && entity.CurrentEpisode >= anime.EpisodeCount)
                    {
                        entity.CurrentEpisode = anime.EpisodeCount;
                        entity.Status = MediaStatus.Completed;
                        args.Item.MediaStatus = MediaStatus.Completed;
                    }
                    else if (entity.Status == MediaStatus.Planned)
                    {
                        entity.Status = MediaStatus.Watching;
                        args.Item.MediaStatus = MediaStatus.Watching;
                    }
                    entity.RecalculateWatchTime(anime);
                    await MyAnimeService.PutAsync(entity, User);
                    args.Item.CurrentEpisode = entity.CurrentEpisode;
                    args.Item.ManualHours = Math.Round(entity.TimeSpend ?? 0, 1);
                    Snackbar.Add($"Updated to episode {entity.CurrentEpisode}", Severity.Success);
                    break;
                }
            }

            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task OpenCreateLibraryEntryDialogAsync(string kind)
    {
        if (User is null || string.IsNullOrWhiteSpace(UserId))
        {
            Snackbar.Add("Please sign in first.", Severity.Info);
            return;
        }

        var normalizedKind = kind is "all" or "" ? "game" : kind;
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        switch (normalizedKind)
        {
            case "game":
            {
                var games = await GameService.GetAll();
                var model = new MyGame { Priority = 3, Status = GameStatus.Planned, StartDate = DateTime.Today };
                var parameters = new DialogParameters<MyGameFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.GameSelect, games },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyGame, Task>(async entity =>
                    {
                        entity.Game = null;
                        var res = await MyGameService.PostAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Added game to library", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyGameFormDialog>("Add Game to Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "movie":
            {
                var movies = await MovieService.GetAll();
                var model = new MyMovie { Priority = 2, Status = MediaStatus.Planned, StartDate = DateTime.Today };
                var parameters = new DialogParameters<MyMovieFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.MovieSelect, movies },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyMovie, Task>(async entity =>
                    {
                        entity.Movie = null;
                        var res = await MyMovieService.PostAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Added movie to library", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyMovieFormDialog>("Add Movie to Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "series":
            {
                var seriesList = await SeriesService.GetAll();
                var model = new Core.Models.MySeries { Priority = 2, Status = MediaStatus.Planned, StartDate = DateTime.Today };
                var parameters = new DialogParameters<MySeriesFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.SeriesSelect, seriesList },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<Core.Models.MySeries, Task>(async entity =>
                    {
                        var res = await MySeriesService.PostAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Added series to library", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MySeriesFormDialog>("Add Series to Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "anime":
            {
                var animes = await AnimeService.GetAll();
                var model = new MyAnime { Priority = 2, Status = MediaStatus.Planned, StartDate = DateTime.Today };
                var parameters = new DialogParameters<MyAnimeFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.AnimeSelect, animes },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyAnime, Task>(async entity =>
                    {
                        var res = await MyAnimeService.PostAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Added anime to library", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyAnimeFormDialog>("Add Anime to Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
        }
    }

    protected async Task OpenEditLibraryEntryDialogAsync(UnifiedMediaItem item)
    {
        if (User is null || string.IsNullOrWhiteSpace(UserId) || item.UserMediaId is null)
        {
            return;
        }

        CloseDetail();
        var id = item.UserMediaId.Value;
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        switch (item.Kind)
        {
            case "game":
            {
                var games = await GameService.GetAll();
                var model = await MyGameService.GetById(id, UserId, MyGameService.Includes);
                var parameters = new DialogParameters<MyGameFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.GameSelect, games },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyGame, Task>(async entity =>
                    {
                        entity.Game = null;
                        var res = await MyGameService.PutAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Updated library entry", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyGameFormDialog>("Edit Game in Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "movie":
            {
                var movies = await MovieService.GetAll();
                var model = await MyMovieService.GetById(id, UserId, MyMovieService.Includes);
                var parameters = new DialogParameters<MyMovieFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.MovieSelect, movies },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyMovie, Task>(async entity =>
                    {
                        entity.Movie = null;
                        var res = await MyMovieService.PutAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Updated library entry", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyMovieFormDialog>("Edit Movie in Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "series":
            {
                var seriesList = await SeriesService.GetAll();
                var model = await MySeriesService.GetById(id, UserId, MySeriesService.Includes);
                var parameters = new DialogParameters<MySeriesFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.SeriesSelect, seriesList },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<Core.Models.MySeries, Task>(async entity =>
                    {
                        var res = await MySeriesService.PutAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Updated library entry", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MySeriesFormDialog>("Edit Series in Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "anime":
            {
                var animes = await AnimeService.GetAll();
                var model = await MyAnimeService.GetById(id, UserId, MyAnimeService.Includes);
                var parameters = new DialogParameters<MyAnimeFormDialog>
                {
                    { x => x.Model, model },
                    { x => x.AnimeSelect, animes },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.EventCallBack, new Func<MyAnime, Task>(async entity =>
                    {
                        var res = await MyAnimeService.PutAsync(entity, User);
                        res.Match(
                            _ => { Snackbar.Add("Updated library entry", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MyAnimeFormDialog>("Edit Anime in Library", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
        }
    }

    protected async Task OpenCreateCatalogEntryDialogAsync(string kind)
    {
        if (!CanUserEdit)
        {
            return;
        }

        var normalizedKind = kind is "all" or "" ? "game" : kind;
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        switch (normalizedKind)
        {
            case "game":
            {
                var vm = new GameViewModel { ReleaseDate = DateTime.Today };
                var parameters = new DialogParameters<GameFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<GameViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<GameViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Game>(entity);
                        var res = await GameService.PostAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Created game in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<GameFormDialog>("Create Game", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "movie":
            {
                var vm = new MovieViewModel { ReleaseDate = DateTime.Today };
                var parameters = new DialogParameters<MovieFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<MovieViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<MovieViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Movie>(entity);
                        var res = await MovieService.PostAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Created movie in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MovieFormDialog>("Create Movie", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "series":
            {
                var vm = new SeriesViewModel { ReleaseDate = DateTime.Today };
                var parameters = new DialogParameters<SeriesFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<SeriesViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<SeriesViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<LuminaPath.Core.Models.Series>(entity);
                        var res = await SeriesService.PostAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Created series in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<SeriesFormDialog>("Create Series", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "anime":
            {
                var vm = new AnimeViewModel { ReleaseDate = DateTime.Today };
                var parameters = new DialogParameters<AnimeFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<AnimeViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<AnimeViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Anime>(entity);
                        var res = await AnimeService.PostAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Created anime in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<AnimeFormDialog>("Create Anime", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
        }
    }

    protected async Task OpenEditCatalogEntryDialogAsync(UnifiedMediaItem item)
    {
        if (!CanUserEdit)
        {
            return;
        }

        CloseDetail();
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        switch (item.Kind)
        {
            case "game":
            {
                var vm = await GameService.GetByIdAndMap<GameViewModel>(item.MediaId, GameService.Includes, UserId);
                var parameters = new DialogParameters<GameFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<GameViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<GameViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Game>(entity);
                        var res = await GameService.PutAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Updated game in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<GameFormDialog>("Edit Game", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "movie":
            {
                var vm = await MovieService.GetByIdAndMap<MovieViewModel>(item.MediaId, MovieService.Includes, UserId);
                var parameters = new DialogParameters<MovieFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<MovieViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<MovieViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Movie>(entity);
                        var res = await MovieService.PutAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Updated movie in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<MovieFormDialog>("Edit Movie", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "series":
            {
                var vm = await SeriesService.GetByIdAndMap<SeriesViewModel>(item.MediaId, SeriesService.Includes, UserId);
                var parameters = new DialogParameters<SeriesFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<SeriesViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<SeriesViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<LuminaPath.Core.Models.Series>(entity);
                        var res = await SeriesService.PutAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Updated series in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<SeriesFormDialog>("Edit Series", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
            case "anime":
            {
                var vm = await AnimeService.GetByIdAndMap<AnimeViewModel>(item.MediaId, AnimeService.Includes, UserId);
                var parameters = new DialogParameters<AnimeFormDialog>
                {
                    { x => x.Model, vm },
                    { x => x.Refresh, new Action(() => _ = InvokeAsync(OnMediaDataMutatedAsync)) },
                    { x => x.OnSubmitFile, new Func<IBrowserFile, Task>(file => HandleSubmitFileAsync(file, vm)) },
                    { x => x.OnDeleteImage, new Func<AnimeViewModel, Task>(model => HandleDeleteImageAsync(model)) },
                    { x => x.EventCallBack, new Func<AnimeViewModel, Task>(async entity =>
                    {
                        var mapped = Mapper.Map<Anime>(entity);
                        var res = await AnimeService.PutAsync(mapped);
                        res.Match(
                            _ => { Snackbar.Add("Updated anime in catalog", Severity.Success); return 0; },
                            err => { Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error); return 0; });
                    }) }
                };
                var dialog = await DialogService.ShowAsync<AnimeFormDialog>("Edit Anime", parameters, options);
                var result = await dialog.Result;
                if (result is { Canceled: false }) await OnMediaDataMutatedAsync();
                break;
            }
        }
    }

    protected async Task HandleSubmitFileAsync<TViewModel>(IBrowserFile file, TViewModel model)
        where TViewModel : class, IMedia<MediaDocument>
    {
        if (!CanUserEdit)
        {
            Snackbar.Add("No permission to edit", Severity.Info);
            return;
        }

        try
        {
            var result = await DocumentService.CreateDocument(file, model);
            result.Match(
                doc =>
                {
                    model.Image = doc;
                    Snackbar.Add("Cover uploaded", Severity.Success);
                    return 0;
                },
                err =>
                {
                    Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Error);
                    return 0;
                });
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to upload cover: {ex.Message}", Severity.Error);
        }
    }

    protected async Task HandleDeleteImageAsync<TViewModel>(TViewModel model)
        where TViewModel : class, IMedia<MediaDocument>
    {
        if (!CanUserEdit)
        {
            Snackbar.Add("No permission to edit", Severity.Info);
            return;
        }

        if (model.Image == null)
        {
            return;
        }

        try
        {
            await DocumentService.DeleteMediaDocument(model);
            Snackbar.Add("Image deleted", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to delete image: {ex.Message}", Severity.Error);
        }
    }

    protected async Task HandleDeleteCatalogEntryAsync(UnifiedMediaItem item)
    {
        if (!CanUserEdit)
        {
            return;
        }

        if (!await ConfirmActionAsync($"Permanently delete \"{item.Title}\" from the catalog?"))
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            switch (item.Kind)
            {
                case "game":
                    await GameService.DeleteAsync(item.MediaId);
                    break;
                case "movie":
                    await MovieService.DeleteAsync(item.MediaId);
                    break;
                case "series":
                    await SeriesService.DeleteAsync(item.MediaId);
                    break;
                case "anime":
                    await AnimeService.DeleteAsync(item.MediaId);
                    break;
            }

            CloseDetail();
            Snackbar.Add($"Deleted \"{item.Title}\" from catalog", Severity.Info);
            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task HandleRefreshGameMetadataAsync(UnifiedMediaItem item)
    {
        if (!CanUserEdit || item.Kind != "game")
        {
            return;
        }

        IsDetailBusy = true;
        try
        {
            var result = await GameMetadataRefreshService.RefreshGameAsync(item.MediaId);
            result.Match(
                refreshed =>
                {
                    if (refreshed.UpdatedReleaseDate || refreshed.UpdatedCover)
                    {
                        Snackbar.Add($"Refreshed metadata for \"{item.Title}\" from {refreshed.Provider}", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add(refreshed.Message, Severity.Info);
                    }
                    return 0;
                },
                err =>
                {
                    Snackbar.Add(string.Join(", ", err.errorMessage), Severity.Warning);
                    return 0;
                });

            await OnMediaDataMutatedAsync();
        }
        finally
        {
            IsDetailBusy = false;
        }
    }

    protected async Task<bool> ConfirmActionAsync(string message)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, message }
        };
        var options = new DialogOptions
        {
            CloseButton = true,
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true
        };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm", parameters, options);
        var result = await dialog.Result;
        return result is { Canceled: false };
    }
}
