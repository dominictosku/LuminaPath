using System.Security.Claims;
using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers.Base;

public abstract class AuthorizedControllerBase : ControllerBase
{
    private readonly UserManager<LuminaUser> _userManager;

    protected AuthorizedControllerBase(UserManager<LuminaUser> userManager)
    {
        _userManager = userManager;
    }

    protected string? CurrentUserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    protected async Task<LuminaUser?> GetCurrentUserAsync()
    {
        return string.IsNullOrWhiteSpace(CurrentUserId)
            ? null
            : await _userManager.FindByIdAsync(CurrentUserId);
    }

    protected async Task<(LuminaUser? User, string? UserId)> GetCurrentUserWithIdAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, null);
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? (null, null) : (user, userId);
    }

    protected UnauthorizedObjectResult LoginRequired()
        => Unauthorized("Please Login");
}
