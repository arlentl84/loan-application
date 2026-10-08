using System.Globalization;
using System.Net;
using System.Text;

namespace Lending.MockExternalService;

/// <summary>A small auto-refreshing page that shows what the mock received.</summary>
public static class DashboardPage
{
    public static string Render(ReceivedCustomers store)
    {
        var (customers, calls) = store.Snapshot();
        var html = new StringBuilder();

        html.Append("""
            <!doctype html>
            <meta charset="utf-8">
            <meta http-equiv="refresh" content="3">
            <title>Mock external service</title>
            <style>
              body { font: 14px system-ui, sans-serif; margin: 2rem; color: #222; }
              table { border-collapse: collapse; }
              td, th { border: 1px solid #ccc; padding: .4rem .7rem; text-align: left; }
              th { background: #f3f3f3; }
              small { color: #666; }
            </style>
            <h1>Mock external service</h1>
            """);

        html.Append(CultureInfo.InvariantCulture, $"<p>{customers.Count} customer(s) received. Refreshes every 3 seconds.</p>");
        html.Append("<table><tr><th>Customer</th><th>Company</th><th>State</th><th>Amount</th><th>Calls</th><th>Last update (UTC)</th></tr>");

        foreach (var received in customers)
        {
            var customer = received.Customer;
            html.Append(CultureInfo.InvariantCulture, $"""
                <tr>
                  <td>{Encode(customer.FirstName)} {Encode(customer.LastName)}</td>
                  <td>{Encode(customer.CompanyName)}</td>
                  <td>{Encode(customer.State)}</td>
                  <td>${received.Application.RequestedAmount:N0}</td>
                  <td>{received.Calls}</td>
                  <td>{received.UpdatedAt:yyyy-MM-dd HH:mm:ss}</td>
                </tr>
                """);
        }

        html.Append("</table><h2>Recent calls</h2><ul>");
        foreach (var call in calls)
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<li><code>{Encode(call.Method)} {Encode(call.Path)}</code> → {call.Status} <small>{call.At:HH:mm:ss}</small></li>");
        }
        html.Append("</ul>");

        return html.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
