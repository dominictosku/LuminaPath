using Data.Models;
using Data.Models.Base;
using Data.Models.Gaming;
using Data.Models.Quests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
	public class LuminaPathDbContext : IdentityDbContext<LuminaUser>
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
