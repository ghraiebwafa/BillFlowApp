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
echo "Review values, then run:"
echo "  docker compose up -d"
echo "  docker compose --profile tools --profile apps up -d --build"
