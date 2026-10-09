# Loan application — .NET + Next.js

> **Video walkthrough:** 
https://www.loom.com/share/491de572ce92414ea66fa9b3f157a939

A small loan flow: a form (Next.js) → a rule engine decides (.NET) → approved applications are saved in
PostgreSQL → a background event sends the data to an external service (a mock).

Design decisions, how to add a rule, and what happens when something fails are in
[ARCHITECTURE.md](ARCHITECTURE.md).

## Requirements

- .NET 10 SDK
- Node.js 20+ (developed on 22), for the frontend
- Docker (PostgreSQL, and the integration tests)

## Run everything

Four things run: PostgreSQL, the loan API, the mock external service (also a .NET API, in the same solution), and the frontend.

```bash
# 1. PostgreSQL on localhost:5433
docker compose up -d --wait

# 2. Mock external service on http://localhost:4000   (terminal 1)
dotnet run --project backend/mock/Lending.MockExternalService

# 3. Loan API on http://localhost:5080                (terminal 2; creates the schema on startup in Development)
dotnet run --project backend/src/Lending.Api

# 4. Frontend on http://localhost:3000                (terminal 3)
cd frontend
npm install
npm run dev
```

**In Visual Studio** the two .NET projects start together with F5:

1. Open `backend/Lending.slnx`.
2. In the startup dropdown in the toolbar, pick the profile **API + Mock external service**
   (defined in `backend/Lending.slnLaunch`). It starts `Lending.MockExternalService` and `Lending.Api` at once.
3. If the profile does not appear: right-click the solution → **Configure Startup Projects…** → **Multiple startup projects**,
   set both projects to **Start**, tick **Share profile** and save. Visual Studio then writes the launch file itself.

You still start PostgreSQL (`docker compose up -d --wait`) and the frontend (`npm run dev`) yourself.

Ports: PostgreSQL is published on **5433** (not 5432) so it does not clash with a local installation.
The API address the frontend calls can be changed with `NEXT_PUBLIC_API_URL` (see `frontend/.env.example`).

## Run the tests

```bash
dotnet test backend/Lending.slnx        # needs Docker running: it starts a real PostgreSQL container
cd frontend && npm run lint && npm run build
```

The backend tests cover the rule engine, the new/returning-customer paths, transaction rollback, the outbox
worker, and the HTTP endpoint. They use a real PostgreSQL (Testcontainers) because the EF Core in-memory provider
has no transactions.

## Test data

| I want... | Type this |
|---|---|
| **Approved** (new customer) | Any state except NY, SSN `123-45-6789`, any valid ZIP (e.g. `73301`) and amount |
| **Denied by state** | State **NY**, any SSN that is not blacklisted |
| **Denied by blacklisted SSN** | SSN `111-11-1111` or `666-66-6666`, any state except NY |
| **Returning customer** | Submit twice with the same SSN (e.g. `123-45-6789`) changing other data, such as the amount |

The blacklist lives in `backend/src/Lending.Api/appsettings.json` under `Rules:BlacklistedSsns`.

### See the result

- **External service:** open http://localhost:4000. It lists every customer it received and how many calls each had.
  After a returning-customer submission you still see **one** customer, with `Calls` = 2 (a `POST` and then a `PUT`).
- **Database:**
  ```bash
  docker compose exec postgres psql -U lending -d lending -c 'select "FirstName","State","CompanyName" from customers;'
  docker compose exec postgres psql -U lending -d lending -c 'select "RequestedAmount" from loan_applications;'
  docker compose exec postgres psql -U lending -d lending -c 'select "Attempts","ProcessedAt","LastError" from outbox_messages order by "CreatedAt";'
  ```
- **Retries:** make the external service fail half the time and submit a few applications; the worker keeps
  retrying with backoff (visible in the API log and in `outbox_messages.Attempts`). Restart the mock with:
  ```bash
  dotnet run --project backend/mock/Lending.MockExternalService -- --FailureRate=0.5
  ```
  (or set `FailureRate` in `backend/mock/Lending.MockExternalService/appsettings.json`).

## Suggested video script

1. Approved application (TX, `123-45-6789`) → approved page → external service page shows the customer.
2. Denial by state (NY) → denied page.
3. Denial by blacklisted SSN (`111-11-1111`) → denied page.
4. Returning customer: same SSN, new amount and name → approved page says it was updated; the database still has 1 customer
   and 1 application with the new values; the external service shows 1 customer with 2 calls.

## Repository layout

```
backend/              .NET 10 solution: Domain, Application, Infrastructure, Api, Tests,
                      and mock/Lending.MockExternalService (the external service, a minimal API)
frontend/             Next.js (App Router) + Tailwind CSS
docker-compose.yml    PostgreSQL
docs/                 Analysis and plan written before building
```
