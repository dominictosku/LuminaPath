using AutoMapper;
using Core.Interfaces;
using Core.Models.Base;
using Server.Controllers.Base.Generic;

namespace Server.Controllers.Base
{
	public abstract class MediaController<TEntity, TEntityDto> : GenericController<TEntity, TEntityDto> where TEntity : Media, IBasicInfo
	{
		public MediaController(IGenericRepository<TEntity> service, IMapper mapper) : base(service, mapper)
		{
			Mapper = mapper;
		}
	}
}
