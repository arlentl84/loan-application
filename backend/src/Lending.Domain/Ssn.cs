namespace Lending.Domain;

/// <summary>
/// Social Security Number normalized to 9 digits.
/// </summary>
public sealed record Ssn
{
    private Ssn(string value) => Value = value;

    public string Value { get; }

    public string Masked => $"***-**-{Value[^4..]}";

    public static Ssn Create(string? input)
    {
        var normalized = (input ?? string.Empty).Trim().Replace("-", "").Replace(" ", "");

        if (normalized.Length != 9 || !normalized.All(char.IsAsciiDigit))
        {
            throw new DomainValidationException("ssn", "SSN must have exactly 9 digits.");
        }

        return new Ssn(normalized);
    }

    public override string ToString() => Masked;
}
