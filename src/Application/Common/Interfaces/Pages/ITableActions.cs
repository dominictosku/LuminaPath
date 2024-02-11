using Domain.Models;

namespace Application.Common.Interfaces.Pages
{
    public interface ITableActions
    {
        string ToggleText { get; }

        Task Delete(Game g);
        void Dummy();
        Task OnCreate();
        Task OnDeleteChecked();
        Task OnUpdate(Game g);
        Task ReloadData();
    }
}