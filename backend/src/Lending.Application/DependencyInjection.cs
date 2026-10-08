using Lending.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Lending.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {        
        AddRules(services);

        services.AddScoped<RuleEngine>();
        services.AddScoped<SubmitApplicationHandler>();

        return services;
    }

    private static void AddRules(IServiceCollection services)
    {
        services.AddScoped<IDenyRule, BlacklistedSsnRule>();
        services.AddScoped<IDenyRule, StateNotServedRule>();        
    }
}
