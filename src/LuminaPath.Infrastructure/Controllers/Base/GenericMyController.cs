using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Controllers.Base
{
    public abstract class GenericMyController<TEntity, TEntityDto>(GenericMyModelService<TEntity> service,
            UserManager<LuminaUser> userManager,
            IObjectMapper mapper) : AuthorizedControllerBase(userManager)
        where TEntity : class, IMyMedia
        where TEntityDto : class, IBasicInfo
    {
        protected readonly GenericMyModelService<TEntity> _service = service;
        protected IEnumerable<string> Includes { get; set; } = new List<string>();
        public IObjectMapper Mapper { get; } = mapper;

        [HttpGet]
        public virtual async Task<ActionResult<PaginatedResult<TEntityDto>>> Get([FromQuery] MediaFilter mediaFilter)
        {
            var (user, _) = await GetCurrentUserWithIdAsync();
            if (user is null)
            {
                return LoginRequired();
            }

            Expression<Func<TEntity, bool>> userFilter = entity => entity.LuminaUserId == user.Id;
            var result = await _service.GetAllPaginated<TEntityDto>(mediaFilter, Includes, userFilter);
            return Ok(new PaginatedResult<TEntityDto>(result));
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TEntityDto>> GetById(int? id)
        {
            var (user, _) = await GetCurrentUserWithIdAsync();
            if (user is null)
            {
                return LoginRequired();
            }

            var result = await _service.GetById(id, Includes);
            if (result.LuminaUserId != user.Id)
            {
                return NotFound();
            }

            return Ok(Mapper.Map<TEntityDto>(result));
        }

        [HttpPost]
        public virtual async Task<ActionResult> PostAsync(TEntityDto viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (user, _) = await GetCurrentUserWithIdAsync();
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

            var (user, _) = await GetCurrentUserWithIdAsync();
            if (user is null)
            {
                return LoginRequired();
            }

            // IDOR guard: load the existing row and confirm it belongs
            // to the caller BEFORE we hand the mapped entity to the
            // service. Without this check, the service would just stamp
            // LuminaUserId = current user and update — meaning anyone
            // could PUT /api/<resource>/<id> against another user's
            // record and silently take it over. Returning NotFound
            // (rather than Forbid) keeps the existence of the row
            // opaque to outsiders.
            TEntity existing;
            try
            {
                existing = await _service.GetById(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            if (existing.LuminaUserId != user.Id)
            {
                return NotFound();
            }

            var entity = Mapper.Map<TEntity>(viewModel);
            var result = await _service.PutAsync(entity, user);
            return result.Match<IActionResult>(
                m => Ok(Mapper.Map<TEntityDto>(m)),
                f => BadRequest(f));
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> DeleteAsync(int? id)
        {
            var (user, _) = await GetCurrentUserWithIdAsync();
            if (user is null)
            {
                return LoginRequired();
            }

            TEntity entity;
            try
            {
                entity = await _service.GetById(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new FailedResult("Entry not found"));
            }

            if (entity.LuminaUserId != user.Id)
            {
                return NotFound();
            }

            var result = await _service.DeleteAsync(id);
            return result.Match<IActionResult>(
                m => Ok(),
                f => NotFound(f));
        }
    }
}
