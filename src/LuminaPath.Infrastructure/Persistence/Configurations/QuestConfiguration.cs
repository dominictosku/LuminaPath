using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestConfiguration : IEntityTypeConfiguration<Quest>
    {
        public void Configure(EntityTypeBuilder<Quest> builder)
        {
            builder.HasOne(quest => quest.MyGame)
                .WithMany(myGame => myGame.Quests)
                .HasForeignKey(quest => quest.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(quest => quest.Skill)
                .WithMany()
                .HasForeignKey(quest => quest.SkillId)
                .OnDelete(DeleteBehavior.SetNull);

            // Deleting a folder shouldn't take its quests with it — they
            // fall back to the "Unfiled" bucket (folderId = null).
            builder.HasOne(quest => quest.QuestFolder)
                .WithMany()
                .HasForeignKey(quest => quest.QuestFolderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(quest => quest.Subtasks)
                .WithOne(subtask => subtask.Quest!)
                .HasForeignKey(subtask => subtask.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(quest => quest.Tags)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb")
                .HasConversion(
                    tags => JsonSerializer.Serialize(tags ?? new List<string>(), (JsonSerializerOptions?)null),
                    json => DeserializeTags(json))
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                    list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list == null ? new List<string>() : list.ToList()));

            builder.HasIndex(quest => new { quest.LuminaUserId, quest.Completed, quest.DueDate });
            builder.HasIndex(quest => new { quest.LuminaUserId, quest.SortOrder });
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
