namespace Lending.Application;

public interface IExternalCustomerGateway
{   
    Task SendAsync(ApplicationApproved applicationApproved, CancellationToken cancellationToken);
}
