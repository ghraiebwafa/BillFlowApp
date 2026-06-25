#!/usr/bin/env bash
# Creates root .env from .env.example with cryptographically random secrets.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXAMPLE_FILE="$ROOT_DIR/.env.example"
ENV_FILE="$ROOT_DIR/.env"
LEGACY_ENV="$ROOT_DIR/Backend/.env"

FORCE=false
if [[ "${1:-}" == "--force" ]]; then
  FORCE=true
fi

random_secret() {
  local length="$1"
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 64 | tr -dc 'A-Za-z0-9' | head -c "$length"
  else
    tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$length"
  fi
}

generate_passwords() {
  POSTGRES_PASSWORD="$(random_secret 32)"
  REDIS_PASSWORD="$(random_secret 32)"
  JWT_SECRET="$(random_secret 64)"
  REFRESH_PEPPER="$(random_secret 48)"
  SUPERADMIN_PASSWORD="$(random_secret 10)A1$(random_secret 4)"
}

is_placeholder() {
  local value="$1"
  local min_length="$2"
  [[ -z "$value" ]] && return 0
  [[ "$value" == change_me* ]] && return 0
  [[ "$value" == change-me* ]] && return 0
  [[ "${#value}" -lt "$min_length" ]] && return 0
  return 1
}

read_env_value() {
  local key="$1"
  local file="$2"
  if [[ ! -f "$file" ]]; then
    echo ""
    return
  fi
  grep -E "^${key}=" "$file" | head -n1 | cut -d= -f2- || true
}

apply_secrets_to_env() {
  local sed_inplace=(-i)
  if [[ "$(uname)" == "Darwin" ]]; then
    sed_inplace=(-i '')
  fi

  sed "${sed_inplace[@]}" "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${POSTGRES_PASSWORD}|" "$ENV_FILE"
  sed "${sed_inplace[@]}" "s|^DB_PASSWORD=.*|DB_PASSWORD=${POSTGRES_PASSWORD}|" "$ENV_FILE"
  sed "${sed_inplace[@]}" "s|^REDIS_PASSWORD=.*|REDIS_PASSWORD=${REDIS_PASSWORD}|" "$ENV_FILE"
  sed "${sed_inplace[@]}" "s|^JWT_SECRET=.*|JWT_SECRET=${JWT_SECRET}|" "$ENV_FILE"
  sed "${sed_inplace[@]}" "s|^REFRESH_TOKEN_PEPPER=.*|REFRESH_TOKEN_PEPPER=${REFRESH_PEPPER}|" "$ENV_FILE"
  sed "${sed_inplace[@]}" "s|^SUPERADMIN_PASSWORD=.*|SUPERADMIN_PASSWORD=${SUPERADMIN_PASSWORD}|" "$ENV_FILE"
}

needs_secret_update() {
  is_placeholder "$(read_env_value POSTGRES_PASSWORD "$ENV_FILE")" 8 \
    || is_placeholder "$(read_env_value REDIS_PASSWORD "$ENV_FILE")" 8 \
    || is_placeholder "$(read_env_value JWT_SECRET "$ENV_FILE")" 32 \
    || is_placeholder "$(read_env_value REFRESH_TOKEN_PEPPER "$ENV_FILE")" 16 \
    || is_placeholder "$(read_env_value SUPERADMIN_PASSWORD "$ENV_FILE")" 12
}

print_summary() {
  local action="$1"
  echo "${action} ${ENV_FILE} with generated secrets."
  echo ""
  echo "Start backend:"
  echo "  docker compose up -d --build"
  echo ""
  echo "Local dev (DB ports + frontend):"
  echo "  docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build"
  echo ""
  echo "SuperAdmin email:"
  grep '^SUPERADMIN_EMAIL=' "$ENV_FILE" | cut -d= -f2-
  echo "  password: see SUPERADMIN_PASSWORD in .env"
}

if [[ ! -f "$EXAMPLE_FILE" ]]; then
  echo "Missing ${EXAMPLE_FILE}"
  exit 1
fi

if [[ -f "$ENV_FILE" ]]; then
  if [[ "$FORCE" == true ]]; then
    generate_passwords
    apply_secrets_to_env
    chmod 600 "$ENV_FILE"
    print_summary "Updated"
    exit 0
  fi

  if needs_secret_update; then
    echo "Found placeholder secrets in ${ENV_FILE} — regenerating..."
    generate_passwords
    apply_secrets_to_env
    chmod 600 "$ENV_FILE"
    print_summary "Updated"
    exit 0
  fi

  echo ".env already exists at ${ENV_FILE} with custom secrets — skipping."
  echo "Run with --force to regenerate all passwords."
  exit 0
fi

if [[ -f "$LEGACY_ENV" ]]; then
  echo "Migrating ${LEGACY_ENV} -> ${ENV_FILE}"
  cp "$LEGACY_ENV" "$ENV_FILE"
  if needs_secret_update; then
    generate_passwords
    apply_secrets_to_env
  fi
  chmod 600 "$ENV_FILE"
  print_summary "Migrated"
  exit 0
fi

generate_passwords
cp "$EXAMPLE_FILE" "$ENV_FILE"
apply_secrets_to_env
chmod 600 "$ENV_FILE"
print_summary "Created"
