using Lending.Domain.Rules;

namespace Lending.Application;

public sealed record SubmitApplicationResult
{
    public bool IsApproved { get; private init; }
    public bool IsReturningCustomer { get; private init; }
    public Guid? CustomerId { get; private init; }
    public Guid? ApplicationId { get; private init; }
    public decimal? RequestedAmount { get; private init; }
    public IReadOnlyList<DenialReason> DenialReasons { get; private init; } = [];

    public static SubmitApplicationResult Approved(
        bool isReturningCustomer, Guid customerId, Guid applicationId, decimal requestedAmount) =>
        new()
        {
            IsApproved = true,
            IsReturningCustomer = isReturningCustomer,
            CustomerId = customerId,
            ApplicationId = applicationId,
            RequestedAmount = requestedAmount,
        };

    public static SubmitApplicationResult Denied(IReadOnlyList<DenialReason> reasons) =>
        new() { IsApproved = false, DenialReasons = reasons };
}
