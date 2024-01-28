using Domain.Common.Interfaces;
using Domain.Models;
using Domain.Models.Base;
using Domain.Models.Gaming;
using Domain.Models.Quests;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Domain
{
	public class LuminaPathDbContext : IdentityDbContext<LuminaUser>, ILuminaPathDbContext
	{
		public LuminaPathDbContext(DbContextOptions<LuminaPathDbContext> options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}

		[DbFunction(Name = "SOUNDEX", IsBuiltIn = true)]
		public static string Soundex(string query)
		{
			throw new NotImplementedException();
		}

		public DbSet<Game> Games { get; set; }
		public DbSet<MyGame> MyGames { get; set; }
		public DbSet<Quest> Quests { get; set; }
		public DbSet<GamesQuest> GamesQuests { get; set; }
	}
}
