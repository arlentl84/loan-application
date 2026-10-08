using Lending.Application;
using Lending.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure.Persistence;

internal sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken) =>
        db.Customers.FirstOrDefaultAsync(c => c.Ssn == ssn, cancellationToken);

    public void Add(Customer customer) => db.Customers.Add(customer);
}
