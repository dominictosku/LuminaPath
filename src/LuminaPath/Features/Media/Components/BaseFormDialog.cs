using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace LuminaPath.Pages.Media.Components
{
    public class BaseFormDialog<TEntity> : MudComponentBase
    {
        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = default!;

        [EditorRequired]
        [Parameter]
        public TEntity Model { get; set; } = default!;

        [EditorRequired]
        [Parameter]
        public Func<TEntity, Task> EventCallBack { get; set; } = default!;

        [Parameter]
        public Action? Refresh { get; set; }


        [Parameter]
        public bool loading { get; set; }

        public string ErrorMessage = "";

        public bool _uploading;

        public async Task PreviewImage()
        {

        }

        public virtual async Task Submit()
        {
            await EventCallBack(Model);
            MudDialog.Close(DialogResult.Ok(true));
        }


        public void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}
