namespace LuminaPath.Features.Media
{
    public interface ITableActions<T>
    {
        string ToggleText { get; }
        string? SearchText { get; set; }

        Task ApplySearch(string? searchText);
        Task Delete(T g);
        Task OnCreate();
        Task OnDeleteChecked();
        Task OnUpdate(T g);
        Task ReloadData();
    }
}
