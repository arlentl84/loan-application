using System.Text.RegularExpressions;

namespace Lending.Domain;

/// <summary>The personal data collected by the form</summary>
public sealed record CustomerDetails
{
    private static readonly Regex ZipPattern = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

    private CustomerDetails(
        string firstName, string lastName, string street, string city, string state, string zipCode, string companyName)
    {
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        CompanyName = companyName;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string City { get; }
    
    public string State { get; }

    public string ZipCode { get; }
    public string CompanyName { get; }

    public static CustomerDetails Create(
        string? firstName, string? lastName, string? street, string? city, string? state, string? zipCode, string? companyName)
    {
        var normalizedState = Required(state, "state").ToUpperInvariant();
        if (!UsStates.IsValid(normalizedState))
        {
            throw new DomainValidationException("state", "State must be a valid two-letter US state code.");
        }

        var normalizedZip = Required(zipCode, "zipCode");
        if (!ZipPattern.IsMatch(normalizedZip))
        {
            throw new DomainValidationException("zipCode", "ZIP code must be 5 digits (or ZIP+4).");
        }

        return new CustomerDetails(
            Required(firstName, "firstName"),
            Required(lastName, "lastName"),
            Required(street, "street"),
            Required(city, "city"),
            normalizedState,
            normalizedZip,
            Required(companyName, "companyName"));
    }

    private static string Required(string? value, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException(field, $"{field} is required.");
        }

        return trimmed;
    }
}
