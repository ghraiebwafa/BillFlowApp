# BillFlow Monorepo

BillFlow is organized as a monorepo with backend services and a frontend app.

## Repository layout

```text
BillFlowProject/
├── Backend/     # .NET 9 APIs, data access, tests, backend Docker setup
└── Frontend/    # Web app (to be implemented)
```

## Quick start (backend only today)

1. Copy backend environment file:

```bash
cp Backend/.env.example Backend/.env
```

2. Start backend stack:

```bash
docker compose --profile backend up -d --build
```

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
