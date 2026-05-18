using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Mvc;
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
}
