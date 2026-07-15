# BillFlow Backend

The BillFlow backend is a **.NET 9** solution that powers identity, administration, and the full billing domain. It is designed as a small set of focused services that share PostgreSQL and Redis, with clear boundaries between authentication and business logic.

For monorepo setup (Docker, `.env`, frontend), start with the [root README](../README.md).

---

## Table of contents

1. [Architecture overview](#architecture-overview)
2. [Services](#services)
3. [User roles and authorization](#user-roles-and-authorization)
4. [Billing domain](#billing-domain)
5. [API reference](#api-reference)
6. [How to run](#how-to-run)
7. [Database and migrations](#database-and-migrations)
8. [Background jobs](#background-jobs)
9. [Security](#security)
10. [Testing](#testing)
11. [Production checklist](#production-checklist)
12. [Solution structure](#solution-structure)
13. [Roadmap](#roadmap)

---

## Architecture overview

```
                    ┌─────────────────┐
                    │   Frontend SPA  │
                    └────────┬────────┘
                             │ HTTPS / JWT
              ┌──────────────┴──────────────┐
              ▼                             ▼
    ┌──────────────────┐         ┌──────────────────────┐
    │   Auth Service   │         │  Management Service  │
    │     :5237        │         │       :5177          │
    └────────┬─────────┘         └──────────┬───────────┘
             │                              │
             │         ┌────────────────────┤
             │         │                    │
             ▼         ▼                    ▼
      ┌──────────┐  ┌──────────┐    ┌─────────────────┐
      │  Redis   │  │ Postgres │    │ Background Jobs │
      │  cache   │  │    DB    │    │ (overdue sync)  │
      └──────────┘  └──────────┘    └─────────────────┘
```

**Auth Service** owns user identity: registration, login, token refresh, profile, and password operations.

**Management Service** owns administration (Admin/Visitor CRUD) and the entire **billing** surface: clients, items, invoices, payments, dashboard, PDF export, CSV/XLSX reports, and company settings.

**Background Jobs** is a headless worker that periodically marks overdue invoices across all business owners.

Both HTTP services read configuration from the **repository root** `.env` (see `../.env.example`).

---

## Services

| Service | Project path | Port | Responsibility |
|---------|--------------|------|----------------|
| Auth | `Services/BillFlow.AuthService` | `5237` | JWT auth, Visitor self-registration |
| Management | `Services/BillFlow.ManagementService` | `5177` | Admin APIs + billing APIs |
| Background jobs | `Services/BillFlow.BackgroundJobs` | — | Scheduled overdue invoice sync |

### Shared infrastructure

| Component | Used for |
|-----------|----------|
| **PostgreSQL** | Users, billing entities, refresh tokens |
| **Redis** | Session invalidation, rate limiting, caching |
| **EF Core** | ORM, migrations, transactions |
| **QuestPDF** | Invoice PDF generation |
| **ClosedXML** | Excel report export |

---

## User roles and authorization

| Role | Enum value | How created | API access |
|------|------------|-------------|------------|
| **Visitor** | Business owner | `POST /auth/account/register` | Billing APIs (`Visitor` policy) |
| **Admin** | Platform admin | SuperAdmin via Management API | `/management/*` |
| **SuperAdmin** | Root admin | Seeded from `.env` on startup | Full management access |

JWT claims use `MapInboundClaims = false`; the role claim value for business owners is `Visitor`.

Billing endpoints require a valid Bearer token and the **Visitor** role. Management endpoints require **Admin** or **SuperAdmin** as appropriate.

---

## Billing domain

Billing data is **scoped per business owner** (`OwnerId`). A Visitor only ever sees and modifies their own clients, items, invoices, and settings.

### Modules

| Module | Description |
|--------|-------------|
| **Company settings** | Company profile, default tax rate, payment terms, invoice prefix — used when creating invoices and on PDFs |
| **Clients** | Customer records (company name, contact, email, address, tax number) |
| **Items** | Billable products/services catalog; supports archive |
| **Invoices** | Create, update, send, duplicate, cancel, mark paid; line items; PDF download |
| **Payments** | Record payments against invoices; refund and cancel |
| **Dashboard** | Revenue summary, counts, charts data |
| **Reports** | Export sales, payments, outstanding, and tax reports (CSV/XLSX) |

### Invoice lifecycle

| Status | Meaning |
|--------|---------|
| `Draft` | Editable; PDF download blocked |
| `Sent` | Issued to client |
| `PartiallyPaid` | Some payment recorded |
| `Paid` | Fully paid |
| `Overdue` | Past due date (synced on read + background job) |
| `Cancelled` | Voided |

### Key business rules

- **Mark paid** creates a balancing payment record (not just a status flip).
- **Soft-deleted clients** use a partial unique index on email (`WHERE IsDeleted = false`).
- **Payment writes** use serializable transactions to prevent race conditions.
- **Archived/inactive items** cannot be added to new invoices.
- **Company settings** provide defaults for tax rate, payment terms, and invoice number prefix on create.
- **Rate limiting** applies to billing read and export endpoints.

---

## API reference

Base paths:

- Auth: `http://localhost:5237/api/v1.0/auth/account`
- Management admin: `http://localhost:5177/api/v1.0/management`
- Billing: `http://localhost:5177/api/v1.0/billing`

Swagger is available at `/swagger` on each service.

### Auth — `/api/v1.0/auth/account`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `register` | Public | Create Visitor account |
| POST | `login` | Public | Get access + refresh tokens |
| POST | `refresh-token` | Public | Rotate access token |
| GET | `profile` | Bearer | Current user profile |
| POST | `logout` | Bearer | Invalidate refresh token |
| POST | `change-password` | Bearer | Change password |
| POST | `reset-password` | Public | Dev only (`ALLOW_DEV_RESET_PASSWORD=true`) |
| DELETE | `deactivate` | Bearer | Deactivate account |
| DELETE | `delete` | Bearer | Delete account |

### Management — Admins `/api/v1.0/management/admins`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/` | SuperAdmin | List admins |
| GET | `/{id}` | SuperAdmin | Get admin |
| POST | `/` | SuperAdmin | Create admin |
| PUT | `/{id}` | SuperAdmin | Update admin |
| DELETE | `/{id}` | SuperAdmin | Delete admin |

### Management — Visitors `/api/v1.0/management/visitors`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/` | Admin+ | List business owners |
| GET | `/{id}` | Admin+ | Get visitor |
| PUT | `/{id}` | Admin+ | Update visitor |
| DELETE | `/{id}` | Admin+ | Delete visitor |

### Billing — Clients `/api/v1.0/billing/clients`

| Method | Path | Description |
|--------|------|-------------|
| GET | `?search=` | List/search clients |
| GET | `/{id}` | Get client |
| POST | `/` | Create client |
| PUT | `/{id}` | Update client |
| DELETE | `/{id}` | Soft-delete client |

### Billing — Items `/api/v1.0/billing/items`

| Method | Path | Description |
|--------|------|-------------|
| GET | `?search=&includeArchived=` | List items |
| GET | `/{id}` | Get item |
| POST | `/` | Create item |
| PUT | `/{id}` | Update item |
| POST | `/{id}/archive` | Archive item |
| DELETE | `/{id}` | Delete item |

### Billing — Invoices `/api/v1.0/billing/invoices`

| Method | Path | Description |
|--------|------|-------------|
| GET | `?status=&search=` | List invoices |
| GET | `/{id}` | Invoice detail |
| GET | `/{id}/payments` | Payments for invoice |
| POST | `/` | Create draft invoice |
| PUT | `/{id}` | Update draft invoice |
| POST | `/{id}/duplicate` | Duplicate invoice |
| DELETE | `/{id}` | Delete draft |
| POST | `/{id}/send` | Mark as sent |
| POST | `/{id}/email` | Email invoice PDF |
| POST | `/{id}/mark-paid` | Mark paid + payment |
| POST | `/{id}/cancel` | Cancel invoice |
| GET | `/{id}/pdf` | Download PDF |
| POST | `/{id}/share-link` | Generate customer portal link |
| DELETE | `/{id}/share-link` | Revoke portal link |

### Billing — Payments `/api/v1.0/billing/payments`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | List all payments |
| POST | `/` | Record payment |
| POST | `/{id}/refund` | Refund payment |
| POST | `/{id}/cancel` | Cancel payment |

### Billing — Dashboard `/api/v1.0/billing/dashboard`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Revenue, counts, chart data |

### Billing — Company settings `/api/v1.0/billing/company-settings`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Get settings (404 if not configured) |
| PUT | `/` | Create or update settings |

### Billing — Reports `/api/v1.0/billing/reports`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/sales` | Sales report file |
| GET | `/payments` | Payments report file |
| GET | `/outstanding` | Outstanding invoices file |
| GET | `/taxes` | Tax summary file |

### Billing — Activity `/api/v1.0/billing/activity`

| Method | Path | Description |
|--------|------|-------------|
| GET | `?limit=` | Recent audit events |

### Portal — `/api/v1.0/portal` (public, no auth)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/{token}` | View invoice (JSON) |
| GET | `/{token}/pdf` | Download branded PDF |

---

## How to run

All paths assume you are at the **repository root** unless noted.

### Option A — Full stack in Docker (recommended)

```bash
./scripts/setup-env.sh
docker compose up -d --build
```

With local DB port exposure:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

Verify:

```bash
curl http://127.0.0.1:5237/health
curl http://127.0.0.1:5177/health
```

### Option B — APIs on host, infra in Docker

```bash
./scripts/setup-env.sh
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d postgres redis
```

From `Backend/`:

```bash
dotnet run --project Services/BillFlow.AuthService
dotnet run --project Services/BillFlow.ManagementService   # second terminal
```

Ensure root `.env` has:

```env
DB_HOST=localhost
DB_PORT=5433
REDIS_HOST=localhost
REDIS_PORT=6381
APPLY_MIGRATIONS=true
```

### Rebuild after code changes

```bash
docker compose up -d --build auth-service
docker compose up -d --build management-service
docker compose up -d --build background-jobs
```

### Stop and reset

```bash
docker compose down
docker compose down -v   # also removes database volumes
```

---

## Database and migrations

- **DbContext:** `Backend/DataAccess/BillFlow.Database/DbContexts/BillFlowDbContext.cs`
- **Migrations:** `Backend/DataAccess/BillFlow.Database/Migrations/`
- **Design-time:** `PostgresConnection.ForDesignTime()` allows EF CLI without a live database

### Apply migrations

With `APPLY_MIGRATIONS=true` in `.env`, **ManagementService** applies migrations automatically on startup (AuthService does not migrate — single owner).

### Create a new migration

```bash
cd Backend
dotnet ef migrations add YourMigrationName \
  --project DataAccess/BillFlow.Database \
  --startup-project Services/BillFlow.ManagementService
```

### EF tools prerequisite

`Microsoft.EntityFrameworkCore.Design` is referenced by ManagementService for CLI design-time support.

---

## Background jobs

The **BillFlow.BackgroundJobs** worker runs `OverdueInvoiceSyncHostedService`, which periodically calls `SyncOverdueStatusesForAllOwnersAsync()` on the invoice repository.

| Variable | Default | Description |
|----------|---------|-------------|
| `OVERDUE_SYNC_INTERVAL_MINUTES` | `60` | How often to run the sync |

The worker is included in the root `docker-compose.yml` stack and starts with the other services.

---

## Security

### Authentication

- JWT access tokens (short-lived) + refresh tokens (stored hashed with pepper).
- Refresh token rotation on use; logout invalidates refresh token.
- Redis-backed session invalidation and rate limiting.

### Billing integrity

- **IDOR protection:** every billing query filters by authenticated owner's ID.
- **Concurrency:** serializable transactions for payment operations.
- **Transactional updates:** invoice line items replaced inside a transaction.
- **Input validation:** shared `BillingInputValidator` and request DTO validation.
- **Integration tests:** `BillingSecurityIntegrationTests` covers 401, 403, and cross-owner access.

### Environment secrets (required)

| Variable | Purpose |
|----------|---------|
| `JWT_SECRET` | Signs access tokens (min 32 chars) |
| `REFRESH_TOKEN_PEPPER` | Hardens refresh token hashing |
| `POSTGRES_PASSWORD` | Database password |
| `REDIS_PASSWORD` | Redis AUTH password |
| `SUPERADMIN_PASSWORD` | Bootstrap admin password |

Generate all secrets with `../scripts/setup-env.sh` from the repo root.

---

## Testing

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) — **Docker must be running**.

```bash
cd Backend
dotnet test
```

### Test projects

| Project | Covers |
|---------|--------|
| `BillFlow.AuthService.Tests` | Auth flows, token hashing, helpers |
| `BillFlow.ManagementService.Tests` | Billing CRUD, PDF, reports, dashboard, security, company settings |

Test fixtures disable rate limiting via `DISABLE_RATE_LIMITING=true`.

---

## Production checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`
- [ ] Set `APPLY_MIGRATIONS=false`; run migrations from CI or a one-off job
- [ ] Set `ALLOW_DEV_RESET_PASSWORD=false`
- [ ] Configure `CORS_ALLOWED_ORIGINS` with your frontend URL(s)
- [ ] Use strong secrets from `setup-env.sh` (never commit `.env`)
- [ ] Plan email/SMTP for password reset before public launch
- [ ] Set up backups for Postgres volume `billflow_pgdata`

---

## Solution structure

```
Backend/
├── BillFlow.sln
├── Dockerfile                          # Multi-service build (PROJECT_PATH arg)
├── Services/
│   ├── BillFlow.AuthService/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   └── Startup.cs
│   ├── BillFlow.ManagementService/
│   │   ├── Controllers/                # Admin + billing controllers
│   │   ├── Services/                   # Billing services, PDF generator
│   │   └── Startup.cs
│   └── BillFlow.BackgroundJobs/
│       └── OverdueInvoiceSyncHostedService.cs
├── DataAccess/
│   ├── BillFlow.Models/                # Entities, DTOs, enums
│   ├── BillFlow.Database/              # DbContext, migrations
│   ├── BillFlow.Repositories/          # Data access implementations
│   └── BillFlow.Shared/                # JWT, Redis, billing helpers
├── Tests/
│   ├── BillFlow.AuthService.Tests/
│   └── BillFlow.ManagementService.Tests/
└── scripts/
    └── setup-env.sh                    # Delegates to ../scripts/setup-env.sh
```

---

## Roadmap

### Completed

- [x] Auth + Management APIs, JWT, Redis sessions
- [x] Docker Compose monorepo architecture
- [x] Billing: clients, items, invoices, payments
- [x] Dashboard, PDF export, CSV/XLSX reports
- [x] Company settings (defaults + PDF issuer block)
- [x] Background overdue invoice sync
- [x] Security hardening + integration tests
- [x] Frontend SPA (in progress — see Frontend README)

### Planned

- [ ] Email / SMTP (password reset, invoice send, payment reminders)
- [ ] Logo upload in company settings
- [ ] Pagination on list endpoints (`GET /clients`, `/invoices`, etc.)
- [ ] Email verification on registration
- [ ] Payment reminder background job

---

## Related documentation

- [Root README](../README.md) — Monorepo setup, Docker, troubleshooting
- [Frontend README](../Frontend/README.md) — Web application
- [Environment template](../.env.example) — All configuration variables
