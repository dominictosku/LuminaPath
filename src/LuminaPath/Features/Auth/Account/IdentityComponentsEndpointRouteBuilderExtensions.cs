using LuminaPath.Features.Auth.Account.Pages;
using LuminaPath.Features.Auth.Account.Pages.Manage;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Json;

namespace LuminaPath.Features.Auth.Account
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/PerformExternalLogin", (
                HttpContext context,
                [FromServices] SignInManager<LuminaUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            accountGroup.MapPost("/Logout", async (
                ClaimsPrincipal user,
                SignInManager<LuminaUser> signInManager,
                [FromServices] AuditLogService auditLog,
                [FromForm] string returnUrl) =>
            {
                var actor = AuditLogService.ActorFromPrincipal(user);
                await signInManager.SignOutAsync();
                await auditLog.RecordAsync(new AuditLogEntry
                {
                    Category = AuditCategories.Account,
                    Action = AuditActions.Logout,
                    Outcome = AuditOutcomes.Success,
                    Actor = actor,
                    TargetType = "User",
                    TargetId = actor.UserId,
                    TargetName = actor.Email,
                    Metadata = new { source = "Blazor" }
                });
                // After sign-out the cookie is gone, so redirecting back
                // to `returnUrl` only "works" when that URL happens to be
                // an [AllowAnonymous] page — otherwise the user lands on
                // a protected route, sees the AuthorizeRouteView's
                // <NotAuthorized> fallback, and never gets a clear
                // "you're signed out" cue. Always sending them to the
                // login page (with the original returnUrl preserved as a
                // query param so Login.razor can bounce them back after
                // sign-in) is the predictable post-logout UX.
                return TypedResults.LocalRedirect(BuildPostLogoutTarget(returnUrl));
            });

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] SignInManager<LuminaUser> signInManager,
                [FromForm] string provider) =>
            {
                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<LuminaUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                var userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(LuminaUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            return accountGroup;
        }

        /// <summary>
        /// Builds the post-logout target — always <c>~/Account/Login</c>,
        /// with the caller's original page passed through as
        /// <c>?ReturnUrl=...</c> so a successful re-login bounces back to
        /// where they were. We reject absolute / protocol-relative URLs
        /// to keep the LocalRedirect safe from open-redirect abuse, and
        /// drop returnUrls that already point at /Account/Login (which
        /// would otherwise create an ugly self-referential querystring).
        /// </summary>
        private static string BuildPostLogoutTarget(string? returnUrl)
        {
            const string LoginPath = "~/Account/Login";

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return LoginPath;
            }

            var trimmed = returnUrl.Trim();

            // Anything that looks externally addressable should not be
            // round-tripped — TypedResults.LocalRedirect would throw on
            // those anyway, but failing fast keeps the URL clean.
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                return LoginPath;
            }

            // Strip the leading slash so we don't double it up later.
            var normalized = trimmed.TrimStart('/');

            // Already at the login page — no point preserving it as a
            // returnUrl that would re-redirect to itself.
            if (normalized.StartsWith("Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                return LoginPath;
            }

            return $"{LoginPath}?ReturnUrl={Uri.EscapeDataString("/" + normalized)}";
        }
    }
}
