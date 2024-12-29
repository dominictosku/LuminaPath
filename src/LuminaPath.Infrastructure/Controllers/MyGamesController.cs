using AutoMapper;
using LuminaPath.Core.Common.Features.Gaming.Dto;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace LuminaPath.Infrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MyGamesController : GenericController<MyGame, MyGameDto>
    {
        private new readonly MyGameService _service;
        private readonly UserManager<LuminaUser> _userManager;
        private readonly ILogger<GamesController> _logger;

        public MyGamesController(MyGameService service, UserManager<LuminaUser> userManager,
            ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
        {
            _service = service;
            _userManager = userManager;
            _logger = logger;
            Includes = new List<string> { "Game" };
        }

        [HttpPost]
        public override async Task<ActionResult> PostAsync(MyGameDto viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (user, userId) = await GetUserAndUserIdAsync(_userManager);
            var entity = Mapper.Map<MyGame>(viewModel);
            var result = await _service.PostAsync(entity, user);
            return result.Match<ActionResult>(
                m => CreatedAtAction("GetById", new { id = viewModel.Id }, Mapper.Map<MyGameDto>(m)),
                f => BadRequest(f)
                );
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> PutAsync(int id, MyGameDto viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("Id does not match entity");
            }

            var (user, userId) = await GetUserAndUserIdAsync(_userManager);
            var entity = Mapper.Map<MyGame>(viewModel);
            var result = await _service.PutAsync(entity, user);
            return result.Match<IActionResult>(
                m => Ok(Mapper.Map<MyGameDto>(m)),
                f => BadRequest(f));
        }
    }
}
