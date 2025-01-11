using AutoMapper;
using LuminaPath.Components.BaseAuth;
using LuminaPath.Components.Interfaces;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Pages.Media.Games.Components;
using LuminaPath.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Runtime.CompilerServices;

namespace LuminaPath.Pages.Media
{
    public class BaseGridActions<TEntity>() : BaseAuth where TEntity : class, IMedia<MediaDocument>, new()
    {
        protected IGenericModelService<TEntity> ModelService { get; set; }
        private TEntity _currentDto = new();
        private MediaFilter _filter = new();
        private IBrowserFile? currentImage;

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public DocumentService documentService { get; set; }

        public bool IsGrid = true;
        public bool loading;
        public HashSet<TEntity> selectedItems = new();
        public string ToggleText => IsGrid ? "Grid View" : "Table View";

        public virtual async Task ReloadData() => await Task.Yield();

        public void Dummy()
        {

        }

        #region Events
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
                TEntity existing = await ModelService.GetById(id);
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
                await SaveFile(game);
                await ModelService.PostAsync(game);
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
                await SaveFile(game);
                await ModelService.PutAsync(game);
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

        public async Task Delete(TEntity g)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            await DeleteMedia(g);
            Snackbar.Add("Deleted Game", Severity.Info);
            await ReloadData();
        }

        public async Task DeleteMedia(TEntity g)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            await ModelService.DeleteAsync(g.Id);
        }

        public async Task SaveFile(TEntity game)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            if (currentImage is not null)
            {
                var result = await documentService.CreateDocument(currentImage, game);
                game.Image = result.Match<MediaDocument>(
                    s => s,
                    f => null);
            }
            currentImage = null;
        }

        public async Task SubmitFile(IBrowserFile file)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            await Task.Yield();
            currentImage = file;
        }

        public async Task DeleteImage(TEntity model)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            if (model.Image != null)
            {
                await documentService.DeleteMediaDocument(model);
            }
        }
        #endregion
    }
}
