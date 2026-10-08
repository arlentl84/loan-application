namespace Lending.Domain.Rules;

/// <summary>Denies applications from states where the company does not operate (currently NY).</summary>
public sealed class StateNotServedRule : IDenyRule
{
    private static readonly HashSet<string> NotServed = ["NY"];

    public DenialReason? Evaluate(ApplicationSubmission submission) =>
        NotServed.Contains(submission.Customer.State)
            ? new DenialReason("StateNotServed", $"We do not currently offer loans in {submission.Customer.State}.")
            : null;
}
