namespace Lending.MockExternalService;

public sealed record ReceivedCustomer(
    CustomerPayload Customer,
    ApplicationPayload Application,
    DateTimeOffset UpdatedAt,
    int Calls);

public sealed record CallLogEntry(DateTimeOffset At, string Method, string Path, int Status);

/// <summary>In-memory "database" of the mock: what the external service has received so far.</summary>
public sealed class ReceivedCustomers
{
    private const int MaxLoggedCalls = 50;

    private readonly object _gate = new();
    private readonly Dictionary<Guid, ReceivedCustomer> _customers = [];
    private readonly List<CallLogEntry> _calls = [];
    
    public bool Upsert(Guid id, ExternalCustomerRequest request)
    {
        lock (_gate)
        {
            var isNew = !_customers.TryGetValue(id, out var existing);
            _customers[id] = Store(request, existing);
            return isNew;
        }
    }
    
    public bool Create(ExternalCustomerRequest request)
    {
        lock (_gate)
        {
            var id = request.Customer!.Id;
            var isNew = !_customers.TryGetValue(id, out var existing);
            _customers[id] = Store(request, existing);
            return isNew;
        }
    }
    
    public bool Update(Guid id, ExternalCustomerRequest request)
    {
        lock (_gate)
        {
            if (!_customers.TryGetValue(id, out var existing))
            {
                return false;
            }

            _customers[id] = Store(request, existing);
            return true;
        }
    }

    public void RecordCall(string method, string path, int status)
    {
        lock (_gate)
        {
            _calls.Insert(0, new CallLogEntry(DateTimeOffset.UtcNow, method, path, status));
            if (_calls.Count > MaxLoggedCalls)
            {
                _calls.RemoveAt(_calls.Count - 1);
            }
        }
    }

    public (IReadOnlyList<ReceivedCustomer> Customers, IReadOnlyList<CallLogEntry> Calls) Snapshot()
    {
        lock (_gate)
        {
            return (_customers.Values.OrderByDescending(c => c.UpdatedAt).ToList(), _calls.ToList());
        }
    }

    private static ReceivedCustomer Store(ExternalCustomerRequest request, ReceivedCustomer? existing) =>
        new(request.Customer!, request.Application!, DateTimeOffset.UtcNow, (existing?.Calls ?? 0) + 1);
}
