using Lending.Application;
using Lending.Infrastructure;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lending.Tests.Support;

internal static class TestServices
{
    /// <summary>The real application services against a real database, with optional overrides.</summary>
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["ExternalService:BaseUrl"] = "http://localhost:1",
                ["Rules:BlacklistedSsns:0"] = "111-11-1111",
                ["Outbox:MaxAttempts"] = "3",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure();
        configure?.Invoke(services); // registered last, so it wins

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return provider;
    }

    public static SubmitApplicationCommand Command(
        string ssn = "123-45-6789",
        string state = "TX",
        decimal amount = 5_000m,
        string firstName = "Ana",
        string company = "Acme") =>
        new(firstName, "Lee", "1 Main St", "Austin", state, "73301", company, amount, ssn);

    public static async Task<SubmitApplicationResult> SubmitAsync(
        IServiceProvider provider, SubmitApplicationCommand command)
    {
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SubmitApplicationHandler>();
        return await handler.HandleAsync(command, CancellationToken.None);
    }

    public static async Task<T> QueryAsync<T>(IServiceProvider provider, Func<AppDbContext, Task<T>> query)
    {
        using var scope = provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
