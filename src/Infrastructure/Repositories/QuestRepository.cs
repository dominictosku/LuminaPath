using Domain.Models.Quests;
using Infrastructure.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class QuestRepository : GenericRepository<GamesQuest>, IQuestRepository
    {
        public QuestRepository(LuminaPathDbContext context) : base(context) { }
    }
}
