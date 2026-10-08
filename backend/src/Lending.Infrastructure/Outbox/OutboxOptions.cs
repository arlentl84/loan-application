namespace Lending.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";
    
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
    
    public int MaxAttempts { get; set; } = 5;

    public int BatchSize { get; set; } = 20;
    
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}
