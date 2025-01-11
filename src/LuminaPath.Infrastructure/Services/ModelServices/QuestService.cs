using AutoMapper;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class QuestService : GenericModelService<GamesQuest>
    {
        public QuestService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IMapper mapper) : base(dbContextFactory, mapper)
        {
        }
    }
}
