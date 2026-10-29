#!/usr/bin/env bash
# Compila o mod no container do Claude e prepara uma cópia pronta para instalar
# no PC do Paulo (via device_commit_files) em /mnt/project-files/build/WuxiaMod.
#
# Antes, as DLLs do jogo precisam estar staged do PC do Paulo (device_stage_files):
#   <Outer Space>\Outer Space_Data\Managed\{Assembly-CSharp, UnityEngine*, Unity.TextMeshPro,
#     com.rlabrecque.steamworks.net}.dll
#   <workshop 3080070>\3803065949\v_common\Assemblies\0Harmony.dll
# Elas ficam só no container, nunca vão para o Git.
#
# Uso: tools/claude_build.sh            compila e prepara a pasta
#      tools/claude_build.sh --list     também imprime os pares origem -> destino no PC
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MANAGED="${MANAGED:-/mnt/user-data/uploads/common--Outer Space/Outer Space_Data/Managed}"
HARMONY="${HARMONY:-/mnt/user-data/uploads/3080070/3803065949/v_common/Assemblies/0Harmony.dll}"
OUT="${OUT:-/mnt/project-files/build/WuxiaMod}"
GAME_MODS='C:\Program Files (x86)\Steam\steamapps\common\Outer Space\mods\WuxiaMod'

if ! command -v dotnet >/dev/null; then
  apt-get update -qq && apt-get install -y -qq dotnet-sdk-8.0 >/dev/null
fi
for f in "$MANAGED/Assembly-CSharp.dll" "$MANAGED/com.rlabrecque.steamworks.net.dll" "$HARMONY"; do
  [ -f "$f" ] || { echo "Falta $f: faça o stage das DLLs do jogo primeiro." >&2; exit 1; }
done

dotnet build "$ROOT/src/WuxiaMod/WuxiaMod.csproj" -c Release -nologo -v q \
  "-p:GameManagedDir=$MANAGED" "-p:HarmonyDll=$HARMONY"

rm -rf "$OUT"
mkdir -p "$OUT"
cp -r "$ROOT/Mod/Manifest" "$ROOT/Mod/v_common" "$OUT/"
rm -f "$OUT/v_common/Assemblies/.gitkeep"
echo "Pronto em $OUT"

if [ "${1:-}" = "--list" ]; then
  (cd "$OUT" && find . -type f | sed 's|^\./||' | sort) | while read -r rel; do
    printf '%s\t%s\\%s\n' "$OUT/$rel" "$GAME_MODS" "${rel//\//\\}"
  done
fi
