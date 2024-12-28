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
	public class BaseFormDialog<TEntity> : MudComponentBase
	{
		[Inject]
		public IDialogService DialogService { get; set; } = default!;

		[CascadingParameter]
		public MudDialogInstance MudDialog { get; set; } = default!;

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

		public async Task Submit()
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
