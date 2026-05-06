using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.ModelConfiguration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
            modelBuilder.Entity<Quest>()
                .HasOne(quest => quest.LuminaUser)
                .WithMany()
                .HasForeignKey(quest => quest.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Quest>()
                .HasOne(quest => quest.MyGame)
                .WithMany(myGame => myGame.Quests)
                .HasForeignKey(quest => quest.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<GamingSession>()
                .HasOne(session => session.LuminaUser)
                .WithMany()
                .HasForeignKey(session => session.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GamingSession>()
                .HasOne(session => session.MyGame)
                .WithMany(myGame => myGame.GamingSessions)
                .HasForeignKey(session => session.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<GamingSession>()
                .HasIndex(session => new { session.LuminaUserId, session.ScheduledAt });
            modelBuilder.Entity<QuestProfile>()
                .HasIndex(profile => profile.LuminaUserId)
                .IsUnique();
            modelBuilder.Entity<QuestProfile>()
                .HasOne(profile => profile.LuminaUser)
                .WithMany()
                .HasForeignKey(profile => profile.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<QuestSkill>()
                .HasOne(skill => skill.LuminaUser)
                .WithMany()
                .HasForeignKey(skill => skill.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<QuestSkillNode>()
                .HasOne(node => node.QuestSkill)
                .WithMany(skill => skill.Nodes)
                .HasForeignKey(node => node.QuestSkillId)
                .OnDelete(DeleteBehavior.Cascade);
            ConfigureUtcDateTimes(modelBuilder);
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

        private static void ConfigureUtcDateTimes(ModelBuilder modelBuilder)
        {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                dateTime => UtcDateTime.Normalize(dateTime),
                dateTime => UtcDateTime.Normalize(dateTime));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                dateTime => UtcDateTime.Normalize(dateTime),
                dateTime => UtcDateTime.Normalize(dateTime));

            var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset),
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset));

            var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset),
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset));

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                }
            }
        }

        private void NormalizeDateTimes()
        {
            foreach (var entry in ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                NormalizeDateTimeProperties(entry);
            }
        }

        private static void NormalizeDateTimeProperties(EntityEntry entry)
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime)
                {
                    SetNormalizedValue(entry, property, UtcDateTime.Normalize(dateTime));
                }
                else if (property.CurrentValue is DateTimeOffset dateTimeOffset)
                {
                    SetNormalizedValue(entry, property, UtcDateTime.Normalize(dateTimeOffset));
                }
            }
        }

        private static void SetNormalizedValue(EntityEntry entry, PropertyEntry property, object value)
        {
            property.CurrentValue = value;
            property.Metadata.PropertyInfo?.SetValue(entry.Entity, value);
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
        public DbSet<QuestProfile> QuestProfiles { get; set; }
        public DbSet<QuestSkill> QuestSkills { get; set; }
        public DbSet<QuestSkillNode> QuestSkillNodes { get; set; }
        public DbSet<GamingSession> GamingSessions { get; set; }
    }
}
