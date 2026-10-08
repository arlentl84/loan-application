using System.ComponentModel.DataAnnotations;
using Lending.Application;

namespace Lending.Api.Contracts;

/// <summary>
/// Shape and size checks live here (the framework turns failures into a 400 with every error).
/// Business invariants (valid state code, SSN digits...) are enforced again by the domain.
/// </summary>
public sealed class SubmitApplicationRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Street { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required, RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "State must be a two-letter code.")]
    public string State { get; init; } = string.Empty;

    [Required, RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "ZIP code must be 5 digits (or ZIP+4).")]
    public string ZipCode { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string CompanyName { get; init; } = string.Empty;

    [Range(0.01, 10_000_000, ErrorMessage = "Requested amount must be between 0.01 and 10,000,000.")]
    public decimal RequestedAmount { get; init; }

    [Required, RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must have 9 digits.")]
    public string Ssn { get; init; } = string.Empty;

    public SubmitApplicationCommand ToCommand() =>
        new(FirstName, LastName, Street, City, State, ZipCode, CompanyName, RequestedAmount, Ssn);
}
