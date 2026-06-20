using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestSeriesConfiguration : IEntityTypeConfiguration<QuestSeries>
    {
        public void Configure(EntityTypeBuilder<QuestSeries> builder)
        {
            builder.HasOne(series => series.MyGame)
                .WithMany()
                .HasForeignKey(series => series.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(series => series.Skill)
                .WithMany()
                .HasForeignKey(series => series.SkillId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(series => series.QuestFolder)
                .WithMany()
                .HasForeignKey(series => series.QuestFolderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(series => series.Quests)
                .WithOne(quest => quest.QuestSeries)
                .HasForeignKey(quest => quest.QuestSeriesId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(series => series.Tags)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb")
                .HasConversion(
                    tags => JsonSerializer.Serialize(tags ?? new List<string>(), (JsonSerializerOptions?)null),
                    json => DeserializeTags(json))
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                    list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list == null ? new List<string>() : list.ToList()));

            builder.HasIndex(series => new { series.LuminaUserId, series.Recurrence });
            builder.HasIndex(series => new { series.LuminaUserId, series.ScheduledStartAt });
        }

        private static List<string> DeserializeTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return new List<string>();
                }
                return doc.RootElement.EnumerateArray()
                    .Where(element => element.ValueKind == JsonValueKind.String)
                    .Select(element => element.GetString() ?? string.Empty)
                    .ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}
