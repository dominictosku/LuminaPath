using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LuminaPath.Infrastructure.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public abstract class GenericController<TEntity, TEntityDto>(IGenericModelService<TEntity> service, IObjectMapper mapper) : ControllerBase
        where TEntity : class, IBasicInfo
        where TEntityDto : class, IBasicInfo
    {
        protected readonly IGenericModelService<TEntity> _service = service;
        protected IEnumerable<string> Includes { get; set; } = new List<string>();
        public IObjectMapper Mapper { get; } = mapper;

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
            var entity = Mapper.Map<TEntity>(viewModel);
            var result = await _service.PostAsync(entity);
            return result.Match<ActionResult>(
                m => CreatedAtAction("GetById", new { id = viewModel.Id }, Mapper.Map<TEntityDto>(m)),
                FailureToActionResult);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> PutAsync(int id, TEntityDto viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("Id does not match entity");
            }

            var entity = Mapper.Map<TEntity>(viewModel);
            var result = await _service.PutAsync(entity);
            return result.Match<IActionResult>(
                m => Ok(Mapper.Map<TEntityDto>(m)),
                FailureToActionResult);
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> DeleteAsync(int? id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Match<IActionResult>(
                m => Ok(),
                f => NotFound(f));
        }

        protected ActionResult FailureToActionResult(FailedResult failure)
        {
            if (failure is ValidationFailed validation)
            {
                var modelState = new ModelStateDictionary();
                foreach (var error in validation.Errors)
                {
                    modelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                return ValidationProblem(modelState);
            }

            return BadRequest(failure);
        }
    }
}
