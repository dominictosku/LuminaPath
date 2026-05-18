namespace LuminaPath.Core.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? ChangesJson { get; set; }
    public string? MetadataJson { get; set; }
    public string? ErrorMessage { get; set; }
}
