#!/usr/bin/env bash
# Creates .env from .env.example with cryptographically random secrets.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXAMPLE="${ROOT}/.env.example"
TARGET="${ROOT}/.env"

if [[ -f "${TARGET}" ]]; then
  echo "Refusing to overwrite existing .env — delete it first if you want a fresh setup."
  exit 1
fi

if ! command -v openssl >/dev/null 2>&1; then
  echo "openssl is required to generate secrets."
  exit 1
fi

rand() {
  openssl rand -base64 48 | tr -d '/+=' | head -c "${1:-32}"
}

cp "${EXAMPLE}" "${TARGET}"

replace() {
  local key="$1"
  local value="$2"
  # Escape sed replacement delimiters in value
  local escaped
  escaped="$(printf '%s' "${value}" | sed 's/[&/\]/\\&/g')"
  sed -i "s|^${key}=.*|${key}=${escaped}|" "${TARGET}"
}

POSTGRES_PASS="$(rand 40)"
REDIS_PASS="$(rand 40)"
PGADMIN_PASS="$(rand 32)"
JWT_SECRET="$(rand 64)"
REFRESH_PEPPER="$(rand 48)"
SUPERADMIN_PASS="$(rand 40)"

replace "POSTGRES_PASSWORD" "${POSTGRES_PASS}"
replace "DB_PASSWORD" "${POSTGRES_PASS}"
replace "REDIS_PASSWORD" "${REDIS_PASS}"
replace "PGADMIN_DEFAULT_PASSWORD" "${PGADMIN_PASS}"
replace "JWT_SECRET" "${JWT_SECRET}"
replace "REFRESH_TOKEN_PEPPER" "${REFRESH_PEPPER}"
replace "SUPERADMIN_PASSWORD" "${SUPERADMIN_PASS}"
replace "SUPERADMIN_EMAIL" "superadmin@billflow.local"
replace "REDIS_HOST" "localhost"
replace "JWT_ISSUER" "BillFlow"
replace "JWT_AUDIENCE" "BillFlow.Api"
# Keep app DB user aligned with Postgres role
replace "DB_USER" "billflow"
replace "POSTGRES_USER" "billflow"
replace "DB_NAME" "billflow"
replace "POSTGRES_DB" "billflow"

chmod 600 "${TARGET}"
echo "Created ${TARGET} with random secrets (mode 600)."

MONOREPO_ROOT="$(cd "${ROOT}/.." && pwd)"
MONOREPO_ENV="${MONOREPO_ROOT}/.env"
if [[ ! -f "${MONOREPO_ENV}" ]]; then
  {
    echo "# Used by root docker-compose.yml for host port mapping"
    echo "# Keep POSTGRES_PORT / REDIS_PORT aligned with Backend/.env"
    grep -E '^(POSTGRES_USER|POSTGRES_PASSWORD|POSTGRES_DB|POSTGRES_PORT|REDIS_PORT|AUTH_SERVICE_PORT|MANAGEMENT_SERVICE_PORT|ASPNETCORE_ENVIRONMENT)=' "${TARGET}"
    echo "FRONTEND_PORT=3000"
  } > "${MONOREPO_ENV}"
  chmod 600 "${MONOREPO_ENV}"
  echo "Created ${MONOREPO_ENV} for monorepo docker compose."
fi

echo "If ports 5432/6379 are already in use on your machine, set in Backend/.env:"
echo "  POSTGRES_PORT=5433  DB_PORT=5433"
echo "  REDIS_PORT=6381"
echo "Then sync the same POSTGRES_PORT and REDIS_PORT into ${MONOREPO_ROOT}/.env"
echo ""
echo "Review values, then run from repo root:"
echo "  docker compose --profile backend up -d --build"
echo "Or from Backend/:"
echo "  docker compose --profile apps up -d --build"
