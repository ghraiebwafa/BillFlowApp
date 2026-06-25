# BillFlow Monorepo

BillFlow is organized as a monorepo with backend services and a frontend app.

## Repository layout

```text
BillFlowProject/
├── Backend/     # .NET 9 APIs, data access, tests, backend Docker setup
└── Frontend/    # Web app (to be implemented)
```

## Quick start (backend)

1. Generate backend secrets (creates `Backend/.env` with random passwords):

```bash
./Backend/scripts/setup-env.sh
```

Or copy manually: `cp Backend/.env.example Backend/.env` and fill in `JWT_SECRET`, `REFRESH_TOKEN_PEPPER`, etc.

2. From the **repo root**, start the backend stack:

```bash
cp .env.example .env   # first time only; set POSTGRES_PASSWORD to match Backend/.env
docker compose --profile backend up -d --build
```

If `5432` or `6379` is already in use, set alternate ports in **both** `Backend/.env` and root `.env`:

```bash
# Backend/.env
POSTGRES_PORT=5433
DB_PORT=5433
REDIS_PORT=6381

# root .env (same POSTGRES_PORT / REDIS_PORT / POSTGRES_PASSWORD)
```

> **Note:** `Backend/docker-compose.yml` is a separate stack (profile `apps`, not `backend`). Use one compose file consistently — both define containers with the same names (`billflow-postgres`, etc.).

3. Health checks:

```bash
curl http://127.0.0.1:5237/health
curl http://127.0.0.1:5177/health
```

## Full stack (backend + frontend)

Frontend service is defined in root `docker-compose.yml` under profile `frontend`.
Once the frontend app exists, run:

```bash
docker compose --profile backend --profile frontend up -d --build
```

## Docs

- Backend details: `Backend/README.md`
- Frontend details: `Frontend/README.md`
