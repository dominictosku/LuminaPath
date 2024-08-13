using LuminaPath.Core.Models;

namespace LuminaPath.UI.Shared.Interfaces
{
	public interface ITableActions<T>
	{
		string ToggleText { get; }

		Task Delete(T g);
		void Dummy();
		Task OnCreate();
		Task OnDeleteChecked();
		Task OnUpdate(T g);
		Task ReloadData();
	}
}