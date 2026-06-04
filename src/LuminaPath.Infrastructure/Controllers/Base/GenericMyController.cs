using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
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

            TEntity result;
            try
            {
                result = await _service.GetById(id, user.Id, Includes);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentNullException)
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

            try
            {
                _ = await _service.GetById(id, user.Id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentNullException)
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

            try
            {
                _ = await _service.GetById(id, user.Id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new FailedResult("Entry not found"));
            }
            catch (ArgumentNullException)
            {
                return NotFound(new FailedResult("Entry not found"));
            }

            var result = await _service.DeleteAsync(id, user);
            return result.Match<IActionResult>(
                m => Ok(),
                f => NotFound(f));
        }
    }
}
