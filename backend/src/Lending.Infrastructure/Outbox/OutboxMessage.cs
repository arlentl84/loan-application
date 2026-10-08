namespace Lending.Infrastructure.Outbox;

/// <summary>
/// A row written in the same transaction as the business data. 
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;
    
    public string Payload { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; }

    public string? LastError { get; set; }
}
