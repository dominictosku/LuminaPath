using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class QuestsController : GenericController<GamesQuest, GamesQuestDto>
    {
        private readonly UserManager<LuminaUser> _userManager;

        public QuestsController(QuestService service, IObjectMapper mapper, UserManager<LuminaUser> userManager) : base(service, mapper)
        {
            _userManager = userManager;
        }

        [HttpPost]
        public override async Task<ActionResult> PostAsync(GamesQuestDto viewModel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Please Login");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found, please login");
            viewModel.Owner = user;

            return await base.PostAsync(viewModel);
        }
    }
}
