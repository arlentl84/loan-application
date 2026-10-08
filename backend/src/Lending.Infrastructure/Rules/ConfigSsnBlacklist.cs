using Lending.Domain;
using Lending.Domain.Rules;
using Microsoft.Extensions.Configuration;

namespace Lending.Infrastructure.Rules;

/// <summary>Reads the blacklist from configuration.</summary>
internal sealed class ConfigSsnBlacklist : ISsnBlacklist
{
    private readonly HashSet<string> _blacklisted;

    public ConfigSsnBlacklist(IConfiguration configuration)
    {
        var configured = configuration.GetSection("Rules:BlacklistedSsns").Get<string[]>() ?? [];
        
        _blacklisted = configured.Select(value => Ssn.Create(value).Value).ToHashSet();
    }

    public bool Contains(Ssn ssn) => _blacklisted.Contains(ssn.Value);
}
