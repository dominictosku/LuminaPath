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
        protected virtual string EntityLabel => "Media";

        public virtual async Task ReloadData() => await Task.Yield();

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

            foreach (var id in ids)
            {
                TEntity existing = await GetById(id);
                await DeleteMedia(existing);
            }

            await ReloadData();
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
            try
            {
                await Save(game);
                Snackbar.Add($"Created {EntityLabel}", Severity.Success);
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to create {EntityLabel}", Severity.Error);
                loading = false;
            }

            await ReloadData();
            loading = false;
        }

        public async Task UpdateGame(TEntity game)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            loading = true;
            try
            {
                await Update(game);
                Snackbar.Add($"Updated {EntityLabel}", Severity.Success);
            }
            catch (Exception)
            {
                Snackbar.Add($"Failed to update {EntityLabel}", Severity.Error);
                loading = false;
            }

            await ReloadData();
            loading = false;
        }

        public async Task Delete(TEntity g)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete Media");
            var result = await dialog.Result;
            if (result == null || result.Canceled)
                return;

            await DeleteMedia(g);
            Snackbar.Add($"Deleted {EntityLabel}", Severity.Info);
            await ReloadData();
        }

        public abstract Task<TEntity> GetById(int id);
        public abstract Task Save(TEntity entity);
        public abstract Task Update(TEntity entity);
        public abstract Task DeleteMedia(TEntity entity);

        protected abstract DialogParameters<TForm> CreateDialogParameters(TEntity command, Func<TEntity, Task> OnSubmit);
        #endregion
    }
}
