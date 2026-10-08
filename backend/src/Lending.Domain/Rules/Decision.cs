namespace Lending.Domain.Rules;

public sealed record Decision(bool IsApproved, IReadOnlyList<DenialReason> Reasons)
{
    public static Decision Approve() => new(true, []);

    public static Decision Deny(IReadOnlyList<DenialReason> reasons) => new(false, reasons);
}
