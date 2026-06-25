# BillFlow

BillFlow is a **billing platform** built with **.NET 9**. Today it provides the backend foundation: user identity, JWT authentication, and admin user management. Invoice and client features are planned next, followed by a frontend.

For monorepo usage, see the root `README.md` and root `docker-compose.yml`.

---

## What this project does

BillFlow is split into two HTTP APIs that share **PostgreSQL** and **Redis**:

| Service | Port (default) | Purpose |
|---------|----------------|---------|
| **Auth** | `5237` | Sign up, sign in, tokens, profile, password, logout |
| **Management** | `5177` | SuperAdmin creates Admins; Admins manage Visitors |

### User roles

| Role | How they are created | What they can do |
|------|----------------------|------------------|
| **Visitor** | Public register on Auth API | Sign in, manage own account |
| **Admin** | Created by SuperAdmin (Management API) | Manage Visitors |
| **SuperAdmin** | Seeded on first Management startup (from `.env`) | Full admin management |

### Auth API (`/api/v1.0/auth/account`)

- Register (Visitor only)
- Login / refresh token
- Profile, logout, change password
- Deactivate / delete account
- Reset password (**dev only** — requires `ALLOW_DEV_RESET_PASSWORD=true`)

### Management API (`/api/v1.0/management`)

- **Admin** — CRUD (SuperAdmin only)
- **Visitor** — CRUD (Admin or SuperAdmin)

### Tech stack

- .NET 9, ASP.NET Core, Entity Framework Core, PostgreSQL
- Redis (session invalidation, rate limiting)
- JWT access + refresh tokens
- Docker Compose for local infrastructure

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

---

## How to run

### Option 1 — Full stack in Docker (recommended)

Everything runs in containers: Postgres, Redis, Auth, and Management.

**1. Clone and enter the project**

```bash
git clone https://github.com/ghraiebwafa/BillFlowApp.git
cd BillFlowApp
```

**2. Create secrets (first time only)**

```bash
./scripts/setup-env.sh
chmod 600 .env
```

This copies `.env.example` → `.env` and fills in random passwords.

**3. Fix port conflicts (if needed)**

If ports `5432` or `6379` are already in use on your machine, edit `.env`:

```env
POSTGRES_PORT=5433
REDIS_PORT=16379
DB_PORT=5433
```

**4. Start all services**

```bash
docker compose --profile apps up -d --build
```

**5. Check that services are up**

```bash
curl http://127.0.0.1:5237/health
curl http://127.0.0.1:5177/health
```

Both should return `"status":"healthy"`.

**6. Open Swagger**

| Service | URL |
|---------|-----|
| Auth | http://127.0.0.1:5237/swagger |
| Management | http://127.0.0.1:5177/swagger |

In Swagger, click **Authorize** and enter: `Bearer <your-access-token>` (include the word `Bearer`).

**7. Rebuild after code changes**

```bash
docker compose --profile apps up -d --build auth-service
docker compose --profile apps up -d --build management-service
```

**8. Stop**

```bash
docker compose --profile apps down
```

To reset the database (e.g. after changing `POSTGRES_PASSWORD`):

```bash
docker compose --profile apps down -v
```

---

### Option 2 — APIs on your machine, infra in Docker

Run Postgres and Redis in Docker, but run the .NET APIs with `dotnet run` (faster for day-to-day coding).

**1. Start infrastructure only**

```bash
./scripts/setup-env.sh   # if you have not already
docker compose up -d     # postgres + redis (+ pgAdmin)
```

**2. Point `.env` at localhost**

```env
DB_HOST=localhost
DB_PORT=5433          # must match POSTGRES_PORT in .env
REDIS_HOST=localhost
REDIS_PORT=16379      # must match REDIS_PORT in .env
APPLY_MIGRATIONS=true
```

**3. Run both APIs** (two terminals)

```bash
dotnet run --project Services/BillFlow.AuthService
```

```bash
dotnet run --project Services/BillFlow.ManagementService
```

Swagger URLs are shown in the terminal output (typically `5237` and `5177`).

---

## First login (SuperAdmin)

On first Management startup, a SuperAdmin account is created from `.env`:

- `SUPERADMIN_EMAIL`
- `SUPERADMIN_PASSWORD`

Login via the **Auth** API (same credentials work for Management Swagger):

```bash
curl -s -X POST http://127.0.0.1:5237/api/v1.0/auth/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"superadmin@billflow.local","password":"YOUR_SUPERADMIN_PASSWORD"}'
```

Copy `accessToken` from the response and use it in Management Swagger to create Admins.

---

## Visitor signup

1. **POST** `/api/v1.0/auth/account/register` — include `confirmPassword` matching `password`
2. **POST** `/api/v1.0/auth/account/login` — same email and password

Or use `Services/BillFlow.AuthService/BillFlow.AuthService.http` from your IDE.

---

## Tests

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) (Docker must be running):

```bash
dotnet test
```

---

## Production notes

- Set `ASPNETCORE_ENVIRONMENT=Production` in `.env`.
- Set `APPLY_MIGRATIONS=false` and run EF migrations from CI or a one-off job.
- Set `ALLOW_DEV_RESET_PASSWORD=false` (public reset-password returns 404).
- Re-enable email/OTP verification before a public launch.
- Never commit `.env` — only `.env.example` is tracked.

---

## Project structure

```
BillFlow/
├── Services/
│   ├── BillFlow.AuthService/        # Identity API
│   └── BillFlow.ManagementService/  # Admin API
├── DataAccess/
│   ├── BillFlow.Models/             # Entities & DTOs
│   ├── BillFlow.Database/           # EF Core + migrations
│   ├── BillFlow.Repositories/
│   └── BillFlow.Shared/             # JWT, Redis, CORS, rate limits
├── Tests/
├── docker-compose.yml
├── .env.example
└── scripts/setup-env.sh
```

---

## Roadmap

- [x] Auth + Management APIs, Docker, JWT, Redis sessions
- [ ] Billing domain (clients, invoices, payments)
- [ ] Frontend web app
