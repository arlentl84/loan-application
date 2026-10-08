using Lending.Application;

namespace Lending.Api.Contracts;

public sealed record SubmitApplicationResponse(
    string Status,
    bool IsReturningCustomer,
    Guid? CustomerId,
    Guid? ApplicationId,
    decimal? RequestedAmount,
    IReadOnlyList<DenialReasonResponse> Reasons)
{
    public static SubmitApplicationResponse From(SubmitApplicationResult result) => new(
        result.IsApproved ? "Approved" : "Denied",
        result.IsReturningCustomer,
        result.CustomerId,
        result.ApplicationId,
        result.RequestedAmount,
        result.DenialReasons.Select(reason => new DenialReasonResponse(reason.Code, reason.Message)).ToList());
}

public sealed record DenialReasonResponse(string Code, string Message);
