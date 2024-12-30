using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LuminaPath.Components.BaseAuth
{
    public class BaseAuth : ComponentBase
    {
        [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject] public LuminaUserService LuminaUserService { get; set; }

        public LuminaUser? User { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var state = await AuthState;
            var user = state.User;
            if (!user.Identity.IsAuthenticated) return;
            var name = user.Identity.Name;
            UserName = name;
            UserId = user.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value;
            User = await LuminaUserService.GetUser(UserId);
        }
    }
}
