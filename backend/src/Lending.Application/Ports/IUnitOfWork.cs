namespace Lending.Application;

public interface IUnitOfWork
{   
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}
