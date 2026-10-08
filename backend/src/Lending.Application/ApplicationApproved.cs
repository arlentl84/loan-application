using Lending.Domain;

namespace Lending.Application;

public sealed record ApplicationApproved(
    bool IsReturningCustomer,
    CustomerSnapshot Customer,
    ApplicationSnapshot Application);

public sealed record CustomerSnapshot(
    Guid Id,
    string Ssn,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string ZipCode,
    string CompanyName)
{
    public static CustomerSnapshot From(Customer customer) => new(
        customer.Id,
        customer.Ssn.Value,
        customer.FirstName,
        customer.LastName,
        customer.Street,
        customer.City,
        customer.State,
        customer.ZipCode,
        customer.CompanyName);
}

public sealed record ApplicationSnapshot(Guid Id, decimal RequestedAmount, Guid CustomerId)
{
    public static ApplicationSnapshot From(LoanApplication application) =>
        new(application.Id, application.RequestedAmount, application.CustomerId);
}
