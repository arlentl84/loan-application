namespace Lending.MockExternalService;

public sealed record ExternalCustomerRequest(CustomerPayload? Customer, ApplicationPayload? Application);

public sealed record CustomerPayload(
    Guid Id,
    string? Ssn,
    string? FirstName,
    string? LastName,
    string? Street,
    string? City,
    string? State,
    string? ZipCode,
    string? CompanyName);

public sealed record ApplicationPayload(Guid Id, decimal RequestedAmount, Guid CustomerId);
