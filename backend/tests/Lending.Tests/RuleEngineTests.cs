using Lending.Domain;
using Lending.Domain.Rules;

namespace Lending.Tests;

public sealed class RuleEngineTests
{
    private const string BlacklistedSsn = "111-11-1111";

    private static RuleEngine EngineWithDefaultRules(params IDenyRule[] extraRules) =>
        new([new StateNotServedRule(), new BlacklistedSsnRule(new FakeBlacklist(BlacklistedSsn)), .. extraRules]);

    private static ApplicationSubmission Submission(
        string state = "TX", string ssn = "123-45-6789", decimal amount = 5_000m) =>
        ApplicationSubmission.Create(
            Ssn.Create(ssn),
            CustomerDetails.Create("Ana", "Lee", "1 Main St", "Austin", state, "73301", "Acme"),
            amount);

    [Fact]
    public void Approves_when_no_rule_denies()
    {
        var decision = EngineWithDefaultRules().Evaluate(Submission());

        Assert.True(decision.IsApproved);
        Assert.Empty(decision.Reasons);
    }

    [Theory]
    [InlineData("NY")]
    [InlineData("ny")]
    public void Denies_applications_from_New_York(string state)
    {
        var decision = EngineWithDefaultRules().Evaluate(Submission(state: state));

        Assert.False(decision.IsApproved);
        Assert.Equal("StateNotServed", Assert.Single(decision.Reasons).Code);
    }

    [Theory]
    [InlineData("111-11-1111")]
    [InlineData("111111111")]
    public void Denies_blacklisted_ssn_whatever_its_format(string ssn)
    {
        var decision = EngineWithDefaultRules().Evaluate(Submission(ssn: ssn));

        Assert.False(decision.IsApproved);
        Assert.Equal("SsnBlacklisted", Assert.Single(decision.Reasons).Code);
    }

    [Fact]
    public void Reports_every_reason_when_several_rules_deny()
    {
        var decision = EngineWithDefaultRules().Evaluate(Submission(state: "NY", ssn: BlacklistedSsn));

        Assert.False(decision.IsApproved);
        Assert.Equal(
            new[] { "SsnBlacklisted", "StateNotServed" },
            decision.Reasons.Select(r => r.Code).Order().ToArray());
    }

    [Fact]
    public void A_new_rule_is_added_without_touching_existing_ones()
    {
        var engine = EngineWithDefaultRules(new AmountTooHighRule());

        Assert.True(engine.Evaluate(Submission(amount: 50_000m)).IsApproved);

        var decision = engine.Evaluate(Submission(amount: 500_000m));
        Assert.Equal("AmountTooHigh", Assert.Single(decision.Reasons).Code);
    }

    [Fact]
    public void Rejects_malformed_input_before_any_rule_runs()
    {
        Assert.Throws<DomainValidationException>(() => Ssn.Create("123"));
        Assert.Throws<DomainValidationException>(() => Submission(state: "ZZ"));
        Assert.Throws<DomainValidationException>(() => Submission(amount: 0m));
    }

    [Fact]
    public void Ssn_is_masked_when_printed()
    {
        Assert.Equal("***-**-6789", Ssn.Create("123-45-6789").ToString());
    }

    private sealed class AmountTooHighRule : IDenyRule
    {
        public DenialReason? Evaluate(ApplicationSubmission submission) =>
            submission.RequestedAmount > 100_000m ? new DenialReason("AmountTooHigh", "Amount is too high.") : null;
    }

    private sealed class FakeBlacklist(params string[] ssns) : ISsnBlacklist
    {
        private readonly HashSet<string> _values = ssns.Select(s => Ssn.Create(s).Value).ToHashSet();

        public bool Contains(Ssn ssn) => _values.Contains(ssn.Value);
    }
}
