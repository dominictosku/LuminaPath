using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Features.Media;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace LuminaPath.Pages.Media
{
    public abstract class MediaGridActions<TEntity, TForm>() : BaseGridActions<TEntity, TForm>, ITableActions<TEntity>
        where TEntity : class, IMedia<MediaDocument>, new()
        where TForm : IComponent
    {
        protected IGenericModelService<TEntity> ModelService { get; set; }

        [Inject]
        public DocumentService documentService { get; set; }

        public override async Task<TEntity> GetById(int id)
        {
            return await ModelService.GetById(id);
        }

        public override async Task Save(TEntity entity)
        {
            await SaveFile(entity);
            await ModelService.PostAsync(entity);
        }

        public override async Task Update(TEntity entity)
        {
            await SaveFile(entity);
            await ModelService.PutAsync(entity);
        }

        public override async Task DeleteMedia(TEntity entity)
        {
            await ModelService.DeleteAsync(entity.Id);
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
    }
}
