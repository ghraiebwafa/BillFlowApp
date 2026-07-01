# BillFlow

BillFlow is an invoicing and billing platform for small businesses. It lets business owners (**Visitors**) manage clients, catalog items, create and send invoices, record payments, export reports, and configure company defaults — all from a single web application backed by .NET microservices.

This repository is a **monorepo**: backend APIs, background workers, frontend SPA, Docker configuration, and CI live in one place with a single root `.env` file.

---

## What is in this repository?

| Part | Folder | Description |
|------|--------|-------------|
| **Auth API** | `Backend/Services/BillFlow.AuthService` | Registration, login, JWT tokens, profile, password |
| **Management API** | `Backend/Services/BillFlow.ManagementService` | Admin user management + full billing domain |
| **Background jobs** | `Backend/Services/BillFlow.BackgroundJobs` | Scheduled overdue-invoice sync worker |
| **Data layer** | `Backend/DataAccess` | EF Core, entities, repositories, shared libraries |
| **Tests** | `Backend/Tests` | Integration and unit tests (Testcontainers) |
| **Frontend** | `Frontend` | React + Vite + TypeScript SPA |
| **Docker** | Root `docker-compose*.yml` | Base, local, and production compose stacks |
| **Scripts** | `scripts/` | Environment setup (`setup-env.sh`) |
| **CI** | `.github/workflows/` | GitHub Actions for backend build and test |
| **Docs assets** | `images/` | Screenshots and diagrams for documentation |

---

## Technology stack

| Layer | Technology | Default port |
|-------|------------|--------------|
| Auth API | ASP.NET Core 9 | `5237` |
| Management API | ASP.NET Core 9 | `5177` |
| Background worker | .NET 9 Worker | — (no HTTP) |
| Database | PostgreSQL 16 | `5433` on host (local override) |
| Cache | Redis 7 | `6381` on host (local override) |
| Frontend | React 19, Vite, TypeScript, Tailwind CSS v4 | `3000` |

---

## Prerequisites

