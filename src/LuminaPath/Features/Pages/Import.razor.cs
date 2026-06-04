using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Imports;
using LuminaPath.Infrastructure.Services.ThirdParty;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using static LuminaPath.Core.Entities.PSN.PSNTitles;

namespace LuminaPath.Features.Pages;

public partial class Import
{
    const long MaxExcelFileSize = ExcelService.MaxWorkbookBytes;
    IQueryable<MyGameDto>? games;
    LuminaUserInfo model = new();
    SteamLibraryPreview? steamPreview;
    SteamImportResult? steamImportResult;
    AchievementSyncResult? steamAchievementSyncResult;
    AchievementSyncResult? psnAchievementSyncResult;
    GameImportPreviewResult? psnPreview;
    ExcelService.GameExcelImportResult? _excelResult;
    ExcelService.GameExcelPreviewResult? _excelPreview;
    byte[]? _excelImportBytes;
    string? _excelFileName;
    bool _excelImporting;
    bool _excelSaving;
    bool steamPreviewing;
    bool steamImporting;
    bool steamAchievementSyncing;
    bool psnAchievementSyncing;
    bool steamConfigured;
    string steamIdentifier = string.Empty;
    int ImportedGameCount => games?.Count() ?? 0;
    string TrophyLevelLabel => model.PSNTrophyLevel > 0 ? model.PSNTrophyLevel.ToString() : "-";
    string ProfileAccountLabel => string.IsNullOrWhiteSpace(model.PSNAccountId) ? "Not connected" : model.PSNAccountId;
    string SteamConfiguredLabel => steamConfigured ? "Configured" : "Missing";
    string SteamGameCountLabel => steamPreview is null ? "-" : steamPreview.GameCount.ToString();
    string SteamIdLabel => !string.IsNullOrWhiteSpace(steamPreview?.Profile.SteamId)
        ? steamPreview.Profile.SteamId
        : string.IsNullOrWhiteSpace(model.SteamId) ? "Not connected" : model.SteamId;
    string SteamProfileLabel => !string.IsNullOrWhiteSpace(steamPreview?.Profile.PersonaName)
        ? steamPreview.Profile.PersonaName
        : string.IsNullOrWhiteSpace(model.SteamPersonaName) ? "Steam profile" : model.SteamPersonaName;
    string SteamAvatarUrl => !string.IsNullOrWhiteSpace(steamPreview?.Profile.AvatarUrl)
        ? steamPreview.Profile.AvatarUrl!
        : model.SteamAvatarUrl;
    IEnumerable<SteamOwnedGame> SteamPreviewGames => steamPreview?.Games ?? Enumerable.Empty<SteamOwnedGame>();
    Dictionary<string, GameImportPreviewItem> PsnPreviewByExternalId => psnPreview?.Rows
        .Where(row => !string.IsNullOrWhiteSpace(row.ExternalId))
        .GroupBy(row => row.ExternalId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase) ?? new();
    bool CanImportSteam => steamConfigured
        && !steamImporting
        && !steamPreviewing
        && !string.IsNullOrWhiteSpace(steamIdentifier);
    string ExcelFileLabel => string.IsNullOrWhiteSpace(_excelFileName) ? "No file loaded" : _excelFileName;
    string ExcelRowsLabel => _excelPreview is null ? "-" : _excelPreview.RowsDetected.ToString();
    string ExcelWarningsLabel => _excelPreview is null ? "-" : _excelPreview.Errors.Count.ToString();
    bool CanSaveExcelImport => !_excelImporting
        && !_excelSaving
        && _excelImportBytes is not null
        && _excelPreview?.Rows.Any() == true;
    IEnumerable<string> ExcelErrors => (_excelPreview?.Errors ?? Enumerable.Empty<string>())
        .Concat(_excelResult?.Errors ?? Enumerable.Empty<string>());

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (User?.LuminaUserInfo != null)
        {
            model = User.LuminaUserInfo;
        }
        if (!string.IsNullOrWhiteSpace(UserId))
        {
            model.UserId = UserId;
        }
        steamIdentifier = model.SteamId;
        steamConfigured = await steamService.IsConfiguredAsync();

