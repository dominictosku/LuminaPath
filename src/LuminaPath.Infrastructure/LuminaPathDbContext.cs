using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure
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

		public DbSet<Document> Documents { get; set; }
		public DbSet<Game> Games { get; set; }
		public DbSet<MyGame> MyGames { get; set; }
		public DbSet<Quest> Quests { get; set; }
		public DbSet<GamesQuest> GamesQuests { get; set; }
	}
}
