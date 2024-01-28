using Application.Controllers.Base.Generic;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Models.Base;

namespace Application.Controllers.Base
{
	public abstract class MediaController<TEntity, TEntityDto> : GenericController<TEntity, TEntityDto> where TEntity : Media, IBasicInfo
	{
		public MediaController(IGenericRepository<TEntity> service, IMapper mapper) : base(service, mapper)
		{
			Mapper = mapper;
		}
	}
}
