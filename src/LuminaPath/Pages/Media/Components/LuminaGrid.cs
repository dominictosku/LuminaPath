using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using LuminaPath.Core.Models;

namespace LuminaPath.Pages.Media.Components
{
    [CascadingTypeParameter(nameof(T))]
    public class LuminaGrid<T> : MudDataGrid<T>
    {
        public LuminaGrid()
        {
            ServerData = ServerReload;
            Filterable = true;
            FixedHeader = true;
            FixedFooter = true;
            Virtualize = true;
            Height = "calc(100vh - 370px)";
            MultiSelection = true;
            Hover = true;
        }

        [Parameter]
        public Func<Task<List<T>>> GetData { get; set; } = default!;

        private async Task<GridData<T>> ServerReload(GridState<T> state)
        {
            try
            {
                var result = await GetData();

                if (state.SortDefinitions.Count > 0)
                {
                    var firstSort = state.SortDefinitions.First();
                    result = firstSort.Descending
                        ? result.OrderByDescending(firstSort.SortFunc).ToList()
                        : result.OrderBy(firstSort.SortFunc).ToList();
                }

                if (state.FilterDefinitions.Any())
                {
                    var filterFunctions = state.FilterDefinitions.Select(x => x.GenerateFilterFunction());
                    result = result
                        .Where(x => filterFunctions.All(f => f(x)))
                        .ToList();
                }

                var totalNumberOfFilteredItems = result.Count;

                result = result
                    .Skip(state.Page * state.PageSize)
                    .Take(state.PageSize)
                    .ToList();

                return new GridData<T>
                {
                    Items = result,
                    TotalItems = totalNumberOfFilteredItems
                };
            }
            catch (TaskCanceledException)
            {
                return new GridData<T>
                {
                    Items = [],
                    TotalItems = 0
                };
            }
        }
    }
}
