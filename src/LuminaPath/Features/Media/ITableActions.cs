namespace LuminaPath.Features.Media
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