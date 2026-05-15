using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Helper;
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
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LuminaPathDbContext).Assembly);
            UtcDateTimeModelConfiguration.Configure(modelBuilder);
        }

        public override int SaveChanges()
        {
            NormalizeDateTimes();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDateTimes();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDateTimes();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDateTimes();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeDateTimes()
        {
            UtcDateTimeChangeTrackerNormalizer.Normalize(ChangeTracker);
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
        public DbSet<Anime> Animes { get; set; }
        public DbSet<MyAnime> MyAnimes { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MyMovie> MyMovies { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<MySeries> MySeries { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestProfile> QuestProfiles { get; set; }
        public DbSet<QuestSkill> QuestSkills { get; set; }
        public DbSet<QuestSkillNode> QuestSkillNodes { get; set; }
        public DbSet<GamingSession> GamingSessions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
        public DbSet<MediaExternalId> MediaExternalIds { get; set; }
    }
}
