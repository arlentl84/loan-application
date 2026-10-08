namespace Lending.Domain;

/// <summary>A validated form submission: the input the rule engine evaluates.</summary>
public sealed record ApplicationSubmission
{
    private ApplicationSubmission(Ssn ssn, CustomerDetails customer, decimal requestedAmount)
    {
        Ssn = ssn;
        Customer = customer;
        RequestedAmount = requestedAmount;
    }

    public Ssn Ssn { get; }
    public CustomerDetails Customer { get; }
    public decimal RequestedAmount { get; }

    public static ApplicationSubmission Create(Ssn ssn, CustomerDetails customer, decimal requestedAmount) =>
        new(ssn, customer, LoanApplication.NormalizeAmount(requestedAmount));
}
