using AutoMapper;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Infrastructure.Controllers.Base
{
    public abstract class GenericMyController<TEntity, TEntityDto>(GenericMyModelService<TEntity> service,
            UserManager<LuminaUser> userManager,
            IMapper mapper) : ControllerBase
        where TEntity : class, IMyMedia
        where TEntityDto : class, IBasicInfo
    {
        protected readonly GenericMyModelService<TEntity> _service = service;
        protected readonly UserManager<LuminaUser> _userManager = userManager;
        protected IEnumerable<string> Includes { get; set; } = new List<string>();
        public IMapper Mapper = mapper;

        [HttpGet]
        public virtual async Task<ActionResult<PaginatedResult<TEntityDto>>> Get([FromQuery] MediaFilter mediaFilter)
        {
            var result = await _service.GetAllPaginated<TEntityDto>(mediaFilter, Includes);
            return Ok(new PaginatedResult<TEntityDto>(result));
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TEntityDto>> GetById(int? id)
        {
            var result = await _service.GetById(id, Includes);
            return Ok(Mapper.Map<TEntityDto>(result));
        }

        [HttpPost]
        public virtual async Task<ActionResult> PostAsync(TEntityDto viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (user, userId) = await GetUserAndUserIdAsync(_userManager);
            var entity = Mapper.Map<TEntity>(viewModel);
            var result = await _service.PostAsync(entity, user);
            return result.Match<ActionResult>(
                m => CreatedAtAction("GetById", new { id = viewModel.Id }, Mapper.Map<TEntityDto>(m)),
                f => BadRequest(f)
                );
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> PutAsync(int id, TEntityDto viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("Id does not match entity");
            }

            var (user, userId) = await GetUserAndUserIdAsync(_userManager);

            var entity = Mapper.Map<TEntity>(viewModel);
            var result = await _service.PutAsync(entity, user);
            return result.Match<IActionResult>(
                m => Ok(Mapper.Map<TEntityDto>(m)),
                f => BadRequest(f));
        }

        [HttpDelete]
        public virtual async Task<IActionResult> DeleteAsync(int? id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Match<IActionResult>(
                m => Ok(),
                f => NotFound(f));
        }

        protected async Task<(LuminaUser? user, string? UserId)> GetUserAndUserIdAsync(UserManager<LuminaUser> userManager)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return (null, null);

            LuminaUser? user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return (null, null);
            return (user, userId);
        }
    }
}
