using System.Text.Json;
using Lending.Application;
using Lending.Infrastructure.Persistence;

namespace Lending.Infrastructure.Outbox;

/// <summary>
/// Adds the event to the DbContext. 
/// </summary>
internal sealed class OutboxWriter(AppDbContext db, TimeProvider clock) : IOutbox
{
    public void Enqueue(ApplicationApproved applicationApproved)
    {
        var now = clock.GetUtcNow();

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = nameof(ApplicationApproved),
            Payload = JsonSerializer.Serialize(applicationApproved),
            CreatedAt = now,
            NextAttemptAt = now,
        });
    }
}
