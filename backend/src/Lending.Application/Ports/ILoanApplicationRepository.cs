using Lending.Domain;

namespace Lending.Application;

public interface ILoanApplicationRepository
{
    Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(LoanApplication application);
}
