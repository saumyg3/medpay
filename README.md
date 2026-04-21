# MedPay

A healthcare claim processing API demonstrating real insurance adjudication logic — copay, deductible, coinsurance, and out-of-pocket maximum handling — built with C# and .NET 10.

**Live demo:** https://medpay-api.onrender.com

## Why this project exists

Healthcare payment processing involves nuanced business rules that most tutorial projects skip. A real claim is not just "charge the card" — it flows through validation, policy lookup, adjudication math that splits costs between the payer and the patient, and ledger posting. This project models that flow end-to-end.

## Architecture

```
MedPay.Core            Pure domain entities and service interfaces (no framework dependencies)
MedPay.Infrastructure  EF Core, PostgreSQL, seed data, service implementations
MedPay.Api             ASP.NET Core Web API, controllers, DTOs, Swagger
MedPay.Tests           xUnit tests for validation and adjudication logic
```

The solution follows layered architecture principles: Core has no dependencies, Infrastructure depends on Core, and Api depends on both. This keeps business logic testable and makes the data layer replaceable.

## What it does

### Claim validation

Before a claim is accepted, the ClaimValidationService checks:

- Patient exists in the system
- Provider exists in the system
- Patient has an active policy on the service date (not just today)
- Claim has at least one line item
- CPT codes follow the 5-digit format
- Line item charges are positive
- No duplicate claim (same patient, provider, service date, and CPT codes)

### Claim adjudication

The AdjudicationService applies real insurance math in the correct order:

1. **Copay** is deducted from the claim (patient always pays this first)
2. **Deductible** — if not yet met, the remaining deductible is applied to the claim
3. **Coinsurance** — any remainder is split between patient and payer at the policy coinsurance rate
4. **Out-of-pocket maximum** — if the patient would exceed their annual OOP max, their responsibility is capped and the payer absorbs the rest

The service returns an Adjudication entity with the split, a decision (Approve / Deny / PartialApprove), and a denial reason if applicable.

### REST API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/patients | List all patients |
| GET | /api/patients/{id} | Get patient by ID |
| GET | /api/providers | List all providers |
| GET | /api/payers | List all payers |
| GET | /api/claims | List all claims |
| GET | /api/claims/{id} | Get claim by ID |
| POST | /api/claims | Submit a new claim (runs validation) |
| POST | /api/claims/{id}/adjudicate | Run adjudication on a submitted claim |

Full OpenAPI / Swagger docs at /swagger on any running instance.

## Running locally

### With Docker (easiest)

```bash
docker compose up --build
```

API available at http://localhost:5110/swagger.

### Without Docker

Requires .NET 10 SDK and PostgreSQL running locally.

```bash
createdb medpay
dotnet ef database update --project MedPay.Infrastructure --startup-project MedPay.Api
dotnet run --project MedPay.Api
```

The app auto-seeds 4 patients, 2 payers, 3 providers, and 4 policies on first run.

## Testing

```bash
dotnet test
```

The test suite includes:

- 9 validation tests covering every rule and their combinations
- 8 adjudication tests covering copay-only, deductible-applied, fully-met-deductible, OOP-max-capped, zero-coinsurance, no-active-policy, partial-deductible-met, and multi-line claims

All tests use an in-memory EF Core provider for isolation.

## Tech stack

- **Language**: C# 12 on .NET 10
- **Web framework**: ASP.NET Core
- **ORM**: Entity Framework Core with PostgreSQL provider
- **Database**: PostgreSQL 16
- **Testing**: xUnit, FluentAssertions, EF Core InMemory
- **API docs**: Swashbuckle (Swagger / OpenAPI)
- **Containerization**: Docker (multi-stage build)
- **CI**: GitHub Actions (builds, runs tests, verifies Docker image on every push)
- **Deployment**: Render

## Design decisions worth noting

- **Decimal precision**: Financial fields use HasPrecision(10, 2) on the column to prevent rounding errors that would be unacceptable in a payment system.
- **Adjudication is pure**: The AdjudicationService does not mutate policy state — callers persist the adjudication and update policy trackers explicitly. This keeps the service easier to test and prevents EF Core concurrency conflicts.
- **Policy date-scoping**: Claims are matched against the policy active on the service date, not the current policy. Patients change insurance mid-year, and claims can be submitted retroactively.
- **Connection string flexibility**: The app accepts both Npgsql-format connection strings (local dev) and URL-format DATABASE_URL environment variables (cloud deployments like Render and Heroku).
