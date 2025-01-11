using LuminaPath.Components.Dialogs;
using LuminaPath.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
namespace LuminaPath.Pages.Media.Components
{
    public class MediaFormDialog<TEntity, TDocument> : BaseFormDialog<TEntity> where TEntity : IMedia<TDocument> where TDocument : IDocument
    {
        private const long MaxAllowedSize = 3145728;
        public IBrowserFile? File { get; set; }

        [EditorRequired]
        [Parameter]
        public Func<TEntity, Task> OnDeleteImage { get; set; } = default!;

        [EditorRequired]
        [Parameter]
        public Func<IBrowserFile, Task> OnSubmitFile { get; set; } = default!;

        public async Task DeleteImage()
        {
            if (Model.Image != null)
            {
                var parameters = new DialogParameters<ConfirmationDialog>
            {
                { x=>x.ContentText, $"Delete Image?" }
            };
                var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
                var dialog = DialogService.Show<ConfirmationDialog>("Delete Image", parameters, options);
                var state = await dialog.Result;

                if (!state.Canceled)
                {
                    await OnDeleteImage(Model);
                }
            }
        }

        public async Task SubmitFile(IBrowserFile file)
        {
            _uploading = true;
            await OnSubmitFile(file);
            _uploading = false;
        }
    }
}
