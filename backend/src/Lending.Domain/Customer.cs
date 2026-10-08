namespace Lending.Domain;

/// <summary>A person applying for a loan. The SSN identifies the customer: one SSN, one Customer.</summary>
public sealed class Customer
{
    private Customer() { } 

    public Guid Id { get; private set; }
    public Ssn Ssn { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string ZipCode { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;

    public static Customer Create(Ssn ssn, CustomerDetails details)
    {
        var customer = new Customer { Id = Guid.CreateVersion7(), Ssn = ssn };
        customer.UpdateDetails(details);
        return customer;
    }
    
    public void UpdateDetails(CustomerDetails details)
    {
        FirstName = details.FirstName;
        LastName = details.LastName;
        Street = details.Street;
        City = details.City;
        State = details.State;
        ZipCode = details.ZipCode;
        CompanyName = details.CompanyName;
    }
}
