# Payment Processing & Compliance API

A simplified payment processing backend for a fintech platform, built with **ASP.NET Core (.NET 10)**, **Clean Architecture** and **Domain-Driven Design**.

Users submit payment requests, background workers process them asynchronously against an external (simulated) payment provider, failed payments are retried automatically, and every status change is audited. Access is controlled with JWT and permission-based policies. The API is exercised entirely through **Swagger UI**.

---

## Table of contents

1. [Tech stack](#tech-stack)
2. [How to run](#how-to-run)
3. [Swagger and test credentials](#swagger-and-test-credentials)
4. [Architecture](#architecture)
5. [Domain model (DDD)](#domain-model-ddd)
6. [Key design decisions](#key-design-decisions)
7. [API endpoints](#api-endpoints)
8. [Background processing](#background-processing)
9. [Database, indexes and reporting](#database-indexes-and-reporting)
10. [Testing the solution in Swagger](#testing-the-solution-in-swagger)
11. [Known limitations and possible improvements](#known-limitations-and-possible-improvements)

---

## Tech stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Web API (controllers) |
| Persistence | EF Core 10 (Code First + migrations), Microsoft SQL Server |
| Reporting | SQL Server stored procedure (added via migration) + MySQL equivalent |
| Auth | JWT bearer tokens, permission-based authorization policies |
| HTTP integration | Typed `HttpClient` + `Microsoft.Extensions.Http.Resilience` (retry with exponential backoff and jitter, timeouts, circuit breaker) |
| Background work | Built-in `BackgroundService`, `System.Threading.Channels`, `PeriodicTimer` |
| API docs | OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Swagger UI |
| Errors | RFC 9457 `ProblemDetails` via a global `IExceptionHandler` |

---

## How to run

### Prerequisites

- .NET 10 SDK
- SQL Server (Developer / Express / LocalDB)
- Optional: `dotnet-ef` tool for running migrations manually: `dotnet tool install --global dotnet-ef --version 10.*`

### 1. Configure the database connection

The connection string is in `Payments.Api/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "PaymentsDb": "Server=localhost;Database=KamaPaymentsDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

For LocalDB use:

```
Server=(localdb)\\MSSQLLocalDB;Database=KamaPaymentsDb;Trusted_Connection=True;TrustServerCertificate=True
```

### 2. Database setup and migrations

In the **Development** environment the API **applies migrations automatically on startup** and seeds the test users, so no manual step is required.

To apply migrations manually instead:

```bash
dotnet ef database update --project Payments.Infrastructure --startup-project Payments.Api
```

This creates the tables, indexes, the seeded payment provider and the `dbo.sp_DailyPaymentReport` stored procedure.

### 3. Start both services

The fake payment provider must run alongside the API.

**Option A: CLI (two terminals)**

```bash
dotnet run --project FakeProvider.Api --launch-profile http
```

```bash
dotnet run --project Payments.Api --launch-profile https
```

**Option B: Visual Studio**

Right-click the solution → **Configure Startup Projects** → **Multiple startup projects** → set `FakeProvider.Api` and `Payments.Api` to **Start** (FakeProvider first) → **F5**.

| Service | URL |
| --- | --- |
| Payments API | `https://localhost:7202` |
| Fake payment provider | `http://localhost:5100` (no UI, called only by the API) |

---

## Swagger and test credentials

**Swagger UI:** `https://localhost:7202/swagger`
**OpenAPI document:** `https://localhost:7202/openapi/v1.json`

1. Call `POST /api/v1/auth/login` with one of the accounts below.
2. Copy `accessToken` from the response.
3. Click **Authorize** in Swagger and paste the token.

| Email | Password | Role | Permissions |
| --- | --- | --- | --- |
| `admin@kama.test` | `Admin@123` | Admin | Payments.Create, Payments.View, Payments.Process, Providers.Manage |
| `user@kama.test` | `User@123` | User | Payments.Create, Payments.View |
| `user2@kama.test` | `User@123` | User | Payments.Create, Payments.View (second user, to test data isolation) |

**Seeded payment provider:** `Fake Pay`, code `FAKEPAY`, id `11111111-1111-1111-1111-111111111111`.

---

## Architecture

The solution follows **Clean Architecture**: dependencies point inward and the Domain has no dependencies at all.

```
Payments.Api ──────────► Payments.Application ──────────► Payments.Domain
     │                          ▲
     └──► Payments.Infrastructure ┘   (implements Application/Domain interfaces)

FakeProvider.Api   (standalone, simulates the external payment provider)
```

| Project | Responsibility |
| --- | --- |
| **Payments.Domain** | Aggregates, value objects, domain events, business rules, repository interfaces. No NuGet packages, no EF Core. |
| **Payments.Application** | Use cases (one handler per use case), DTOs, application exceptions, ports (`IUnitOfWork`, `IPaymentQueue`, `IPaymentProviderClient`, `ICurrentUser`, read-side query interfaces). Contains no business rules. |
| **Payments.Infrastructure** | EF Core `DbContext`, entity configurations, migrations, repositories, read queries, stored procedure call, typed HttpClient, background workers, JWT/identity. |
| **Payments.Api** | Controllers, authentication and permission policies, global exception handling, OpenAPI/Swagger, composition root. |
| **FakeProvider.Api** | Minimal API simulating the external payment provider (latency, transient errors, timeouts, declines, idempotency). |

### Request flow

```
HTTP request
  → Controller (permission policy check)
  → Application handler: load aggregate via repository → call domain method → save via unit of work
  → Domain enforces business rules and records an audit entry
  → Infrastructure persists with EF Core
  → Controller returns the DTO / ProblemDetails
```

---

## Domain model (DDD)

| DDD concept | Implementation |
| --- | --- |
| Aggregate root | `Payment`: all state changes go through its methods (`Create`, `StartProcessing`, `MarkCompleted`, `MarkFailed`, `RequeueAfterInterruption`, `RetryManually`) |
| Second aggregate | `PaymentProvider`: `Create`, `Rename`, `Activate`, `Deactivate` |
| Entity inside an aggregate | `PaymentStatusChange`: the audit trail, created only by `Payment` (internal constructor) |
| Value object | `Money`: immutable record, amount > 0 with max 2 decimals, supported ISO currency |
| Encapsulation | Private setters, private constructors with factory methods, read-only collections |
| References between aggregates | `Payment` holds `ProviderId`, not a `PaymentProvider` object |
| Domain events | `PaymentCreated`, `PaymentCompleted`, `PaymentFailed`, raised by the aggregate and published after a successful save |
| Repositories | One per aggregate root (`IPaymentRepository`, `IPaymentProviderRepository`); interfaces in Domain, implementations in Infrastructure |
| Persistence ignorance | All mapping is in `Infrastructure/Persistence/Configurations` (`Money` mapped with `ComplexProperty`) |

**Payment status lifecycle**

```
Pending ──► Processing ──► Completed
               │
               └──► Failed ──► (automatic retry, max 3 attempts) ──► Processing
                       └──► (admin manual retry, resets attempts) ──► Pending
```

Business rules enforced inside `Payment`:

- A payment always starts as **Pending**.
- Only **Pending** or retryable **Failed** payments can start processing.
- Only a **Processing** payment can be completed or failed.
- Automatic processing is limited to **3 attempts**.
- Only **Failed** payments can be retried manually; a manual retry resets the attempt counter.
- Every transition is recorded in the audit trail (from, to, who, when, note).

Login/users are intentionally **outside the domain** (`Infrastructure/Identity`) because identity is not part of the payments business.

---

## Key design decisions

1. **No MediatR.** Each use case is a plain handler class registered in DI. It keeps the flow explicit and avoids a commercially licensed dependency for a project of this size.
2. **The database is the source of truth for the queue.** A `Channel<Guid>` is used as a fast in-memory signal, but a payment with `Status = Pending` *is* the queued item. Nothing is lost if the process restarts.
3. **Enqueue after commit.** A payment is only signalled to the worker after it has been saved, so the worker never sees an uncommitted payment.
4. **Idempotency at three levels.**
   - API: `Idempotency-Key` header + unique index on `(UserId, IdempotencyKey)`. Same key and same body returns the original payment (200); same key with a different body returns 409. A request hash detects the difference.
   - Processing: optimistic concurrency (`RowVersion`) so only one worker can move a payment to Processing; completed or in-flight payments are skipped.
   - Provider: the payment ID is sent as the provider-side `Idempotency-Key`, so a repeated call never charges twice. This is also what makes retrying a POST safe.
5. **Resilient HTTP integration.** Typed `HttpClient` via `IHttpClientFactory` with the standard resilience handler: exponential backoff with jitter, per-attempt timeout, total timeout and circuit breaker. Values are configurable under `PaymentProvider` in `appsettings.json`.
6. **CQRS-style read side.** Writes go through aggregates and repositories; reads use dedicated query services with `AsNoTracking` and projections into DTOs.
7. **404 instead of 403 for other users' payments.** A user requesting another user's payment receives 404, so the API does not even reveal that the payment exists.
8. **Permission-based authorization.** Roles map to permissions in one place (`RolePermissions`). Tokens carry `permission` claims and every endpoint is protected by a policy per permission.
9. **Errors as ProblemDetails.** A global `IExceptionHandler` maps domain and application exceptions to 400/404/409, includes a `traceId`, and never leaks internal details on 500.
10. **Money as `decimal(18,2)` + currency**, never float/double. Reports group by currency so different currencies are never summed together.
11. **UTC everywhere.** All timestamps are generated in UTC and an EF value converter marks them as UTC when read, so the API always returns ISO 8601 dates with `Z`.
12. **Configuration split.** Shared defaults are in `appsettings.json`; environment-specific values (connection string, provider URL, JWT signing key) are in `appsettings.Development.json`. In production these would come from environment variables or Azure Key Vault.
13. **Fake provider as a separate minimal API.** It is a test stub for an external system, so it is deliberately kept tiny, while the main API uses controllers for structure, per-endpoint policies and richer OpenAPI metadata.

---

## API endpoints

| Method | Endpoint | Access | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous | 200 | 401 |
| POST | `/api/v1/payments` (header `Idempotency-Key`) | `Payments.Create` | 201 (new), 200 (replay) | 400, 401, 403, 404, 409 |
| GET | `/api/v1/payments/{id}` | `Payments.View` | 200 | 401, 403, 404 |
| GET | `/api/v1/payments?status=&userId=&page=&pageSize=` | `Payments.View` | 200 | 401, 403 |
| POST | `/api/v1/payments/{id}/retry` | `Payments.Process` (Admin) | 202 | 401, 403, 404, 409 |
| GET | `/api/v1/providers` | Any authenticated user | 200 | 401 |
| POST | `/api/v1/providers` | `Providers.Manage` | 201 | 400, 401, 403, 409 |
| PUT | `/api/v1/providers/{id}` | `Providers.Manage` | 200 | 400, 401, 403, 404 |
| GET | `/api/v1/reports/daily-payments?from=&to=` | Admin role | 200 | 400, 401, 403 |

Notes:

- Users only ever see their own payments. Admins see all payments and can filter by `userId`; the filter is ignored for non-admins.
- Listing providers is open to any authenticated user because a user needs a provider ID to create a payment.
- The report defaults to the last 30 days and is limited to a 366-day range.

### Validation and status codes for `POST /api/v1/payments`

| Case | Status |
| --- | --- |
| Payment created | 201 Created (status `Pending`) |
| Same `Idempotency-Key` and same body | 200 OK (original payment) |
| Same `Idempotency-Key`, different body | 409 Conflict |
| Missing `Idempotency-Key`, amount ≤ 0, more than 2 decimals, unsupported currency, inactive provider | 400 Bad Request |
| Provider does not exist | 404 Not Found |
| No/invalid token | 401 Unauthorized |
| Missing `Payments.Create` permission | 403 Forbidden |

Supported currencies: `USD`, `EUR`, `GBP`, `AED`, `SAR`.

---

## Background processing

```
POST /payments
  → save Payment (Pending) → commit → enqueue(id) → return 201 immediately

PaymentProcessingWorker (BackgroundService)
  on startup : payments stuck in Processing → back to Pending; all Pending → enqueued
  loop       : dequeue id → PaymentProcessor.ProcessAsync(id) in its own DI scope
                 1. skip if Completed / Processing / no attempts left
                 2. StartProcessing() + save   (RowVersion: only one worker wins)
                 3. call Fake Provider via typed HttpClient (retry + backoff + timeouts)
                 4. MarkCompleted / MarkFailed + save (audit entry added)

FailedPaymentRetryWorker (BackgroundService + PeriodicTimer)
  every X minutes : Failed payments with AttemptCount < 3 → enqueued again
```

- Processing never blocks the API request.
- Payments still queued when the application restarts are picked up again.
- Processing is safe to run twice for the same payment (see idempotency above).
- The interval is configurable: `BackgroundJobs:RetryIntervalMinutes` (default 1 minute, to make retries easy to observe).

### Fake provider behaviour

| Amount | Behaviour |
| --- | --- |
| Normal amounts | ~80% approved, ~10% declined, ~20% transient `503` (usually recovered by retries) |
| `503` | Always returns `503`: demonstrates retries with backoff, then `Failed` |
| `408` | Always hangs: demonstrates per-attempt timeouts, then `Failed` |
| `> 10000` | Always declined ("Amount exceeds provider limit") |

The provider stores results per idempotency key, so repeating a call returns the original result instead of charging again.

---

## Database, indexes and reporting

### Tables

| Table | Purpose |
| --- | --- |
| `Payments` | Payment aggregate (amount `decimal(18,2)` + `char(3)` currency, status, attempts, `RowVersion`) |
| `PaymentStatusChanges` | Append-only audit trail of every status transition |
| `PaymentProviders` | Configured providers (seeded with Fake Pay) |
| `Users` | Test users for login (password hashes, role) |

### Indexes

| Index | Columns | Why |
| --- | --- | --- |
| `UX_Payments_UserId_IdempotencyKey` (unique) | `UserId, IdempotencyKey` | Guarantees idempotency even when two identical requests arrive at the same time |
| `IX_Payments_UserId_Status_CreatedAtUtc` | `UserId, Status, CreatedAtUtc` | Main user query: "my payments", optionally filtered by status, newest first |
| `IX_Payments_Status_UpdatedAtUtc` | `Status, UpdatedAtUtc` | Background workers looking up Pending, Failed or Processing payments |
| `IX_Payments_CreatedAtUtc_ProviderId` | `CreatedAtUtc, ProviderId` | Daily report: range filter on date, grouped by provider |
| `UX_PaymentProviders_Code` (unique) | `Code` | Provider codes must be unique |
| `IX_Users_Email` (unique) | `Email` | Login lookup |

### Daily payment report

Daily payment count and total amount per provider, status and currency.

**SQL Server:** stored procedure `dbo.sp_DailyPaymentReport`, created by the migration `AddDailyPaymentReportProcedure` and exposed through the Admin-only endpoint `GET /api/v1/reports/daily-payments`. The date filter is written as a range (`>= @FromDate AND < @ToDate + 1 day`) so it is SARGable and can use the `CreatedAtUtc` index.

```sql
EXEC dbo.sp_DailyPaymentReport @FromDate = '2026-11-01', @ToDate = '2026-11-30';
```

**MySQL equivalent:** `sql/mysql_daily_payment_report.sql`

```sql
DROP PROCEDURE IF EXISTS sp_DailyPaymentReport;

DELIMITER $$

CREATE PROCEDURE sp_DailyPaymentReport(IN p_FromDate DATE, IN p_ToDate DATE)
BEGIN
    SELECT
        DATE(p.CreatedAtUtc)   AS `Day`,
        pr.Name                AS ProviderName,
        p.Status               AS `Status`,
        COUNT(*)               AS PaymentCount,
        SUM(p.Amount)          AS TotalAmount,
        p.Currency             AS Currency
    FROM Payments p
    INNER JOIN PaymentProviders pr ON pr.Id = p.ProviderId
    WHERE p.CreatedAtUtc >= p_FromDate
      AND p.CreatedAtUtc <  DATE_ADD(p_ToDate, INTERVAL 1 DAY)
    GROUP BY DATE(p.CreatedAtUtc), pr.Name, p.Status, p.Currency
    ORDER BY `Day`, ProviderName, `Status`;
END$$

DELIMITER ;

CALL sp_DailyPaymentReport('2026-11-01', '2026-11-30');
```

Differences from the SQL Server version: `DATE()` instead of `CAST(... AS DATE)`, `DATE_ADD(... INTERVAL 1 DAY)` instead of `DATEADD`, backticks instead of brackets, `DELIMITER`, `CALL` instead of `EXEC`, and drop/create because MySQL has no `CREATE OR ALTER PROCEDURE`.

---

## Testing the solution in Swagger

The provider ID used below is `11111111-1111-1111-1111-111111111111`.

**As `user@kama.test`**

1. `POST /api/v1/payments` with `Idempotency-Key: pay-001` and `{ "providerId": "1111…", "amount": 100, "currency": "USD" }` → **201**, `Pending`.
2. `GET /api/v1/payments/{id}` a few seconds later → `Completed` (or `Failed` if randomly declined) with the full status history.
3. Repeat step 1 with the same key and body → **200**, same payment ID. Change the amount with the same key → **409**.
4. Amount `0`, currency `XYZ` or missing key → **400**. Unknown provider ID → **404**.
5. Amount `503` → `Failed` after retries; wait 1–3 minutes → `attemptCount` reaches 3 and it stays `Failed`.
6. `POST /api/v1/providers`, `POST /api/v1/payments/{id}/retry` or `GET /api/v1/reports/daily-payments` → **403**.

**As `user2@kama.test`**

7. `GET /api/v1/payments/{user1 payment id}` → **404**. `GET /api/v1/payments` → empty list, even with `?userId=` set to user1's ID.

**As `admin@kama.test`**

8. `GET /api/v1/payments` → all users' payments.
9. `POST /api/v1/payments/{failed id}/retry` → **202**; history shows "Manual retry by admin". Retrying a completed payment → **409**.
10. `GET /api/v1/reports/daily-payments` → report rows from the stored procedure.
11. Create a provider → **201**; same code again → **409**. Deactivate Fake Pay, then create a payment as a user → **400** ("Payment provider is not active").

**Restart recovery**

12. Set a payment back to `Pending` in the database, restart the API → the log shows "Startup recovery: 1 pending payments queued" and the payment is processed again; the fake provider logs an idempotent replay and returns the original reference.

**Audit trail**

```sql
SELECT * FROM PaymentStatusChanges ORDER BY ChangedAtUtc;
```

---

## Known limitations and possible improvements

- **Single instance assumed for recovery.** On startup, payments in `Processing` are re-queued. With multiple API instances this should use a processing lease/timeout (or a distributed lock) instead.
- **In-memory channel.** Fine for a single instance because the database remains the source of truth; at scale a message broker (Azure Service Bus, RabbitMQ, Kafka) would replace it.
- **Domain events are only logged.** In production they would be published through a transactional outbox to a message broker.
- **Users are seeded, not managed.** There is no registration, refresh tokens or user management, as it is outside the scope of the assessment.
- **Fake provider state is in memory.** Restarting it clears its idempotency store.
- **Time in the domain.** The domain uses `DateTime.UtcNow`; injecting `TimeProvider` would make time-based rules easier to unit test.
- **Tests.** Unit tests for the `Payment` aggregate and integration tests (e.g. `WebApplicationFactory` + Testcontainers) would be the next step.
- **Secrets.** The development JWT signing key is in `appsettings.Development.json` for convenience only; production would use User Secrets, environment variables or Azure Key Vault.
