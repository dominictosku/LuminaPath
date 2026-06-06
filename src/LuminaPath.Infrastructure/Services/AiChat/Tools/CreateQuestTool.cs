using System.Globalization;
using System.Text.Json;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

/// <summary>
/// The first write-capable chat tool: creates a quest on the signed-in
/// user's board. Like the read tools it is strictly scoped to
/// <see cref="ChatToolContext.UserId"/>, so the assistant can only ever
/// mutate the caller's own data. The system prompt instructs the model to
/// confirm with the user before calling this.
/// </summary>
public sealed class CreateQuestTool : IChatTool
{
    public string Name => "create_quest";

    public bool IsWriteAction => true;

    public string Description =>
        "Create a new quest on the current user's quest board. This WRITES data, so only call it " +
        "after the user has explicitly asked to create a quest and confirmed the details in the conversation. " +
        "Returns the created quest's id, title, type and reward XP.";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {
        "title": { "type": "string", "description": "Quest title (required, non-empty)." },
        "type": { "type": "string", "enum": ["Main", "Sub", "Faction"], "default": "Sub" },
        "priority": { "type": "string", "enum": ["Low", "Medium", "High"], "default": "Medium" },
        "recurrence": { "type": "string", "enum": ["None", "Daily", "Weekly", "Monthly"], "default": "None" },
        "notes": { "type": "string", "description": "Optional free-text notes." },
        "due_date": { "type": "string", "description": "Optional due date as ISO yyyy-MM-dd." }
      },
      "required": ["title"]
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var title = ChatToolJson.OptionalString(arguments, "title")?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return ChatToolJson.Serialize(new { created = false, error = "title is required." });
        }

        var dto = new QuestCreateDto
        {
            Title = title,
            Notes = ChatToolJson.OptionalString(arguments, "notes"),
            Type = ParseEnum(ChatToolJson.OptionalString(arguments, "type"), QuestType.Sub),
            Priority = ParseEnum(ChatToolJson.OptionalString(arguments, "priority"), QuestPriority.Medium),
            Recurrence = ParseEnum(ChatToolJson.OptionalString(arguments, "recurrence"), QuestRecurrence.None),
            DueDate = ParseUtcDate(ChatToolJson.OptionalString(arguments, "due_date")),
        };

        var service = context.Services.GetRequiredService<QuestService>();
        var result = await service.CreateAsync(context.UserId, dto);

        return result.Match(
            success => ChatToolJson.Serialize(new
            {
                created = true,
                quest = new
                {
                    success.Quest.Id,
                    success.Quest.Title,
                    Type = success.Quest.Type.ToString(),
                    Priority = success.Quest.Priority.ToString(),
                    success.Quest.RewardXp,
                    DueDate = success.Quest.DueDate?.ToString("yyyy-MM-dd"),
                },
                totalXp = success.TotalXp,
            }),
            failure => ChatToolJson.Serialize(new
            {
                created = false,
                error = string.Join("; ", failure.errorMessage),
            }));
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : fallback;

    private static DateTime? ParseUtcDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
