using Core;
using Core.Models;
using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork, IDisposable
	{
		private LuminaPathDbContext context;
		private UserManager<LuminaUser> userManager;
		private GameRepo? gameRepo;
		private MyGameRepo? myGameRepo;

		public UnitOfWork(IDbContextFactory<LuminaPathDbContext> dbFactory, UserManager<LuminaUser> userManager)
		{
			context = dbFactory.CreateDbContext();
			this.userManager = userManager;
		}

		public GameRepo GameRepo
		{
			get
			{

				if (gameRepo == null)
				{
					gameRepo = new GameRepo(context);
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
					myGameRepo = new MyGameRepo(context, userManager);
				}
				return myGameRepo;
			}
		}

		public void Save()
		{
			context.SaveChanges();
		}

        public async Task SaveAsync()
        {
            await context.SaveChangesAsync();
        }

        private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					context.Dispose();
				}
			}
			this.disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}