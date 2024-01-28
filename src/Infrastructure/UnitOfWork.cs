using Domain;
using Domain.Common.Interfaces;
using Domain.Models;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly LuminaPathDbContext _context;
		private UserManager<LuminaUser> userManager;
		private IGameRepository? gameRepo;
		private IMyGameRepository? myGameRepo;

		public UnitOfWork(LuminaPathDbContext context, UserManager<LuminaUser> userManager)
		{
			_context = context;
			this.userManager = userManager;
		}

		public IGameRepository GameRepo
		{
			get
			{

				if (gameRepo == null)
				{
					gameRepo = new GameRepository(_context);
				}
				return gameRepo;
			}
		}

		public IMyGameRepository MyGameRepo
		{
			get
			{

				if (myGameRepo == null)
				{
					myGameRepo = new MyGameRepository(_context, userManager);
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