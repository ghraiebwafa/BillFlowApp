#!/usr/bin/env bash
# Copy cached .nupkg files into Backend/.nuget-local for offline restores
# when api.nuget.org is unreachable (NU1301 / connection reset).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$ROOT/Backend/.nuget-local"
SRC="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
mkdir -p "$DEST"
count=0
copied=0
while IFS= read -r -d '' pkg; do
  count=$((count + 1))
  base="$(basename "$pkg")"
  if [[ ! -f "$DEST/$base" ]]; then
    cp "$pkg" "$DEST/"
    copied=$((copied + 1))
  fi
done < <(find "$SRC" -name '*.nupkg' -type f -print0 2>/dev/null)
echo "Found $count package(s) under $SRC; copied $copied new into $DEST"
echo "Offline restore:"
echo "  cd Backend && dotnet restore --source ./.nuget-local && dotnet build --no-restore"
