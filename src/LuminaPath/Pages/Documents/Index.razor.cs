using Application.Common.Interfaces;
using Application.Common.Interfaces.Pages;
using Domain.Common.Entities;
using Domain.Models;
using Infrastructure.Services;
using LuminaPath.Pages.Documents.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace LuminaPath.Pages.Documents
{
	public partial class Index : ITableActions<Document>
	{
		private MudDataGrid<Document> _table = default!;
		private Document _currentDto = new();
		private MediaFilter _filter = new();

		[Inject]
		public IAzureStorage Storage { get; set; } = default!;

		[Inject]
		public ILogger<Index> Logger { get; set; } = default!;

		public bool IsGrid = true;
		public bool loading;
		public HashSet<Document> selectedItems = new();
		public string ToggleText => IsGrid ? "Grid View" : "Table View";

		private async Task<GridData<Document>> ServerReload(GridState<Document> state)
		{
			selectedItems = new HashSet<Document>();
			loading = true;
			try
			{
				var result = await GetData(state.Page + 1);

				return new GridData<Document> { TotalItems = result.Count(), Items = result };
			}
			finally
			{
				loading = false;
			}
		}

		public async Task ReloadData()
		{
			await _table.ReloadServerData();
		}

		private async Task SwitchView()
		{
			IsGrid = !IsGrid;
			await ReloadData();
		}

		private async Task<PaginatedList<Document>> GetData(int pageIndex)
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var documentService = new DocumentService(dbContext, Storage, Logger);
			var paging = new Paging(pageIndex, 15);
			return await documentService.GetAllPaginated(paging);
		}

		public void Dummy()
		{

		}

		#region Events
		public async Task OnCreate()
		{
			var command = new Document();
			var parameters = new DialogParameters<DocumentFormDialog>
		{
			{ x=>x.Refresh , new Action(async () => await ReloadData()) },
			{ x=>x.Model, command },
			{ x=>x.loading, loading },
			{ x=>x.EventCallBack, Create }
		};
			var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
			var dialog = DialogService.Show<DocumentFormDialog>("Create document", parameters, options);
			var state = await dialog.Result;
			if (!state.Canceled)
				await ReloadData();
		}

		public async Task OnUpdate(Document entity)
		{
			var command = entity;
			var parameters = new DialogParameters<DocumentFormDialog>
		{
			{ x=>x.Refresh , new Action(async () => await ReloadData()) },
			{ x=>x.Model, command },
			{ x=>x.loading, loading },
			{ x=>x.EventCallBack, Update }
		};
			var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
			var dialog = DialogService.Show<DocumentFormDialog>("Update document", parameters, options);
			var state = await dialog.Result;
			if (!state.Canceled)
				await ReloadData();
		}

		public async Task OnDeleteChecked()
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var gameService = new GameService(dbContext, mapper);
			var ids = selectedItems.Select(x => x.Id).ToArray();

			foreach (var id in ids)
			{
				await gameService.DeleteAsync(id);
			}

			await dbContext.SaveChangesAsync();
			await ReloadData();
		}

		#endregion

		#region CRUD Actions
		async Task Create(Document entity)
		{
			loading = true;
			try
			{
				using var dbContext = dbContextFactory.CreateDbContext();
				await dbContext.Documents.AddAsync(entity);
				await dbContext.SaveChangesAsync();
				Snackbar.Add("Created Document", Severity.Success);
			}
			catch (Exception ex)
			{
				Snackbar.Add("Failed to create Document", Severity.Error);
				loading = false;
			}

			await ReloadData();
			loading = false;
		}

		async Task Update(Document entity)
		{
			loading = true;
			using var dbContext = dbContextFactory.CreateDbContext();
			try
			{
				dbContext.Documents.Update(entity);
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

		public async Task Delete(Document entity)
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var documentService = new DocumentService(dbContextFactory.CreateDbContext(), Storage, logger);
			await documentService.DeleteDocument(entity);

			Snackbar.Add("Deleted Document", Severity.Info);
			await ReloadData();
		}
		#endregion
	}
}