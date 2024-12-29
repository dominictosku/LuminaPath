using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LuminaPath.Components.BaseAuth
{
    public class BaseAuth : ComponentBase
    {
        [Inject]
        public AuthenticationStateProvider GetAuthenticationStateAsync { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authstate = await GetAuthenticationStateAsync.GetAuthenticationStateAsync();
            var user = authstate.User;
            if (!user.Identity.IsAuthenticated) return;
            var name = user.Identity.Name;
            UserName = name;
            UserId = user.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value;
        }
    }
}
