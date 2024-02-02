using AutoMapper;
using Domain.Models.Gaming;
using Infrastructure.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GameService : GenericModelService<Game>
    {
        public GameService(IGameRepository repo, IMapper mapper) : base(repo, mapper) { }
    }
}
