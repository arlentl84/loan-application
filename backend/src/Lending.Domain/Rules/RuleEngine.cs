namespace Lending.Domain.Rules;

/// <summary>Runs every deny rule. The application is approved only when no rule denies it.</summary>
public sealed class RuleEngine(IEnumerable<IDenyRule> rules)
{
    private readonly IDenyRule[] _rules = rules.ToArray();

    public Decision Evaluate(ApplicationSubmission submission)
    {
        var reasons = _rules
            .Select(rule => rule.Evaluate(submission))
            .OfType<DenialReason>()
            .ToList();

        return reasons.Count == 0 ? Decision.Approve() : Decision.Deny(reasons);
    }
}
