using AutoMapper;
using LuminaPath.Core.Models;
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
