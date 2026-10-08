# Architecture

## Structure

Dependencies point inward: `Api → Infrastructure → Application → Domain`. Domain references nothing.

| Project | Responsibility |
|---|---|
| `Lending.Domain` | Business rules and invariants. `Customer`, `LoanApplication`, the `Ssn` value object (validates, normalizes, masks when printed), `CustomerDetails`, and the rule engine with its rules. |
| `Lending.Application` | The single use case, `SubmitApplicationHandler`, plus the ports it needs: `ICustomerRepository`, `ILoanApplicationRepository`, `IUnitOfWork`, `IOutbox`, `IExternalCustomerGateway`. |
| `Lending.Infrastructure` | Adapters: EF Core + PostgreSQL, the outbox table and its background worker, the HTTP client for the external service, the blacklist read from configuration. |
| `Lending.Api` | Thin controller (one endpoint), request/response contracts, DI wiring. No business logic. |
| `Lending.MockExternalService` | The external system (in `backend/mock`): a standalone minimal API with an in-memory store. It references none of the other projects; it only shares the HTTP contract below. |
| `Lending.Tests` | Rule engine (pure unit tests); handler, outbox worker and endpoint against a real PostgreSQL. |

`frontend/` is a Next.js app with three pages (form, approved, denied).

In Visual Studio, `backend/Lending.slnLaunch` defines a launch profile that starts the API and the mock together.

**Endpoint:** `POST /api/applications` → `200 { status: "Approved" | "Denied", isReturningCustomer, reasons[] ... }`.
A denial is a business outcome, so it is a 200 with `status: "Denied"`; malformed input is a 400.

## Rule engine

`RuleEngine` receives every `IDenyRule` and runs all of them; the application is approved when none returns a reason.
Reasons accumulate, so a NY applicant with a blacklisted SSN gets both.

- `StateNotServedRule`: denies NY.
- `BlacklistedSsnRule`: denies SSNs in `ISsnBlacklist` (implemented in Infrastructure from `Rules:BlacklistedSsns`).
  Its message is deliberately vague so the applicant does not learn why.

**Adding a rule:** create a class implementing `IDenyRule` in `Lending.Domain/Rules`. That is all: rules are discovered
by scanning the assembly (`AddApplication`), and no existing rule or the handler changes. A test
(`A_new_rule_is_added_without_touching_existing_ones`) shows it.

## Transaction and returning customer

The handler runs inside `IUnitOfWork.ExecuteAsync`, which opens one PostgreSQL transaction:

1. Look the customer up by SSN.
2. **New:** insert `Customer` and `LoanApplication`. **Returning:** update the `Customer` and its single `LoanApplication`.
3. Add an `OutboxMessage` (the event) to the same context.
4. `SaveChanges` + commit.

Customer, application and event are therefore one atomic unit. "One SSN = one customer = one application" is
enforced by the database as well: a unique index on `customers.Ssn` and on `loan_applications.CustomerId`.
If two requests race on a new SSN, the loser hits the unique index, rolls back and retries once, now finding the customer.

| What fails | Result |
|---|---|
| A rule denies | Nothing is written. |
| Database error, or the event cannot be registered | Whole transaction rolled back: no customer, no application, no event. The API answers 500. Covered by tests for both the new and the returning path. |
| Process crashes after commit | The event is already in the table; the worker delivers it after restart. |
| External service is down or answers an error | The data stays saved and the event stays pending. See below. |

## Background event → external service

The event is `ApplicationApproved`: a self-contained snapshot (`isReturningCustomer`, customer, application).

**Why an outbox table and not an in-memory queue:** an in-memory queue cannot take part in the database transaction, so it
could publish an event for data that then fails to commit, or lose events on a crash. Writing the event as a row in the same
transaction gives exactly the guarantee requested: committed ⇒ event exists, rolled back ⇒ no event.

`OutboxProcessor` (a `BackgroundService`) polls every 2 s, outside the HTTP request, and calls `IExternalCustomerGateway`:

| Case | Call |
|---|---|
| New customer | `POST /customers` with `{ customer, application }` |
| Returning customer | `PUT /customers/{customerId}` with the same body |

`customerId` (a GUID) is the key in the external service, so the SSN is not used as an identifier.

- **Success** → `ProcessedAt` is set.
- **Failure** (any exception, non-2xx, timeout) → `Attempts++`, `LastError` stored, next try after an exponential backoff
  (5 s, 10 s, 20 s, 40 s between tries). After 5 failed attempts the row stays unprocessed for manual review:
  `select * from outbox_messages where "ProcessedAt" is null and "Attempts" >= 5`.
- **Delivery is at-least-once**, so the external service must be idempotent. The mock is: a repeated `POST` with a known id
  answers 200 and a `PUT` replaces the record. A `PUT` for an unknown id answers 404 (an update never creates), which
  the worker simply retries until the original `POST` has been delivered.

Retries live in the API (outbox), not in the HTTP client, because they must survive restarts.

## Trade-offs: what was left out and why

- **No EF migrations.** In Development the schema is created with `EnsureCreated`. Fine for a take-home; for production, add
  `dotnet ef migrations add Initial` and call `Migrate()` instead.
- **SSN is stored and sent in clear text** (it is also in the outbox payload). Production would encrypt or tokenize it and
  decide whether the external service needs it at all. It is never logged: `Ssn.ToString()` is masked.
- **One worker instance.** Running several would need claiming rows with `FOR UPDATE SKIP LOCKED`. Not needed here.
- **Ordering** is by creation time but not strictly guaranteed across retries; the 404-then-retry behavior above covers the
  only case where it matters.
- **Rules are synchronous.** The blacklist comes from configuration. A database-backed blacklist would make the port async.
- **No MediatR, CQRS, generic repository, AutoMapper or domain-event dispatcher.** One use case does not earn them.
- **The mock has no tests of its own.** It is test tooling; the outbox worker and its retry behavior are tested against a stub gateway.
- **No authentication, CI, or structured logging** (not requested). The frontend uses plain `useState` and its own small
  validator instead of a form library, and has no browser (e2e) tests.
- **Floating package versions** (`10.0.*`) in the `.csproj` files, to follow the latest .NET 10 patch releases.
