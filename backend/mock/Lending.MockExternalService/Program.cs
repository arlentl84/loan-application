using Lending.MockExternalService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ReceivedCustomers>();

var app = builder.Build();

var failureRate = app.Configuration.GetValue<double>("FailureRate");
const string InvalidBody = "Expected { customer: { id, ... }, application: { ... } }.";

app.Use(async (context, next) =>
{
    var isWrite = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method);

    if (isWrite && Random.Shared.NextDouble() < failureRate)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { error = "Simulated failure" });
    }
    else
    {
        await next();
    }

    if (isWrite)
    {
        context.RequestServices.GetRequiredService<ReceivedCustomers>()
            .RecordCall(context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
});

app.MapPost("/customers", (ExternalCustomerRequest request, ReceivedCustomers store) =>
{
    if (request.Customer is null || request.Application is null || request.Customer.Id == Guid.Empty)
    {
        return Results.BadRequest(new { error = InvalidBody });
    }

    var id = request.Customer.Id;
    return store.Upsert(id, request)
        ? Results.Created($"/customers/{id}", new { id })
        : Results.Ok(new { id });
});

app.MapPut("/customers/{id:guid}", (Guid id, ExternalCustomerRequest request, ReceivedCustomers store) =>
{
    if (request.Customer is null || request.Application is null)
    {
        return Results.BadRequest(new { error = InvalidBody });
    }

    if (request.Customer.Id != id)
    {
        return Results.BadRequest(new { error = "The customer id in the body must match the id in the URL." });
    }
    
    return store.Upsert(id, request)
        ? Results.Created($"/customers/{id}", new { id })
        : Results.Ok(new { id });
});

app.MapGet("/customers", (ReceivedCustomers store) =>
{
    var (customers, calls) = store.Snapshot();
    return Results.Ok(new { count = customers.Count, customers, recentCalls = calls });
});

app.MapGet("/", (ReceivedCustomers store) =>
    Results.Content(DashboardPage.Render(store), "text/html; charset=utf-8"));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