Install these before you start:

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (for the frontend)
- [Docker](https://www.docker.com/) and Docker Compose v2
- [Git](https://git-scm.com/)

Optional but useful:

- `curl` or an HTTP client (for health checks)
- An IDE with C# and TypeScript support (Rider, VS Code, Visual Studio)

---

## First-time setup

All commands below are run from the **repository root** unless stated otherwise.

### Step 1 — Create environment secrets

BillFlow stores secrets in a single root `.env` file (never committed to git).

```bash
./scripts/setup-env.sh
```

This script:

1. Copies `.env.example` → `.env` if `.env` does not exist
2. Migrates `Backend/.env` → `.env` automatically if you have a legacy file
3. Generates random passwords for Postgres, Redis, JWT, refresh-token pepper, and SuperAdmin
4. Sets file permissions to `600`

To **regenerate all secrets** in an existing `.env`:

```bash
./scripts/setup-env.sh --force
```

### Step 2 — Fix port conflicts (if needed)

If another project already uses PostgreSQL (`5432`) or Redis (`6379`) on your machine, edit `.env`:

```env
POSTGRES_PORT=5433
DB_PORT=5433
REDIS_PORT=6381
```

`DB_PORT` must match `POSTGRES_PORT` when running APIs with `dotnet run` on the host.

### Step 3 — Start the backend

**Recommended — full API stack in Docker:**

```bash
docker compose up -d --build
```

**Local development — expose database ports and optional frontend container:**

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

### Step 4 — Verify services

```bash
curl http://127.0.0.1:5237/health
curl http://127.0.0.1:5177/health
```

Both should return JSON with `"status":"healthy"`.

| Service | Swagger UI |
|---------|------------|
| Auth | http://127.0.0.1:5237/swagger |
| Management | http://127.0.0.1:5177/swagger |

### Step 5 — Start the frontend (development)

```bash
cd Frontend
cp .env.example .env
npm install
npm run dev
```

Open http://localhost:3000

---

## Docker Compose files explained

BillFlow uses three compose files, layered like the Athlo project architecture:

| File | Purpose |
|------|---------|
| `docker-compose.yml` | **Base stack** — Postgres, Redis, Auth, Management, Background jobs. Database is internal-only by default. |
| `docker-compose.local.yml` | **Local overrides** — exposes Postgres/Redis on localhost; adds the frontend container. |
| `docker-compose.prod.yml` | **Production overrides** — keeps DB/Redis off the host; sets `Production` environment and safer defaults. |

### Common commands

```bash
# Start everything (APIs only)
docker compose up -d --build

# Local dev with DB ports + frontend
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build

# Production deployment
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Rebuild one service after code changes
docker compose up -d --build auth-service
docker compose up -d --build management-service

# Stop all containers
docker compose down

# Stop and delete database volumes (full reset)
docker compose down -v

# View logs
docker compose logs -f management-service
```

---

## Running APIs on your host (faster iteration)

Use this when you want hot reload with `dotnet run` but still use Docker for Postgres and Redis.

```bash
./scripts/setup-env.sh
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d postgres redis
```

Ensure `.env` points at localhost:

```env
DB_HOST=localhost
DB_PORT=5433
REDIS_HOST=localhost
REDIS_PORT=6381
APPLY_MIGRATIONS=true
```

Run each API in a separate terminal:

```bash
cd Backend
dotnet run --project Services/BillFlow.AuthService
```

```bash
cd Backend
dotnet run --project Services/BillFlow.ManagementService
```

---

## User roles

| Role | Created by | Access |
|------|------------|--------|
| **Visitor** | Public registration (Auth API) | Own billing data: clients, invoices, payments, settings |
| **Admin** | SuperAdmin via Management API | Manage Visitors (admin module) |
| **SuperAdmin** | Seeded from `.env` on first startup | Full admin management |

The frontend routes users by role:

- **Visitor** → `/dashboard`, billing navigation
- **Admin / SuperAdmin** → `/admin/users`

---

## Environment variables

All backend configuration lives in the **root** `.env`. See [.env.example](.env.example) for every variable and its purpose.

The frontend has its own small `.env` in `Frontend/`:

| Variable | Description |
|----------|-------------|
| `VITE_AUTH_API_URL` | Auth API base URL (default `http://localhost:5237`) |
| `VITE_MANAGEMENT_API_URL` | Management API base URL (default `http://localhost:5177`) |

Never commit `.env` files. Only `.env.example` is tracked in git.

---

## Running tests

**Backend** (requires Docker for Testcontainers):

```bash
cd Backend
dotnet test
```

**Frontend**:

```bash
cd Frontend
npm run typecheck
npm run build
```

CI runs backend tests automatically on pushes to `Backend/**` (see `.github/workflows/backend-ci.yml`).

---

## Project structure

```
BillFlowProject/
├── Backend/
│   ├── Services/
│   │   ├── BillFlow.AuthService/         # Identity & tokens
│   │   ├── BillFlow.ManagementService/   # Admin + billing APIs
│   │   └── BillFlow.BackgroundJobs/      # Overdue invoice worker
│   ├── DataAccess/
│   │   ├── BillFlow.Models/
│   │   ├── BillFlow.Database/            # EF Core + migrations
│   │   ├── BillFlow.Repositories/
│   │   └── BillFlow.Shared/
│   ├── Tests/
│   └── Dockerfile
├── Frontend/
│   ├── src/                              # React application source
│   ├── public/                           # Static assets (logo, icons)
│   └── Dockerfile
├── images/                               # Documentation screenshots
├── scripts/
│   └── setup-env.sh                      # Generate root .env secrets
├── .github/workflows/
│   └── backend-ci.yml
├── docker-compose.yml
├── docker-compose.local.yml
├── docker-compose.prod.yml
├── .env.example
└── README.md                             # ← you are here
```

---

## SuperAdmin first login

On first Management service startup, a SuperAdmin account is created from `.env`:

- `SUPERADMIN_EMAIL`
- `SUPERADMIN_PASSWORD`

Login via the Auth API:

```bash
curl -s -X POST http://127.0.0.1:5237/api/v1.0/auth/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"superadmin@billflow.local","password":"YOUR_PASSWORD_FROM_ENV"}'
```

Use the returned `accessToken` in Management Swagger: **Authorize** → `Bearer <token>`.

---

## Troubleshooting

### `POSTGRES_PASSWORD` or `REFRESH_TOKEN_PEPPER` missing

Run `./scripts/setup-env.sh` from the repo root. Docker Compose reads the **root** `.env`, not `Backend/.env`.

### Port already allocated (`5432` / `6379`)

Another container or local service is using the port. Set alternate ports in `.env` (see Step 2 above) and restart:

```bash
docker compose down
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

### Container name conflict

If you previously used an old compose setup, stop old containers first:

```bash
docker compose down
docker ps -a --filter name=billflow
```

### Frontend cannot reach APIs

Check `Frontend/.env` URLs match running services. CORS in development allows `localhost:3000` and `localhost:5173`.

### Database reset after password change

```bash
docker compose down -v
./scripts/setup-env.sh --force
docker compose up -d --build
```

---

## Further reading

- [Backend/README.md](Backend/README.md) — API reference, billing modules, migrations, security
- [Frontend/README.md](Frontend/README.md) — SPA architecture, routing, auth flow, UI conventions
- [images/README.md](images/README.md) — Where to put documentation screenshots

---

## License and contributions

BillFlow is open source under the [MIT License](LICENSE).

- [Contributing guide](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

Do not commit secrets, production credentials, or personal `.env` files.

---

## Roadmap (open source)

BillFlow is being built in public. Planned milestones:

| Step | Focus | Status |
|------|--------|--------|
| 1 | Open-source foundation + toast notifications | Done |
| 2 | Audit trail (who changed what, when) | Done |
| 3 | Email invoice delivery + branded PDF templates | Planned |
| 4 | Customer portal (view/pay invoice via secure link) | Planned |
| 5 | Recurring invoices + payment gateway (Stripe) | Planned |
| 6 | Multi-currency, advanced reports, accounting export | Planned |

See [CONTRIBUTING.md](CONTRIBUTING.md) to pick up the next item.
