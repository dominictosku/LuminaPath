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
    public class MyGameService : GenericModelService<MyGame>
    {
        public MyGameService(IMyGameRepository repo, IMapper mapper) : base(repo, mapper) { }
    }
}
