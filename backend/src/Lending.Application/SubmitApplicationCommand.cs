using Lending.Domain;
using DomainSsn = Lending.Domain.Ssn;

namespace Lending.Application;

/// <summary>Raw form input. Converted to domain types (and validated) by <see cref="ToSubmission"/>.</summary>
public record SubmitApplicationCommand(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string ZipCode,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn)
{
    internal ApplicationSubmission ToSubmission() =>
        ApplicationSubmission.Create(
            DomainSsn.Create(Ssn),
            CustomerDetails.Create(FirstName, LastName, Street, City, State, ZipCode, CompanyName),
            RequestedAmount);
}
