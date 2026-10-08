using Npgsql;
using Testcontainers.PostgreSql;

namespace Lending.Tests.Support;

/// <summary>
/// One real PostgreSQL container for the whole test run (needs Docker). Each test gets its own
/// database, so tests are isolated and can run without cleaning up. The EF Core in-memory provider
/// is not used because it has no real transactions, which is exactly what these tests verify.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string NewDatabaseConnectionString() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = $"test_{Guid.NewGuid():N}",
        }.ConnectionString;
}

[CollectionDefinition(PostgresCollection.Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
