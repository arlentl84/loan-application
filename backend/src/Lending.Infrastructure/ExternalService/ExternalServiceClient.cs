using System.Net.Http.Json;
using Lending.Application;

namespace Lending.Infrastructure.ExternalService;

internal sealed class ExternalServiceClient(HttpClient http) : IExternalCustomerGateway
{
    public async Task SendAsync(ApplicationApproved applicationApproved, CancellationToken cancellationToken)
    {
        var body = new { customer = applicationApproved.Customer, application = applicationApproved.Application };

        using var response = applicationApproved.IsReturningCustomer
            ? await http.PutAsJsonAsync($"customers/{applicationApproved.Customer.Id}", body, cancellationToken)
            : await http.PostAsJsonAsync("customers", body, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
