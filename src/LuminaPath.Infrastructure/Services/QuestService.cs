using AutoMapper;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Infrastructure.Services
{
	public class QuestService : GenericModelService<GamesQuest>
	{
		public QuestService(LuminaPathDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
		{
		}
	}
}
