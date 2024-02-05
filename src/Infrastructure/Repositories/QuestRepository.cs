using Application.Common.Interfaces.Repositories;
using Domain.Models;
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
