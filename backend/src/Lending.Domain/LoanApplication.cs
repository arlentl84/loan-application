namespace Lending.Domain;

/// <summary>An approved loan request.</summary>
public sealed class LoanApplication
{
    private LoanApplication() { } 

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal RequestedAmount { get; private set; }

    public static LoanApplication Create(Guid customerId, decimal requestedAmount) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            RequestedAmount = NormalizeAmount(requestedAmount),
        };

    public void ChangeRequestedAmount(decimal requestedAmount) =>
        RequestedAmount = NormalizeAmount(requestedAmount);
    
    public static decimal NormalizeAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("requestedAmount", "Requested amount must be greater than zero.");
        }

        return decimal.Round(amount, 2);
    }
}
