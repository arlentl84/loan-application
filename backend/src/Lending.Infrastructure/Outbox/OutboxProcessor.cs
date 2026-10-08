using System.Text.Json;
using Lending.Application;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lending.Infrastructure.Outbox;

/// <summary>
/// Background worker: delivers pending outbox messages to the external service, outside the HTTP
/// request that created them.
/// </summary>
public sealed class OutboxProcessor(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, clock);

        try
        {
            do
            {
                try
                {
                    await ProcessPendingAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Outbox polling failed; it will be retried on the next tick.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Application is shutting down.
        }
    }
  
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IExternalCustomerGateway>();

        var settings = options.Value;
        var now = clock.GetUtcNow();

        var due = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < settings.MaxAttempts && m.NextAttemptAt <= now)
            .OrderBy(m => m.CreatedAt)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in due)
        {
            await DeliverAsync(message, gateway, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    private async Task DeliverAsync(
        OutboxMessage message, IExternalCustomerGateway gateway, CancellationToken cancellationToken)
    {
        try
        {
            var applicationApproved = JsonSerializer.Deserialize<ApplicationApproved>(message.Payload)
                ?? throw new InvalidOperationException("Outbox payload is empty.");

            await gateway.SendAsync(applicationApproved, cancellationToken);

            message.ProcessedAt = clock.GetUtcNow();
            message.LastError = null;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {            
            message.Attempts++;
            message.LastError = Truncate(ex.Message, 1000);
            message.NextAttemptAt = clock.GetUtcNow() + RetryDelay(message.Attempts);

            logger.LogWarning(
                "Outbox message {MessageId} failed (attempt {Attempts}/{MaxAttempts}): {Error}",
                message.Id, message.Attempts, options.Value.MaxAttempts, message.LastError);
        }
    }

    private TimeSpan RetryDelay(int attempts)
    {
        var seconds = options.Value.BaseRetryDelay.TotalSeconds * Math.Pow(2, attempts - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, 300));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
