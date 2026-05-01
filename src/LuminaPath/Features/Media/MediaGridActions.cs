using AutoMapper;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Features.Media;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace LuminaPath.Pages.Media
{
    public abstract class MediaGridActions<TEntity, TViewModel, TForm>() : BaseGridActions<TViewModel, TForm>, ITableActions<TViewModel>
        where TEntity : class, IMedia<MediaDocument>, new()
        where TViewModel : class, IMedia<MediaDocument>, new()
        where TForm : IComponent
    {
        protected IGenericModelService<TEntity> ModelService { get; set; } = default!;

        [Inject]
        public IMapper mapper { get; set; } = default!;

        [Inject]
        public DocumentService documentService { get; set; } = default!;

        public override async Task<TViewModel> GetById(int id)
        {
            var result = await ModelService.GetById(id);
            return mapper.Map<TViewModel>(result);
        }

        public override async Task Save(TViewModel viewModel)
        {
            var entity = mapper.Map<TEntity>(viewModel);
            await SaveFile(entity);
            await ModelService.PostAsync(entity);
        }

        public override async Task Update(TViewModel viewModel)
        {
            var entity = mapper.Map<TEntity>(viewModel);
            await SaveFile(entity);
            await ModelService.PutAsync(entity);
        }

        public override async Task DeleteMedia(TViewModel entity)
        {
            await ModelService.DeleteAsync(entity.Id);
        }

        public async Task SaveFile(TEntity entity)
        {
            if (!CanUserEdit)
            {
                Snackbar.Add("No permission to edit", Severity.Info);
                return;
            }
            if (currentImage is not null)
            {
                var result = await documentService.CreateDocument(currentImage, entity);
                entity.Image = result.Match<MediaDocument?>(
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
