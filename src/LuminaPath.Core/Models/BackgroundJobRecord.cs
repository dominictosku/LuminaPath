using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Models;

public class BackgroundJobRecord
{
    public int Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
