namespace Lending.Domain.Rules;

/// <summary>Denies applications whose SSN is blacklisted.</summary>
public sealed class BlacklistedSsnRule(ISsnBlacklist blacklist) : IDenyRule
{
    public DenialReason? Evaluate(ApplicationSubmission submission) =>
        blacklist.Contains(submission.Ssn)            
            ? new DenialReason("SsnBlacklisted", "We are unable to approve this application.")
            : null;
}
