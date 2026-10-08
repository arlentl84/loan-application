using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lending.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Lending.Tests;

[Collection(PostgresCollection.Name)]
public sealed class ApplicationsEndpointTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Approves_a_valid_application()
    {
        var body = await PostOkAsync(Form());

        Assert.Equal("Approved", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("isReturningCustomer").GetBoolean());
        Assert.Equal(5_000m, body.GetProperty("requestedAmount").GetDecimal());
        Assert.Empty(body.GetProperty("reasons").EnumerateArray());
    }

    [Fact]
    public async Task Denies_applications_from_New_York()
    {
        var body = await PostOkAsync(Form(state: "NY"));

        Assert.Equal("Denied", body.GetProperty("status").GetString());
        Assert.Equal("StateNotServed", body.GetProperty("reasons")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Denies_blacklisted_ssn()
    {
        var body = await PostOkAsync(Form(ssn: "666-66-6666"));

        Assert.Equal("Denied", body.GetProperty("status").GetString());
        Assert.Equal("SsnBlacklisted", body.GetProperty("reasons")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Same_ssn_is_a_returning_customer_with_the_same_application()
    {
        var first = await PostOkAsync(Form(amount: 5_000m));
        var second = await PostOkAsync(Form(amount: 8_000m));

        Assert.True(second.GetProperty("isReturningCustomer").GetBoolean());
        Assert.Equal(first.GetProperty("customerId").GetGuid(), second.GetProperty("customerId").GetGuid());
        Assert.Equal(first.GetProperty("applicationId").GetGuid(), second.GetProperty("applicationId").GetGuid());
        Assert.Equal(8_000m, second.GetProperty("requestedAmount").GetDecimal());
    }

    [Theory]
    [InlineData("ssn", "123")]
    [InlineData("state", "")]
    [InlineData("state", "ZZ")]
    [InlineData("requestedAmount", 0)]
    [InlineData("zipCode", "abc")]
    public async Task Rejects_invalid_input_with_400(string field, object value)
    {
        var form = Form();
        form[field] = value;

        var response = await _client.PostAsJsonAsync("/api/applications", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Dictionary<string, object> Form(
        string state = "TX", string ssn = "123-45-6789", decimal amount = 5_000m) => new()
    {
        ["firstName"] = "Ana",
        ["lastName"] = "Lee",
        ["street"] = "1 Main St",
        ["city"] = "Austin",
        ["state"] = state,
        ["zipCode"] = "73301",
        ["companyName"] = "Acme",
        ["requestedAmount"] = amount,
        ["ssn"] = ssn,
    };

    private async Task<JsonElement> PostOkAsync(Dictionary<string, object> form)
    {
        var response = await _client.PostAsJsonAsync("/api/applications", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = connectionString,
                    ["ExternalService:BaseUrl"] = "http://localhost:1",
                }));
            
            builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        }
    }
}
