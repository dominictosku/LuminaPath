using LuminaPath.Components.Dialogs;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Features.Auth;
using LuminaPath.Features.Media;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace LuminaPath.Pages.Media
{
    public abstract class BaseGridActions<TEntity, TForm>() : BaseAuth, ITableActions<TEntity> 
        where TEntity : class, IBasicInfo, new()
        where TForm : IComponent
    {
        protected TEntity _currentDto = new();
        protected MediaFilter _filter = new();
        protected IBrowserFile? currentImage;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        public bool IsGrid = true;
        public bool loading;
        public HashSet<TEntity> selectedItems = new();
        public string ToggleText => IsGrid ? "Grid View" : "Table View";
        public string? SearchText
        {
            get => _filter.SearchString;
            set => _filter.SearchString = value;
        }

        protected virtual string EntityLabel => "Media";

        public virtual async Task ReloadData() => await Task.Yield();

        public async Task ApplySearch(string? searchText)
        {
            SearchText = searchText;
            await ReloadData();
        }

        #region Events
        public async Task OnCreate()
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            var command = new TEntity();
            var parameters = CreateDialogParameters(command, CreateGame);
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await DialogService.ShowAsync<TForm>("Create Media", parameters, options);
            if (!dialog.Result.IsCanceled)
                await ReloadData();
        }

        public async Task OnUpdate(TEntity media)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            var command = media;
            var parameters = CreateDialogParameters(media, UpdateGame);
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await DialogService.ShowAsync<TForm>("Update Media", parameters, options);
            if (!dialog.Result.IsCanceled)
                await ReloadData();
        }

        public async Task OnDeleteChecked()
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }

            var ids = selectedItems.Select(x => x.Id).ToArray();
            if (ids.Length == 0)
            {
                return;
            }

            var confirmed = await ConfirmDelete(
                GetBulkDeleteTitle(ids.Length),
                GetBulkDeleteMessage(ids.Length),
                GetBulkDeleteDetail(ids.Length),
                GetDeleteConfirmText(ids.Length));
            if (!confirmed)
            {
                return;
            }

            loading = true;
            await InvokeAsync(StateHasChanged);

            var deletedCount = 0;
            try
            {
                foreach (var id in ids)
                {
                    TEntity existing = await GetById(id);
                    await DeleteMedia(existing);
                    deletedCount++;
                }

                selectedItems.Clear();
                Snackbar.Add($"Deleted {deletedCount} {EntityLabel.ToLowerInvariant()}{(deletedCount == 1 ? string.Empty : " items")}", Severity.Info);
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to delete selected {EntityLabel.ToLowerInvariant()} items", Severity.Error);
            }
            finally
            {
                loading = false;
                await ReloadData();
                await InvokeAsync(StateHasChanged);
            }
        }
        #endregion

        #region CRUD Actions
        public async Task CreateGame(TEntity game)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }

            loading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                await Save(game);
                Snackbar.Add($"Created {EntityLabel}", Severity.Success);
                await ReloadData();
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to create {EntityLabel}", Severity.Error);
            }
            finally
            {
                loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public async Task UpdateGame(TEntity game)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }

            loading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                await Update(game);
                Snackbar.Add($"Updated {EntityLabel}", Severity.Success);
                await ReloadData();
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to update {EntityLabel}", Severity.Error);
            }
            finally
            {
                loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public async Task Delete(TEntity g)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }

            if (!await ConfirmDelete(
                    GetDeleteTitle(g),
                    GetDeleteMessage(g),
                    GetDeleteDetail(g),
                    GetDeleteConfirmText(1)))
            {
                return;
            }

            loading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                await DeleteMedia(g);
                selectedItems.Remove(g);
                Snackbar.Add($"Deleted {EntityLabel}", Severity.Info);
                await ReloadData();
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to delete {EntityLabel}", Severity.Error);
            }
            finally
            {
                loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public abstract Task<TEntity> GetById(int id);
        public abstract Task Save(TEntity entity);
        public abstract Task Update(TEntity entity);
        public abstract Task DeleteMedia(TEntity entity);

        protected abstract DialogParameters<TForm> CreateDialogParameters(TEntity command, Func<TEntity, Task> OnSubmit);
        #endregion

        protected virtual string GetDeleteTitle(TEntity entity) => $"Delete {EntityLabel.ToLowerInvariant()}?";

        protected virtual string GetDeleteMessage(TEntity entity) => $"Are you sure you want to delete this {EntityLabel.ToLowerInvariant()}?";

        protected virtual string? GetDeleteDetail(TEntity entity) => "This cannot be undone.";

        protected virtual string GetBulkDeleteTitle(int count) => $"Delete selected {EntityLabel.ToLowerInvariant()} items?";

        protected virtual string GetBulkDeleteMessage(int count) => $"Are you sure you want to delete {count} selected {EntityLabel.ToLowerInvariant()}{(count == 1 ? string.Empty : " items")}?";

        protected virtual string? GetBulkDeleteDetail(int count) => "This cannot be undone.";

        protected virtual string GetDeleteConfirmText(int count) => count == 1 ? "Delete" : $"Delete {count}";

        protected async Task<bool> ConfirmDelete(string title, string message, string? detail = null, string confirmText = "Delete")
        {
            var parameters = new DialogParameters<ConfirmationDialog>
            {
                { x => x.TitleText, title },
                { x => x.SubtitleText, "Please confirm before continuing." },
                { x => x.ContentText, message },
                { x => x.DetailText, detail },
                { x => x.ConfirmText, confirmText },
                { x => x.ConfirmIcon, Icons.Material.Filled.Delete },
                { x => x.ConfirmColor, Color.Error },
                { x => x.Severity, Severity.Error }
            };
            var options = new DialogOptions { CloseButton = true, CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm delete", parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }
    }
}
