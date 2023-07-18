using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class LuminaPathDbContext : DbContext
    {
        public LuminaPathDbContext(DbContextOptions<LuminaPathDbContext> options) : base(options)
        {
        }

        public DbSet<Games> Games { get; set; }
		public DbSet<PersonalGaming> PersonalGaming { get; set; }
	}
}
