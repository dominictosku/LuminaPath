using AutoMapper;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Models;
using LuminaPath.UI.Shared.Interfaces;
using LuminaPath.UI.Shared.Media;
using LuminaPath.ViewModel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using MudBlazor;
using LuminaPath.Pages.Media.Components;
using LuminaPath.Infrastructure;

namespace LuminaPath.Pages.Media.Games
{
	public partial class Index : ITableActions<Game>
	{

		private List<Game> Games = new();
		private MudDataGrid<Game> _table = default!;
		private Game _currentDto = new();
		private MediaFilter _filter = new();
		private IBrowserFile? currentImage;

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
			using var gameService = new GameService(dbContextFactory.CreateDbContext(), mapper);
			_filter.Paging = new Paging(pageIndex, 15);
			var includes = new List<string>() { "Image" };
			return await gameService.GetAllPaginated(_filter, includes);
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
				{ x=>x.EventCallBack, CreateGame },
				{ x=>x.OnSubmitFile, SubmitFile },
				{ x=>x.OnDeleteImage, DeleteImage }
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
				{ x=>x.EventCallBack, UpdateGame },
				{ x=>x.OnSubmitFile, SubmitFile },
				{ x=>x.OnDeleteImage, DeleteImage }
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
			var ids = selectedItems.Select(x => x.Id).ToArray();

			foreach (var id in ids)
			{
				Game existing = await dbContext.Games.FindAsync(id) ?? throw new Exception("id not found");
                await DeleteMedia(existing);
            }

			await dbContext.SaveChangesAsync();
			await ReloadData();
		}

		#endregion

		#region CRUD Actions
		async Task CreateGame(Game game)
		{
			loading = true;
			try
			{
				using var dbContext = dbContextFactory.CreateDbContext();
				await SaveFile(dbContext, game);
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

		async Task UpdateGame(Game game)
		{
			loading = true;
			using var dbContext = dbContextFactory.CreateDbContext();
			try
			{
				await SaveFile(dbContext, game);
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
			await DeleteMedia(g);
			Snackbar.Add("Deleted Game", Severity.Info);
			await ReloadData();
		}

		private async Task DeleteMedia(Game g)
		{
            using var dbContext = dbContextFactory.CreateDbContext();
            var documentService = new DocumentService(dbContextFactory.CreateDbContext(), Storage, logger);
            await documentService.DeleteMediaDocument(g);
            dbContext.Games.Remove(g);
            await dbContext.SaveChangesAsync();
        }

        private async Task SaveFile(LuminaPathDbContext context, Game game)
        {
            if (currentImage is not null)
            {
                var documentService = new DocumentService(dbContextFactory.CreateDbContext(), Storage, logger);
                var result = await documentService.CreateDocument(currentImage, game);
                game.Image = result.Match<Document>(
                    s => s,
                    f => null);
            }
			currentImage = null;
        }

        private async Task SubmitFile(IBrowserFile file)
		{
			await Task.Yield();
			currentImage = file;
		}

		private async Task DeleteImage(Game model)
		{
			if (model.Image != null)
			{
				var documentService = new DocumentService(dbContextFactory.CreateDbContext(), Storage, logger);
				await documentService.DeleteMediaDocument(model);
			}
		}
		#endregion
	}
}