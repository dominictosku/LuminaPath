using Core;
using Core.Models;
using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork
	{
		private readonly LuminaPathDbContext _context;
		private UserManager<LuminaUser> userManager;
		private GameRepo? gameRepo;
		private MyGameRepo? myGameRepo;

		public UnitOfWork(LuminaPathDbContext context, UserManager<LuminaUser> userManager)
		{
			_context = context;
			this.userManager = userManager;
		}

		public GameRepo GameRepo
		{
			get
			{

				if (gameRepo == null)
				{
					gameRepo = new GameRepo(_context);
				}
				return gameRepo;
			}
		}

		public MyGameRepo MyGameRepo
		{
			get
			{

				if (myGameRepo == null)
				{
					myGameRepo = new MyGameRepo(_context, userManager);
				}
				return myGameRepo;
			}
		}

		public void Save()
		{
			_context.SaveChanges();
		}

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
	}
}