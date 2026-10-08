namespace Lending.Domain.Rules;

/// <summary>
/// Main rules criteria
/// </summary>
public interface IDenyRule
{    
    DenialReason? Evaluate(ApplicationSubmission submission);
}
