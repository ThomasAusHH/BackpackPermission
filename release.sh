#!/usr/bin/env bash
# Builds the Release configuration and packs the Thunderstore zip into release/.
set -euo pipefail
cd "$(dirname "$0")"

VERSION="$(python -c "import json; print(json.load(open('thunderstore/manifest.json'))['version_number'])")"
OUT="release/BackpackPermission-$VERSION.zip"

dotnet build src/BackpackPermission.csproj -c Release -v minimal

mkdir -p release
rm -f "$OUT"
python - "$OUT" <<'PY'
import sys, zipfile
out = sys.argv[1]
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    z.write("thunderstore/manifest.json", "manifest.json")
    z.write("thunderstore/README.md", "README.md")
    z.write("thunderstore/CHANGELOG.md", "CHANGELOG.md")
    z.write("thunderstore/icon.png", "icon.png")
    z.write("src/bin/Release/netstandard2.1/BackpackPermission.dll", "BackpackPermission.dll")
print("Wrote", out)
PY
