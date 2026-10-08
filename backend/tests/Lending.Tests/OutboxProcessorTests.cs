using Lending.Application;
using Lending.Infrastructure.Outbox;
using Lending.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lending.Tests;

[Collection(PostgresCollection.Name)]
public sealed class OutboxProcessorTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Delivers_pending_events_and_marks_them_processed()
    {
        var gateway = new StubGateway();
        using var provider = TestServices.Build(
            postgres.NewDatabaseConnectionString(), services => services.AddScoped<IExternalCustomerGateway>(_ => gateway));
        await TestServices.SubmitAsync(provider, TestServices.Command());

        var attempted = await ProcessorOf(provider).ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, attempted);
        var sent = Assert.Single(gateway.Sent);
        Assert.False(sent.IsReturningCustomer);
        var message = await Single(provider);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task Failed_delivery_is_kept_and_retried_later_with_backoff()
    {
        var gateway = new StubGateway { Failure = new HttpRequestException("503 from external service") };
        using var provider = TestServices.Build(
            postgres.NewDatabaseConnectionString(), services => services.AddScoped<IExternalCustomerGateway>(_ => gateway));
        await TestServices.SubmitAsync(provider, TestServices.Command());
        var processor = ProcessorOf(provider);

        await processor.ProcessPendingAsync(CancellationToken.None);

        var message = await Single(provider);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("503", message.LastError);
        Assert.True(message.NextAttemptAt > DateTimeOffset.UtcNow);
        
        Assert.Equal(0, await processor.ProcessPendingAsync(CancellationToken.None));
        Assert.Single(gateway.Sent);
    }

    private static OutboxProcessor ProcessorOf(IServiceProvider provider) =>
        provider.GetServices<IHostedService>().OfType<OutboxProcessor>().Single();

    private static Task<OutboxMessage> Single(IServiceProvider provider) =>
        TestServices.QueryAsync(provider, db => db.OutboxMessages.AsNoTracking().SingleAsync());

    private sealed class StubGateway : IExternalCustomerGateway
    {
        public List<ApplicationApproved> Sent { get; } = [];

        public Exception? Failure { get; init; }

        public Task SendAsync(ApplicationApproved applicationApproved, CancellationToken cancellationToken)
        {
            Sent.Add(applicationApproved);
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
