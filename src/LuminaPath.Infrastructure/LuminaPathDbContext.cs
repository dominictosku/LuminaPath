using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.ModelConfiguration;
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
            modelBuilder.ApplyConfiguration(new GameConfiguration());
        }

        [DbFunction("pg_trgm", IsBuiltIn = true)]
        public static double pg_trgm(string a, string b)
        {
            throw new NotImplementedException();
        }

        public DbSet<Document> Documents { get; set; }
        public DbSet<MediaDocument> MediaDocuments { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<MyGame> MyGames { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<GamesQuest> GamesQuests { get; set; }
    }
}
