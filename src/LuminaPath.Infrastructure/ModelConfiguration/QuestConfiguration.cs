using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestConfiguration : IEntityTypeConfiguration<Quest>
    {
        public void Configure(EntityTypeBuilder<Quest> builder)
        {
            builder.HasOne(quest => quest.LuminaUser)
                .WithMany()
                .HasForeignKey(quest => quest.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(quest => quest.MyGame)
                .WithMany(myGame => myGame.Quests)
                .HasForeignKey(quest => quest.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(quest => quest.Tags)
                .HasColumnType("jsonb")
                .HasConversion(
                    tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                    json => string.IsNullOrWhiteSpace(json)
                        ? new List<string>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                    list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list == null ? new List<string>() : list.ToList()));

            builder.HasIndex(quest => new { quest.LuminaUserId, quest.Completed, quest.DueDate });
            builder.HasIndex(quest => new { quest.LuminaUserId, quest.SortOrder });
        }
    }
}
