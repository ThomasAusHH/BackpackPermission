#!/usr/bin/env bash
# Builds the mod and copies it into the game. Adjust GAME_DIR if needed.
set -euo pipefail
cd "$(dirname "$0")"

GAME_DIR="/c/Program Files (x86)/Steam/steamapps/common/PEAK"
PLUGIN_DIR="$GAME_DIR/BepInEx/plugins/BackpackPermission"

if tasklist 2>/dev/null | grep -qi "PEAK.exe"; then
  echo "ERROR: PEAK is running and locks the plugin DLL. Close the game, then run ./deploy.sh again." >&2
  exit 1
fi

dotnet build src/BackpackPermission.csproj -c Release -v minimal

if [ ! -d "$GAME_DIR/BepInEx/plugins" ]; then
  echo "ERROR: $GAME_DIR/BepInEx/plugins does not exist - install BepInEx first." >&2
  exit 1
fi

mkdir -p "$PLUGIN_DIR"
cp src/bin/Release/netstandard2.1/BackpackPermission.dll "$PLUGIN_DIR/"
echo "Deployed to: $PLUGIN_DIR"
