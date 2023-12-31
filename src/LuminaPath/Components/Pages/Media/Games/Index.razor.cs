using Core;
using Core.Models.Gaming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace LuminaPath.Components.Pages.Media.Games
{
    public partial class Index
    {
        [Inject]
        public LuminaPathDbContext Context { get; set; }
        PaginationState pagination = new PaginationState { ItemsPerPage = 15 };
        string nameFilter = string.Empty;
        private IQueryable<Game>? games;

        protected override async Task OnInitializedAsync()
        {
            games = Context.Games;
        }
    }
}