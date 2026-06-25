#!/usr/bin/env bash
# Delegates to the monorepo root setup script.
exec "$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/scripts/setup-env.sh" "$@"
