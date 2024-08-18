using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.UI.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace LuminaPath.UI.Shared.Media
{
	public class BaseFormDialog<TEntity, TDocument> : MudComponentBase where TEntity : IMedia<TDocument> where TDocument : IDocument
	{
		[Inject]
		public IDialogService DialogService { get; set; } = default!;

		[CascadingParameter]
		private MudDialogInstance MudDialog { get; set; } = default!;

		[EditorRequired]
		[Parameter]
		public TEntity Model { get; set; } = default!;

		[EditorRequired]
		[Parameter]
		public Func<TEntity, Task> EventCallBack { get; set; } = default!;

		[EditorRequired]
		[Parameter]
		public Func<TEntity, Task> OnDeleteImage { get; set; } = default!;

		[EditorRequired]
		[Parameter]
		public Func<IBrowserFile, Task<TDocument?>> OnSubmitFile { get; set; } = default!;

		[Parameter]
		public Action? Refresh { get; set; }


		[Parameter]
		public bool loading { get; set; }

		public string ErrorMessage = "";

		public bool _uploading;


		private const long MaxAllowedSize = 3145728;
		public IBrowserFile? File { get; set; }

		public async Task Submit()
		{
			await EventCallBack(Model);
			MudDialog.Close(DialogResult.Ok(true));
		}

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

		public async Task PreviewImage()
		{

		}

		public async Task SubmitFile(IBrowserFile file)
		{
			_uploading = true;
			Model.Image = await OnSubmitFile(file);
			_uploading = false;
		}

		public void Cancel()
		{
			MudDialog.Cancel();
		}
	}
}
