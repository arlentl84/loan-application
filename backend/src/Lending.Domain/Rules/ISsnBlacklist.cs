namespace Lending.Domain.Rules;

/// <summary>Check blocked SSNs</summary>
public interface ISsnBlacklist
{
    bool Contains(Ssn ssn);
}
