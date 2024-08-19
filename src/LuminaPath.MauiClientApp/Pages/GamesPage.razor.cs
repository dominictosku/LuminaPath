using MudBlazor;
using LuminaPath.MauiClientApp.Database;
using LuminaPath.UI.Shared.Interfaces;
using LuminaPath.MauiClientApp.Models;
using LuminaPath.Core.Models;
using LuminaPath.Core.Common.Entities;
using LuminaPath.MauiClientApp.Components.Media;
using LuminaPath.MauiClientApp.ViewModel;

namespace LuminaPath.MauiClientApp.Pages
{
	public partial class GamesPage : ITableActions<GameViewModel>
	{
		private List<GameViewModel> Games = new();
		private MudDataGrid<GameViewModel> _table = default!;
		private GameViewModel _currentDto = new();
		private MediaFilter _filter = new();

		public bool IsGrid = true;
		public bool loading;
		public HashSet<GameViewModel> selectedItems = new();
		public string ToggleText => IsGrid ? "Grid View" : "Table View";

		private async Task<GridData<GameViewModel>> ServerReload(GridState<GameViewModel> state)
		{
			selectedItems = new HashSet<GameViewModel>();
			loading = true;
			try
			{
				var result = await GetData(state.Page + 1);

				return new GridData<GameViewModel> { TotalItems = result.Count(), Items = result };
			}
			finally
			{
				loading = false;
			}
		}

		public async Task ReloadData()
		{
			if (IsGrid)
			{
				await _table.ReloadServerData();
			}
			else
			{
				Games = await GetData(1);
				StateHasChanged();
			}
		}

		private async Task SwitchView()
		{
			IsGrid = !IsGrid;
			await ReloadData();
		}

		private async Task<PaginatedList<GameViewModel>> GetData(int pageIndex)
		{
			_filter.Paging = new Paging(pageIndex, 15);
			var includes = new List<string>() { "Image" };
			//return await gameService.GetEntities(_filter, includes);
			var localGames = await database.GetItemsAsync() ?? new List<LocalGame>();
			var games = localGames.Select(x => new GameViewModel(x)).ToList();
			return PaginatedList<GameViewModel>.Create(games, 0, 100);
		}

		public void Dummy()
		{

		}

		#region Events
		public async Task OnCreate()
		{
			var command = new GameViewModel();
			var parameters = new DialogParameters<MediaFormDialog>
		{
			{ x=>x.Refresh , new Action(async () => await ReloadData()) },
			{ x=>x.Model, command },
			{ x=>x.loading, loading },
			{ x=>x.EventCallBack, CreateGame }
		};
			var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
			var dialog = DialogService.Show<MediaFormDialog>("Create Game", parameters, options);
			var state = await dialog.Result;
			if (!state.Canceled)
				await ReloadData();
		}

		public async Task OnUpdate(GameViewModel g)
		{
			var command = g;
			var parameters = new DialogParameters<MediaFormDialog>
		{
			{ x=>x.Refresh , new Action(async () => await ReloadData()) },
			{ x=>x.Model, command },
			{ x=>x.loading, loading },
			{ x=>x.EventCallBack, UpdateGame }
		};
			var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
			var dialog = DialogService.Show<MediaFormDialog>("Update Game", parameters, options);
			var state = await dialog.Result;
			if (!state.Canceled)
				await ReloadData();
		}

		public async Task OnDeleteChecked()
		{
			var ids = selectedItems.Select(x => x.Id).ToArray();

			foreach (var id in ids)
			{
				LocalGame existing = await database.GetItemAsync(id) ?? throw new Exception("id not found");
				//if (existing.Image is not null)
				//{
				//	dbContext.Documents.Remove(existing.Image);
				//}
				await database.DeleteItemAsync(existing);
			}
			await ReloadData();
		}

		#endregion

		#region CRUD Actions
		async Task CreateGame(GameViewModel game)
		{
			loading = true;
			try
			{
				var LocalGame = mapper.Map<LocalGame>(game);
				await database.SaveItemAsync(LocalGame);
				Snackbar.Add("Created Game", Severity.Success);
			}
			catch (Exception ex)
			{
				Snackbar.Add("Failed to create Game", Severity.Error);
				loading = false;
			}

			await ReloadData();
			loading = false;
		}

		async Task UpdateGame(GameViewModel game)
		{
			loading = true;
			try
			{
				var LocalGame = mapper.Map<LocalGame>(game);
				await database.SaveItemAsync(LocalGame);
				Snackbar.Add("Updated Game", Severity.Success);
			}
			catch (Exception ex)
			{
				Snackbar.Add("Failed to update Game", Severity.Error);
				loading = false;
			}

			await ReloadData();
			loading = false;
		}

		public async Task Delete(GameViewModel game)
		{
			//if (g.Image is not null)
			//	dbContext.Documents.Remove(g.Image);
			//dbContext.Games.Remove(g);
			//await dbContext.SaveChangesAsync();
			var LocalGame = mapper.Map<LocalGame>(game);
			await database.DeleteItemAsync(LocalGame);

			Snackbar.Add("Deleted Game", Severity.Info);
			await ReloadData();
		}
		#endregion
	}
}