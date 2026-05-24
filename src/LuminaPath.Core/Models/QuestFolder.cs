namespace LuminaPath.Core.Models
{
    /// <summary>
    /// A user-defined bucket for grouping quests outside the type/priority/
    /// due-date axes (e.g. "Work", "Home", "Side Projects"). Folders are
    /// flat — no parent/child — but can be visually grouped via
    /// <see cref="SectionName"/> so the UI can render section headers
    /// like "Lists" or "Pinned" without a second entity.
    /// </summary>
    public class QuestFolder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Single emoji (or short glyph string) shown beside the folder name.
        /// Stored as plain text so any Unicode emoji works without a lookup table.
        /// </summary>
        public string Emoji { get; set; } = string.Empty;

        /// <summary>Optional hex colour (#RRGGBB) for the folder accent.</summary>
        public string? Color { get; set; }

        /// <summary>
        /// Optional grouping header — folders sharing the same name render
        /// together under that label. Null means "ungrouped" (appears at top).
        /// </summary>
        public string? SectionName { get; set; }

        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string LuminaUserId { get; set; } = string.Empty;
    }
}
