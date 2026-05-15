using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace LuminaPath.Features.Auth
{
    public class BaseAuth : ComponentBase
    {
        [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject] private LuminaUserService LuminaUserService { get; set; } = default!;

        private ClaimsPrincipal? claimsPrincipal { get; set; }

        public LuminaUser? User { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        public bool CanUserEdit => claimsPrincipal is null ? false : claimsPrincipal.IsInRole("Editor") || claimsPrincipal.IsInRole("Administrator");

        protected override async Task OnInitializedAsync()
        {
            var state = await AuthState;
            var user = state.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            claimsPrincipal = user;
            var name = user.Identity.Name;
            UserName = name;
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(c => c.Type.Contains("nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value;
            if (string.IsNullOrWhiteSpace(UserId))
            {
                return;
            }

            User = await LuminaUserService.GetUser(UserId);
        }
    }
}
