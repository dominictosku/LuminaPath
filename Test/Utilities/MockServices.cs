using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Test.Utilities
{
    public static class MockServices
    {
        public static Mock<UserManager<LuminaUser>> UserManagerMock(LuminaUser? user = null)
        {
            var store = new Mock<IUserStore<LuminaUser>>();
            var manager = new Mock<UserManager<LuminaUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            if (user != null)
            {
                manager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            }

            return manager;
        }

        public static ControllerContext ControllerContextWithUser(string? userId)
        {
            var identity = userId == null
                ? new ClaimsIdentity()
                : new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    authenticationType: "Test");

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
