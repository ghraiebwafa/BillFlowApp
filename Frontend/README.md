# BillFlow Frontend

Frontend SPA for BillFlow (React + Vite + TypeScript).

## Implemented foundation

- Dark-capable UI with **light default**
- Tailwind CSS enabled
- App shell for billing + admin in one app
- i18n baseline (`en`, `fr`)
- Router + auth guards scaffold
- Real auth integration (login/register/logout/refresh)
- Management API client with token refresh retry
- Company settings form (GET/Upsert)

## Local run

```bash
cp .env.example .env
npm install
npm run dev
```

Requires backend APIs running (default `http://localhost:5237` and `http://localhost:5177`).

## Build

```bash
npm run build
npm run preview
```
