using System.Threading.RateLimiting;

namespace LuminaPath.Infrastructure.RateLimiting;

public sealed class UserActionRateLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _directMessageLimiter =
        PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 60,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromMinutes(1)
                }));

    public async ValueTask<bool> TryAcquireDirectMessageAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var lease = await _directMessageLimiter.AcquireAsync($"dm:user:{userId}", permitCount: 1, cancellationToken);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        _directMessageLimiter.Dispose();
    }
}
