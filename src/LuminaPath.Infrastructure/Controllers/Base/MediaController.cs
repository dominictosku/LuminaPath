using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers.Base;

public abstract class MediaController<TMedia, TMediaDto, TService, TUserMedia> : GenericController<TMedia, TMediaDto>
    where TMedia : Media
    where TMediaDto : class, IBasicInfo
    where TService : MediaModelService<TMedia, TUserMedia>
    where TUserMedia : MyMedia, IMyMedia
{
    protected MediaController(TService service, IObjectMapper mapper)
        : base(service, mapper)
    {
        MediaService = service;
    }

    protected TService MediaService { get; }

    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public override async Task<ActionResult<PaginatedResult<TMediaDto>>> Get([FromQuery] MediaFilter mediaFilter)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await MediaService.GetAndMapEntities<TMediaDto>(mediaFilter, Includes, userId);
        return Ok(new PaginatedResult<TMediaDto>(result));
    }

    [HttpGet("{id}")]
    public override async Task<ActionResult<TMediaDto>> GetById(int? id)
    {
        return await GetByIdWithIncludes(id);
    }

    protected async Task<ActionResult<TMediaDto>> GetByIdWithIncludes(int? id, params string[] extraIncludes)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var includes = extraIncludes.Length == 0
            ? Includes
            : Includes.Concat(extraIncludes);
        var result = await MediaService.GetByIdAndMap<TMediaDto>(id, includes, userId);
        return Ok(result);
    }

    // ---------------------------------------------------------------------
    // Catalog mutation gates: any user can browse / read the catalog (the
    // class-level [Authorize] handles that), but adding / updating /
    // deleting a shared catalog entry is restricted to Administrator or
    // Editor roles via the CatalogEditors policy.
    // ---------------------------------------------------------------------

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public override Task<ActionResult> PostAsync(TMediaDto viewModel)
        => base.PostAsync(viewModel);

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public override Task<IActionResult> PutAsync(int id, TMediaDto viewModel)
        => base.PutAsync(id, viewModel);

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public override Task<IActionResult> DeleteAsync(int? id)
        => base.DeleteAsync(id);
}
