using MudBlazor;
using LuminaPath.UI.Shared.Media;
using LuminaPath.MauiClientApp.Database;
using LuminaPath.UI.Shared.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Common.Entities;

namespace LuminaPath.MauiClientApp.Pages
{
	public partial class GamesPage : ITableActions<Game>
	{
		private GamesDatabase database;
		private List<Game> Games = new();
		private MudDataGrid<Game> _table = default!;
		private Game _currentDto = new();
		private MediaFilter _filter = new();

		public bool IsGrid = true;
		public bool loading;
		public HashSet<Game> selectedItems = new();
		public string ToggleText => IsGrid ? "Grid View" : "Table View";

		private async Task<GridData<Game>> ServerReload(GridState<Game> state)
		{
			selectedItems = new HashSet<Game>();
			loading = true;
			try
			{
				var result = await GetData(state.Page + 1);

				return new GridData<Game> { TotalItems = result.Count(), Items = result };
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

		private async Task<PaginatedList<Game>> GetData(int pageIndex)
		{
			_filter.Paging = new Paging(pageIndex, 15);
			var includes = new List<string>() { "Image" };
			//return await gameService.GetEntities(_filter, includes);
			var games = await database.GetItemsAsync();
			return await PaginatedList<Game>.CreateAsync(games.AsQueryable(),0,100);
		}

		public void Dummy()
		{

		}

		#region Events
		public async Task OnCreate()
		{
			var command = new Game();
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

		public async Task OnUpdate(Game g)
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
				Game existing = await database.GetItemAsync(id) ?? throw new Exception("id not found");
				if (existing.Image is not null)
				{
					//dbContext.Documents.Remove(existing.Image);
				}
				await database.DeleteItemAsync((Models.Game)existing);
			}
			await ReloadData();
		}

		#endregion

		#region CRUD Actions
		async Task CreateGame(Game game)
		{
			loading = true;
			try
			{
				await database.SaveItemAsync((Models.Game)game);
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

		async Task UpdateGame(Game game)
		{
			loading = true;
			try
			{
				await database.SaveItemAsync((Models.Game)game);
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

		public async Task Delete(Game g)
		{
			//if (g.Image is not null)
			//	dbContext.Documents.Remove(g.Image);
			//dbContext.Games.Remove(g);
			//await dbContext.SaveChangesAsync();
			await database.DeleteItemAsync((Models.Game)g);

			Snackbar.Add("Deleted Game", Severity.Info);
			await ReloadData();
		}
		#endregion
	}
}