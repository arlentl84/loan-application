using Lending.Domain;
using Lending.Domain.Rules;

namespace Lending.Application;

/// <summary>
/// SubmitApplicationHandler
/// </summary>
public sealed class SubmitApplicationHandler( 
    RuleEngine ruleEngine, 
    ICustomerRepository customers, 
    ILoanApplicationRepository applications,
    IOutbox outbox,
    IUnitOfWork unitOfWork)
{    
    private const int MaxAttempts = 2;

    public async Task<SubmitApplicationResult> HandleAsync(
        SubmitApplicationCommand command, CancellationToken cancellationToken)
    {
        var submission = command.ToSubmission();

        var decision = ruleEngine.Evaluate(submission);
        if (!decision.IsApproved)
        {
            return SubmitApplicationResult.Denied(decision.Reasons);
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await unitOfWork.ExecuteAsync(
                    token => ApproveAsync(submission, token), cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                // Retry: the customer now exists.
            }
        }
    }

    private async Task<SubmitApplicationResult> ApproveAsync(
        ApplicationSubmission submission, CancellationToken cancellationToken)
    {
        var customer = await customers.FindBySsnAsync(submission.Ssn, cancellationToken);
        var isReturningCustomer = customer is not null;
        LoanApplication application;

        if (customer is null)
        {
            customer = Customer.Create(submission.Ssn, submission.Customer);
            customers.Add(customer);

            application = LoanApplication.Create(customer.Id, submission.RequestedAmount);
            applications.Add(application);
        }
        else
        {
            customer.UpdateDetails(submission.Customer);

            var existing = await applications.FindByCustomerIdAsync(customer.Id, cancellationToken);
            if (existing is null)
            {                
                application = LoanApplication.Create(customer.Id, submission.RequestedAmount);
                applications.Add(application);
            }
            else
            {
                existing.ChangeRequestedAmount(submission.RequestedAmount);
                application = existing;
            }
        }

        outbox.Enqueue(new ApplicationApproved(
            isReturningCustomer,
            CustomerSnapshot.From(customer),
            ApplicationSnapshot.From(application)));

        return SubmitApplicationResult.Approved(
            isReturningCustomer, customer.Id, application.Id, application.RequestedAmount);
    }
}
