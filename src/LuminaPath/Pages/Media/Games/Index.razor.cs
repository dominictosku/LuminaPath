using Application.Common.Interfaces.Pages;
using AutoMapper;
using Domain.Common.Entities;
using Domain.Models;
using Infrastructure.Repositories;
using Infrastructure.Services;
using LuminaPath.Pages.Media.Components;
using LuminaPath.ViewModel;
using MudBlazor;

namespace LuminaPath.Pages.Media.Games
{
    public partial class Index : ITableActions<Game>
	{

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
			using var gameService = new GameService(new GameRepository(dbContextFactory.CreateDbContext()), mapper);
			_filter.Paging = new Paging(pageIndex, 15);
            var includes = new List<string>() { "Image" };
			return await gameService.GetEntities(_filter, includes);
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

		public async Task OnUpdate(Game g)
		{
			var command = mapper.Map<Game, GameViewModel>(g);
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
			using var dbContext = dbContextFactory.CreateDbContext();
			var gameRepo = new GameRepository(dbContext);
			var ids = selectedItems.Select(x => x.Id).ToArray();

			foreach (var id in ids)
			{
				Game existing = await dbContext.Games.FindAsync(id) ?? throw new Exception("id not found");
				if (existing.Image is not null)
					dbContext.Documents.Remove(existing.Image);
				dbContext.Games.Remove(existing);
			}

			await dbContext.SaveChangesAsync();
			await ReloadData();
		}

		#endregion

		#region CRUD Actions
		async Task CreateGame(GameViewModel game)
		{
			loading = true;
			try
			{
				using var dbContext = dbContextFactory.CreateDbContext();
				await dbContext.Games.AddAsync(game);
				await dbContext.SaveChangesAsync();
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
			using var dbContext = dbContextFactory.CreateDbContext();
			try
			{
				dbContext.Games.Update(game);
				await dbContext.SaveChangesAsync();
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
			using var dbContext = dbContextFactory.CreateDbContext();
			if(g.Image is not null)
				dbContext.Documents.Remove(g.Image);
			dbContext.Games.Remove(g);
			await dbContext.SaveChangesAsync();

			Snackbar.Add("Deleted Game", Severity.Info);
			await ReloadData();
		}
		#endregion
	}
}