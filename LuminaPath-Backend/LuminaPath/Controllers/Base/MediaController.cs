using AutoMapper;
using Data.Interfaces;
using Data.Models.Base;
using Data.Models.Dto;
using LuminaPath.Controllers.Base.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Controllers.Base
{
	public abstract class MediaController<TEntity, TEntityDto> : GenericController<TEntity, TEntityDto> where TEntity : Media, IBasicInfo
	{
		public MediaController(IGenericRepo<TEntity> service, IMapper mapper) : base(service, mapper)
		{
			Mapper = mapper;
		}

		[HttpGet("{id}")]
		[AllowAnonymous]
		public override async Task<ActionResult<TEntityDto>> GetById(int? id)
		{
			return await base.GetById(id);
		}
	}
}
