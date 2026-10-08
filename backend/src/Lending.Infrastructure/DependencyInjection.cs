using Lending.Application;
using Lending.Domain.Rules;
using Lending.Infrastructure.ExternalService;
using Lending.Infrastructure.Outbox;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lending.Infrastructure;

public static class DependencyInjection
{   
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, OutboxWriter>();

        services.AddSingleton<ISsnBlacklist>(provider =>
            new ConfigSsnBlacklist(provider.GetRequiredService<IConfiguration>()));

        services.AddHttpClient<IExternalCustomerGateway, ExternalServiceClient>((provider, client) =>
        {
            var baseUrl = provider.GetRequiredService<IConfiguration>()["ExternalService:BaseUrl"]
                ?? throw new InvalidOperationException("ExternalService:BaseUrl is not configured.");
            
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName);
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