        var bearer = await applicationSettingsService.GetPsnBearerTokenAsync();
        psnService.SetBearer(bearer);
    }

    async Task ImportProfile()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            Snackbar.Add("Please sign in before importing a profile", Severity.Info);
            return;
        }

        try
        {
            var profile = await psnService.GetProfile(model.PSNOnlineId);
            model.PSNAccountId = profile.Profile.AccountId;
            var trophies = await psnService.GetUserProfileTrophy(model.PSNAccountId);
            model.PSNTrophyLevel = trophies.TrophyLevel;
            model.PSNPlatinum = trophies.EarnedTrophies.Platinum;
            model.PSNGold = trophies.EarnedTrophies.Gold;
            model.PSNSilver = trophies.EarnedTrophies.Silver;
            model.PSNBronze = trophies.EarnedTrophies.Bronze;
            await userService.UpdateThirdParty(UserId, model);
            Snackbar.Add("Imported Profile", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Profile not found", Severity.Warning);
        }
    }

    async Task AddGamesToLibrary()
    {
        bool? confirm = await dialogService.Confirm("Add shown games to library?", "Add to Library");
        if(confirm is not null && confirm.Value)
        {
            if (User is null || games is null)
            {
                Snackbar.Add("No games available to import", Severity.Info);
                return;
            }

            await psnService.ImportGames(User, games.ToList());
            Snackbar.Add("Added games to library", Severity.Success);
        }
    }

    async Task ImportGames()
    {
        if (model.PSNAccountId == null || model.PSNAccountId == string.Empty)
        {
            Snackbar.Add("Please import your profile first", Severity.Info);
            return;
        }
        try
        {
            var gameData = await GetGameData();
            var result = psnService.ConvertPSNTitles(gameData);
            games = result.AsQueryable();
            psnPreview = User is null ? null : await psnService.PreviewGames(User, result);

        }catch
        {
            Snackbar.Add("Admin token expired, please notify the admin", Severity.Warning);
        }
    }

    async Task PreviewSteamLibrary()
    {
        if (!ValidateSteamInput())
        {
            return;
        }

        steamPreviewing = true;
        steamImportResult = null;

        try
        {
            steamPreview = await steamService.PreviewLibraryAsync(steamIdentifier, CancellationToken.None);
            if (steamPreview is null)
            {
                Snackbar.Add("Steam profile not found", Severity.Warning);
                return;
            }

            await SaveSteamProfile(steamPreview.Profile);
            steamIdentifier = steamPreview.Profile.SteamId;

            if (steamPreview.GameCount == 0)
            {
                Snackbar.Add("Steam profile found, but no public games were returned", Severity.Info);
                return;
            }

            Snackbar.Add($"Loaded {steamPreview.GameCount} Steam games", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Steam library could not be loaded", Severity.Warning);
        }
        finally
        {
            steamPreviewing = false;
        }
    }

    async Task ImportSteamLibrary()
    {
        if (!ValidateSteamInput() || User is null)
        {
            return;
        }

        bool? confirm = await dialogService.Confirm("Import this Steam library?", "Import Steam");
        if (confirm != true)
        {
            return;
        }

        steamImporting = true;

        try
        {
            steamImportResult = await steamService.ImportLibraryAsync(User, steamIdentifier, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(steamImportResult.SteamId))
            {
                Snackbar.Add("Steam profile not found", Severity.Warning);
                return;
            }

            steamIdentifier = steamImportResult.SteamId;
            if (steamPreview?.Profile.SteamId == steamImportResult.SteamId)
            {
                await SaveSteamProfile(steamPreview.Profile);
            }
            else
            {
                model.SteamId = steamImportResult.SteamId;
                await userService.UpdateThirdParty(UserId!, model);
            }

            if (steamImportResult.Total == 0)
            {
                Snackbar.Add("Steam profile found, but no public games were returned", Severity.Info);
                return;
            }

            Snackbar.Add($"Imported {steamImportResult.Total} Steam games", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Steam import failed", Severity.Error);
        }
        finally
        {
            steamImporting = false;
        }
    }

    async Task SyncSteamAchievements()
    {
        if (!ValidateSteamInput() || User is null)
        {
            return;
        }

        steamAchievementSyncing = true;
        steamAchievementSyncResult = null;

        try
        {
            steamAchievementSyncResult = await achievementSyncService.SyncSteamAsync(User, CancellationToken.None);
            foreach (var warning in steamAchievementSyncResult.Warnings)
            {
                Snackbar.Add(warning, Severity.Warning);
            }

            Snackbar.Add(
                $"Synced Steam achievements: {steamAchievementSyncResult.UnlocksAdded} new unlocks",
                Severity.Success);
        }
        catch
        {
            Snackbar.Add("Steam achievement sync failed", Severity.Error);
        }
        finally
        {
            steamAchievementSyncing = false;
        }
    }

    async Task SyncPsnAchievements()
    {
        if (User is null || string.IsNullOrWhiteSpace(model.PSNAccountId))
        {
            Snackbar.Add("Please import your PSN profile first", Severity.Info);
            return;
        }

        psnAchievementSyncing = true;
        psnAchievementSyncResult = null;

        try
        {
            psnAchievementSyncResult = await achievementSyncService.SyncPsnAsync(User, CancellationToken.None);
            foreach (var warning in psnAchievementSyncResult.Warnings)
            {
                Snackbar.Add(warning, Severity.Warning);
            }

            Snackbar.Add(
                $"Synced PSN trophies: {psnAchievementSyncResult.UnlocksAdded} new unlocks",
                Severity.Success);
        }
        catch
        {
            Snackbar.Add("PSN trophy sync failed", Severity.Error);
        }
        finally
        {
            psnAchievementSyncing = false;
        }
    }

    bool ValidateSteamInput()
    {
        if (!steamConfigured)
        {
            Snackbar.Add("Steam API key is not configured", Severity.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(UserId))
        {
            Snackbar.Add("Please sign in before importing a Steam library", Severity.Info);
            return false;
        }

        if (string.IsNullOrWhiteSpace(steamIdentifier))
        {
            Snackbar.Add("Enter a Steam ID, vanity name, or profile URL", Severity.Info);
            return false;
        }

        return true;
    }

    async Task SaveSteamProfile(SteamProfile profile)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return;
        }

        model.SteamId = profile.SteamId;
        model.SteamPersonaName = profile.PersonaName;
        model.SteamProfileUrl = profile.ProfileUrl ?? string.Empty;
        model.SteamAvatarUrl = profile.AvatarUrl ?? string.Empty;
        await userService.UpdateThirdParty(UserId, model);
    }

    async Task<GameData> GetGameData()
    {
        GameData gameData = new();
        int currentIteration = 0;

        while (true)
        {
            if (currentIteration >= 5)
                break;
            int offSet = currentIteration * 200;
            var result = await psnService.GetTitles(offSet, model.PSNAccountId!);
            gameData.Titles.AddRange(result.Titles);

            if (result.NextOffset == null || result.NextOffset <= 0)
                break;

            currentIteration++;
        }

        return gameData;
    }

    async Task DownloadExcel()
    {
        if (User is null)
        {
            Snackbar.Add("Please sign in before exporting your library", Severity.Info);
            return;
        }

        var fileStream = new MemoryStream(await excelService.ExportGamesAsync(User));
        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JS.InvokeVoidAsync("downloadFileFromStream", "LuminaLibrary.xlsx", streamRef);
    }

    async Task PreviewExcel(IBrowserFile file)
    {
        if (User is null)
        {
            Snackbar.Add("Please sign in before importing games", Severity.Info);
            return;
        }

        _excelImporting = true;
        _excelResult = null;
        _excelPreview = null;
        _excelImportBytes = null;
        _excelFileName = file.Name;

        try
        {
            using var importStream = await CopyToMemoryStream(file);
            _excelImportBytes = importStream.ToArray();
            importStream.Position = 0;
            _excelPreview = await excelService.PreviewGamesAsync(importStream, User, file.Name);
            ShowExcelPreviewResult(_excelPreview);
        }
        catch (Exception ex)
        {
            _excelImportBytes = null;
            _excelFileName = null;
            Snackbar.Add($"Excel preview failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _excelImporting = false;
        }
    }

    async Task SaveExcelImport()
    {
        if (User is null)
        {
            Snackbar.Add("Please sign in before importing games", Severity.Info);
            return;
        }

        if (_excelImportBytes is null)
        {
            Snackbar.Add("Preview an Excel file before importing it", Severity.Info);
            return;
        }

        bool? confirm = await dialogService.Confirm("Import the previewed Excel rows?", "Import Excel");
        if (confirm != true)
        {
            return;
        }

        _excelSaving = true;
        _excelResult = null;

        try
        {
            using var importStream = new MemoryStream(_excelImportBytes);
            _excelResult = await excelService.ImportGamesAsync(importStream, User, _excelFileName);
            ShowExcelImportResult(_excelResult);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Excel import failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _excelSaving = false;
        }
    }

    static async Task<MemoryStream> CopyToMemoryStream(IBrowserFile file)
    {
        await using var uploadStream = file.OpenReadStream(MaxExcelFileSize);
        var importStream = new MemoryStream();
        await uploadStream.CopyToAsync(importStream);
        importStream.Position = 0;
        return importStream;
    }

    string GetPsnChangeType(MyGameDto game)
    {
        var psnId = game.Game?.PsnId;
        if (!string.IsNullOrWhiteSpace(psnId)
            && PsnPreviewByExternalId.TryGetValue(psnId, out var preview))
        {
            return string.IsNullOrWhiteSpace(preview.ChangeType) ? preview.GameAction : preview.ChangeType;
        }

        return "New";
    }

    Color GetImportChangeColor(string? changeType)
    {
        return (changeType ?? string.Empty).ToLowerInvariant() switch
        {
            "new" or "create" => Color.Success,
            "updated" or "update" => Color.Info,
            "duplicate" => Color.Warning,
            "error" => Color.Error,
            _ => Color.Default
        };
    }

    void ShowExcelImportResult(ExcelService.GameExcelImportResult result)
    {
        if (result.Errors.Any())
        {
            Snackbar.Add($"Imported {result.RowsImported} rows with {result.Errors.Count} warnings", Severity.Warning);
            return;
        }

        Snackbar.Add($"Imported {result.RowsImported} rows", Severity.Success);
    }

    void ShowExcelPreviewResult(ExcelService.GameExcelPreviewResult result)
    {
        if (result.Errors.Any())
        {
            Snackbar.Add($"Previewed {result.RowsDetected} rows with {result.Errors.Count} warnings", Severity.Warning);
            return;
        }

        Snackbar.Add($"Previewed {result.RowsDetected} rows", Severity.Success);
    }
}
