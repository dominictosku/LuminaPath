using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.Identity;
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
            ConfigureUserRelationships(modelBuilder);
            UtcDateTimeModelConfiguration.Configure(modelBuilder);
        }

        private static void ConfigureUserRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyGame>()
                .HasOne<LuminaUser>().WithMany(u => u.MyGames).HasForeignKey(e => e.LuminaUserId);
            modelBuilder.Entity<MyAnime>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);
            modelBuilder.Entity<MyMovie>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);
            modelBuilder.Entity<MySeries>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);

            modelBuilder.Entity<Quest>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);
            modelBuilder.Entity<QuestProfile>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);
            modelBuilder.Entity<QuestSkill>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);

            modelBuilder.Entity<GamingSession>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.LuminaUserId);

            modelBuilder.Entity<UserDocument>()
                .HasOne<LuminaUser>().WithMany(u => u.Documents).HasForeignKey(e => e.UserId);

            modelBuilder.Entity<LuminaUserInfo>()
                .HasOne<LuminaUser>().WithOne(u => u.LuminaUserInfo).HasForeignKey<LuminaUserInfo>(e => e.UserId);

            modelBuilder.Entity<Friendship>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.RequesterId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Friendship>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.AddresseeId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DirectMessage>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DirectMessage>()
                .HasOne<LuminaUser>().WithMany().HasForeignKey(e => e.RecipientId).OnDelete(DeleteBehavior.Cascade);
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
            throw new InvalidOperationException($"{nameof(pg_trgm)} can only be used inside Entity Framework queries.");
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
        public DbSet<QuestSubtask> QuestSubtasks { get; set; }
        public DbSet<QuestProfile> QuestProfiles { get; set; }
        public DbSet<QuestSkill> QuestSkills { get; set; }
        public DbSet<QuestSkillNode> QuestSkillNodes { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<GameAchievement> GameAchievements { get; set; }
        public DbSet<UserGameAchievement> UserGameAchievements { get; set; }
        public DbSet<GamingSession> GamingSessions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
        public DbSet<BackgroundJobRecord> BackgroundJobs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MediaExternalId> MediaExternalIds { get; set; }
    }
}
