using Data.Models;
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

		public DbSet<Games> Games { get; set; }
		public DbSet<PersonalGaming> PersonalGaming { get; set; }
	}
}
