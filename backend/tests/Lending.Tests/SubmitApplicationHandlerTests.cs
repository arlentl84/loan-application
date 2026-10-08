using System.Text.Json;
using Lending.Application;
using Lending.Infrastructure.Persistence;
using Lending.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lending.Tests;

[Collection(PostgresCollection.Name)]
public sealed class SubmitApplicationHandlerTests(PostgresFixture postgres)
{
    [Fact]
    public async Task New_customer_creates_one_customer_one_application_and_one_create_event()
    {
        using var provider = TestServices.Build(postgres.NewDatabaseConnectionString());

        var result = await TestServices.SubmitAsync(provider, TestServices.Command());

        Assert.True(result.IsApproved);
        Assert.False(result.IsReturningCustomer);

        var (customers, applications, messages) = await Snapshot(provider);
        Assert.Single(customers);
        Assert.Single(applications);
        var evt = Deserialize(Assert.Single(messages));
        Assert.False(evt.IsReturningCustomer);
        Assert.Equal(customers[0].Id, evt.Customer.Id);
        Assert.Equal(applications[0].Id, evt.Application.Id);
    }

    [Fact]
    public async Task Returning_customer_updates_existing_records_and_queues_an_update_event()
    {
        using var provider = TestServices.Build(postgres.NewDatabaseConnectionString());

        var first = await TestServices.SubmitAsync(provider, TestServices.Command(amount: 5_000m));
        var second = await TestServices.SubmitAsync(
            provider,
            TestServices.Command(ssn: "123456789", state: "CA", amount: 9_000m, firstName: "Anna", company: "NewCo"));
        
        Assert.True(second.IsApproved);
        Assert.True(second.IsReturningCustomer);
        Assert.Equal(first.CustomerId, second.CustomerId);
        Assert.Equal(first.ApplicationId, second.ApplicationId);

        var (customers, applications, messages) = await Snapshot(provider);
        var customer = Assert.Single(customers);
        Assert.Equal("Anna", customer.FirstName);
        Assert.Equal("CA", customer.State);
        Assert.Equal("NewCo", customer.CompanyName);
        Assert.Equal(9_000m, Assert.Single(applications).RequestedAmount);

        var events = messages.OrderBy(m => m.CreatedAt).Select(Deserialize).ToList();
        Assert.Equal(new[] { false, true }, events.Select(e => e.IsReturningCustomer).ToArray());
        Assert.Equal(9_000m, events[1].Application.RequestedAmount);
    }

    [Fact]
    public async Task Denied_application_saves_nothing_and_queues_no_event()
    {
        using var provider = TestServices.Build(postgres.NewDatabaseConnectionString());

        var ny = await TestServices.SubmitAsync(provider, TestServices.Command(state: "NY"));
        var blacklisted = await TestServices.SubmitAsync(provider, TestServices.Command(ssn: "111-11-1111"));

        Assert.False(ny.IsApproved);
        Assert.False(blacklisted.IsApproved);

        var (customers, applications, messages) = await Snapshot(provider);
        Assert.Empty(customers);
        Assert.Empty(applications);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task Failure_while_queueing_the_event_rolls_back_customer_and_application()
    {
        using var provider = TestServices.Build(
            postgres.NewDatabaseConnectionString(),
            services => services.AddScoped<IOutbox, FailingOutbox>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TestServices.SubmitAsync(provider, TestServices.Command()));

        var (customers, applications, messages) = await Snapshot(provider);
        Assert.Empty(customers);
        Assert.Empty(applications);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task Failure_on_the_returning_path_leaves_the_existing_records_untouched()
    {
        var connectionString = postgres.NewDatabaseConnectionString();

        using var healthy = TestServices.Build(connectionString);
        await TestServices.SubmitAsync(healthy, TestServices.Command(amount: 5_000m, firstName: "Ana"));

        using var failing = TestServices.Build(
            connectionString, services => services.AddScoped<IOutbox, FailingOutbox>());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TestServices.SubmitAsync(failing, TestServices.Command(amount: 9_000m, firstName: "Anna")));

        var (customers, applications, messages) = await Snapshot(healthy);
        Assert.Equal("Ana", Assert.Single(customers).FirstName);
        Assert.Equal(5_000m, Assert.Single(applications).RequestedAmount);
        Assert.Single(messages);
    }

    private static async Task<(
        List<Lending.Domain.Customer> Customers,
        List<Lending.Domain.LoanApplication> Applications,
        List<Lending.Infrastructure.Outbox.OutboxMessage> Messages)> Snapshot(IServiceProvider provider) =>
        await TestServices.QueryAsync(provider, async (AppDbContext db) => (
            await db.Customers.AsNoTracking().ToListAsync(),
            await db.LoanApplications.AsNoTracking().ToListAsync(),
            await db.OutboxMessages.AsNoTracking().ToListAsync()));

    private static ApplicationApproved Deserialize(Lending.Infrastructure.Outbox.OutboxMessage message) =>
        JsonSerializer.Deserialize<ApplicationApproved>(message.Payload)!;
    
    private sealed class FailingOutbox : IOutbox
    {
        public void Enqueue(ApplicationApproved applicationApproved) =>
            throw new InvalidOperationException("Simulated outbox failure.");
    }
}
