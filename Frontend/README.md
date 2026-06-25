# BillFlow Frontend

The BillFlow frontend is a **single-page application (SPA)** built with React, Vite, and TypeScript. It provides the web interface for business owners to manage billing and for administrators to manage users — all in one app with role-based routing.

For backend setup and Docker, see the [root README](../README.md). For API details, see [Backend/README](../Backend/README.md).

---

## Table of contents

1. [Overview](#overview)
2. [Technology stack](#technology-stack)
3. [Prerequisites](#prerequisites)
4. [Getting started](#getting-started)
5. [Environment variables](#environment-variables)
6. [Project structure](#project-structure)
7. [Architecture](#architecture)
8. [Authentication flow](#authentication-flow)
9. [Routing and roles](#routing-and-roles)
10. [API integration](#api-integration)
11. [Internationalization](#internationalization)
12. [Theming](#theming)
13. [Implemented features](#implemented-features)
14. [Scripts](#scripts)
15. [Docker](#docker)
16. [Roadmap](#roadmap)

---

## Overview

BillFlow's UI is designed as a **mobile-first** billing application with a warm brand palette (cream, orange, maroon). On small screens it uses a bottom navigation bar; on desktop it shows a sidebar.

The app talks to two backend services:

| API | Purpose |
|-----|---------|
| **Auth** (`:5237`) | Login, register, logout, token refresh |
| **Management** (`:5177`) | Billing data, dashboard, company settings |

---

## Technology stack

| Category | Choice |
|----------|--------|
| Framework | React 19 |
| Build tool | Vite 8 |
| Language | TypeScript |
| Styling | Tailwind CSS v4 + CSS custom properties |
| Routing | React Router v7 |
| Server state | TanStack Query |
| Client state | Zustand (session store) |
| Forms | React Hook Form + Zod |
| Icons | Lucide React |
| i18n | i18next (English + French) |

---

## Prerequisites

- [Node.js 20+](https://nodejs.org/)
- npm (comes with Node)
- Running BillFlow backend (see root README)

---

## Getting started

### 1. Start the backend

From the **repository root**:

```bash
./scripts/setup-env.sh
docker compose up -d --build
```

Verify APIs:

```bash
curl http://localhost:5237/health
curl http://localhost:5177/health
```

### 2. Configure the frontend

```bash
cd Frontend
cp .env.example .env
```

Default `.env`:

```env
VITE_AUTH_API_URL=http://localhost:5237
VITE_MANAGEMENT_API_URL=http://localhost:5177
```

Change these if your APIs run on different hosts or ports.

### 3. Install and run

```bash
npm install
npm run dev
```

Open http://localhost:5173

### 4. First visit flow

1. Open `/welcome` — choose **Login** or **Register**
2. **Register** as a Visitor → redirected to **Company Settings** to complete your profile
3. After saving settings, use the dashboard and billing navigation

---

## Environment variables

Vite only exposes variables prefixed with `VITE_`.

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `VITE_AUTH_API_URL` | Yes | `http://localhost:5237` | Auth service base URL (no trailing slash) |
| `VITE_MANAGEMENT_API_URL` | Yes | `http://localhost:5177` | Management service base URL |

Type definitions live in `src/vite-env.d.ts`. Runtime access via `src/shared/config/env.ts`.

---

## Project structure

```
Frontend/
├── public/
│   └── assets/              # Logo, icons (served at /assets/...)
├── src/
│   ├── app/                 # App shell, router, providers
│   │   ├── App.tsx
│   │   ├── router.tsx
│   │   └── providers.tsx    # QueryClient, i18n
│   ├── domain/              # TypeScript types + mappers (no React)
│   │   ├── auth/
│   │   └── billing/
│   ├── features/            # Feature modules (pages + feature logic)
│   │   ├── auth/pages/      # Welcome, Login, Register
│   │   ├── dashboard/
│   │   ├── clients/
│   │   ├── company-settings/
│   │   └── admin/
│   ├── shared/
│   │   ├── api/             # HTTP clients (auth + management)
│   │   ├── auth/            # Session store, guards, token storage
│   │   ├── config/
│   │   ├── i18n/            # en.ts, fr.ts
│   │   ├── layout/          # AppShell, AuthLayout, BottomNav
│   │   └── ui/              # FormField, ThemeToggle, etc.
│   ├── styles/
│   │   └── global.css       # Tailwind + design tokens
│   └── main.tsx
├── .env.example
├── Dockerfile               # nginx production image
├── index.html
├── package.json
└── README.md
```

### Folder conventions

| Folder | Rule |
|--------|------|
| `domain/` | Pure types and mapping functions — no React imports |
| `features/` | One folder per business area; pages live in `pages/` |
| `shared/` | Reusable code used by multiple features |
| `app/` | Application wiring only (router, providers, root component) |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  main.tsx → App → Providers (Query + i18n) → Router     │
└───────────────────────────┬─────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
   Guest routes        Visitor routes      Admin routes
   /welcome            /dashboard          /admin/users
   /login              /clients            ...
   /register           /invoices
                       /settings/company
```

### Layer responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Pages** | Compose UI, call hooks, handle user events |
| **shared/api** | HTTP requests, error parsing, token attachment |
| **shared/auth** | Session persistence, route guards, role checks |
| **domain** | API response types, form ↔ API mappers |

---

## Authentication flow

1. **Login / Register** calls the Auth API via `auth-api.ts`.
2. Tokens are stored in `sessionStorage` (`token-storage.ts`).
3. User profile (including role) is kept in Zustand (`session-store.ts`).
4. On app load, `AuthBootstrap` restores the session and refreshes tokens if needed.
5. **Management API** requests attach `Authorization: Bearer <accessToken>`.
6. On **401**, `management-client.ts` attempts one token refresh, then retries; on failure the session is cleared.

### Register flow

Registration only creates the account. The frontend then **auto-logs in** and redirects to `/settings/company` so new users configure company defaults before invoicing.

---

## Routing and roles

| Route | Guard | Role |
|-------|-------|------|
| `/` | `HomeRedirect` | Sends authenticated users to their home path |
| `/welcome` | `GuestOnly` | Unauthenticated landing |
| `/login`, `/register` | `GuestOnly` | Auth forms |
| `/dashboard` | `RequireVisitor` | Business owner home |
| `/clients` | `RequireVisitor` | Client list |
| `/items`, `/invoices`, `/reports` | `RequireVisitor` | Placeholders (in progress) |
| `/settings/company` | `RequireVisitor` | Company settings form |
| `/admin/users` | `RequireAdmin` | Admin user management |

### Home paths by role

| Role | Redirect target |
|------|-----------------|
| Visitor | `/dashboard` |
| Admin / SuperAdmin | `/admin/users` |

Unauthenticated users are sent to `/welcome`.

---

## API integration

### Auth client (`shared/api/auth-api.ts`)

Base path: `{VITE_AUTH_API_URL}/api/v1.0/auth/account`

Endpoints: `login`, `register`, `refresh-token`, `profile`, `logout`

### Management client (`shared/api/management-client.ts`)

Base path: `{VITE_MANAGEMENT_API_URL}`

- Attaches Bearer token from session store
- Retries once after refresh on 401
- Parses API errors into `ApiError` with `detail` from ProblemDetails

### TanStack Query

Server data uses query keys like `["dashboard", "summary"]`, `["clients", search]`, `["company-settings"]`. Mutations invalidate or update cache on success.

---

## Internationalization

Supported languages: **English** (`en`) and **French** (`fr`).

- Translation files: `src/shared/i18n/locales/en.ts`, `fr.ts`
- Switcher component: `LanguageSwitcher` (in app header and auth screens)
- Usage: `const { t } = useTranslation();` → `t("nav.clients")`

Add new keys to **both** locale files when introducing UI text.

---

## Theming

- **Default:** light theme with warm cream/orange palette
- **Dark mode:** toggle via `ThemeToggle`; persisted in `localStorage` under `theme`
- CSS variables defined in `src/styles/global.css` under `:root` and `html[data-theme="dark"]`

Brand tokens:

| Token | Light value | Usage |
|-------|-------------|-------|
| `--billflow-orange` | `#f27121` | Buttons, accents |
| `--billflow-maroon` | `#4a150b` | Headings, primary text |
| `--billflow-cream` | `#fff9f0` | Page background |

---

## Implemented features

| Feature | Status | Route |
|---------|--------|-------|
| Welcome landing | Done | `/welcome` |
| Login / Register | Done | `/login`, `/register` |
| Session restore + refresh | Done | — |
| Dashboard summary | Done | `/dashboard` |
| Company settings form | Done | `/settings/company` |
| Clients list + search | Done | `/clients` |
| Items module | Placeholder | `/items` |
| Invoices module | Placeholder | `/invoices` |
| Reports module | Placeholder | `/reports` |
| Admin users | Placeholder | `/admin/users` |
| Mobile bottom nav | Done | Visitor routes |
| Desktop sidebar | Done | Visitor routes |

---

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Vite dev server (http://localhost:5173) |
| `npm run build` | Typecheck + production build to `dist/` |
| `npm run preview` | Serve production build locally |
| `npm run typecheck` | TypeScript check only |

---

## Docker

The frontend Dockerfile builds with Node and serves static files via nginx on port **3000**.

Included in the **local** compose stack:

```bash
# From repo root
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build frontend
```

Open http://localhost:3000

For development, `npm run dev` is usually faster (hot reload).

---

## Roadmap

### Next UI work

- [ ] Invoice list and detail (match design mockups)
- [ ] Create / edit client modal
- [ ] Items catalog CRUD
- [ ] Invoice create, send, PDF download
- [ ] Payments UI
- [ ] Reports export download
- [ ] Admin users management screen

### Nice to have

- [ ] Forgot password flow (when backend email is ready)
- [ ] Toast notifications for save/error feedback
- [ ] Pagination on long lists

---

## Troubleshooting

### Network errors on login

- Confirm backend is running: `curl http://localhost:5237/health`
- Check `Frontend/.env` URLs match your setup
- CORS allows `localhost:5173` in development

### 401 on billing pages

- Session may have expired — log out and log in again
- Ensure you registered as a **Visitor** (not Admin) for billing routes

### Company settings 404 on first visit

Expected for new users — the form shows defaults; saving calls `PUT Upsert` to create settings.

### Build errors

```bash
npm run typecheck
```

Fix any TypeScript errors before `npm run build`.

---

## Related documentation

- [Root README](../README.md) — Monorepo setup, Docker, environment
- [Backend README](../Backend/README.md) — API reference and billing rules
