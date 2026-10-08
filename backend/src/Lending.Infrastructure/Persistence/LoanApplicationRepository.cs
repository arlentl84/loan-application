using Lending.Application;
using Lending.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure.Persistence;

internal sealed class LoanApplicationRepository(AppDbContext db) : ILoanApplicationRepository
{
    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        db.LoanApplications.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);

    public void Add(LoanApplication application) => db.LoanApplications.Add(application);
}
