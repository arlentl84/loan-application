namespace Lending.Domain.Rules;

/// <param name="Code">Stable identifier for clients and tests.</param>
/// <param name="Message">Safe to show to the applicant. Must not reveal sensitive criteria.</param>
public record DenialReason(string Code, string Message);
