using Lending.Domain;

namespace Lending.Application;

public interface ICustomerRepository
{
    Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken);

    void Add(Customer customer);
}
