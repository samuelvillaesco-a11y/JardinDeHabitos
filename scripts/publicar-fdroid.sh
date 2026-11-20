#!/usr/bin/env bash
# Publica un APK firmado en el repositorio F-Droid propio (github.com/samuelvillaesco-a11y/jardin-fdroid).
#
#   scripts/publicar-fdroid.sh ruta/al/jardin-de-habitos-X.Y.apk
#
# Necesita, FUERA de este repositorio (nunca se suben):
#   $FDROID_DIR/venv       fdroidserver instalado (python3 -m venv venv && venv/bin/pip install fdroidserver)
#   $FDROID_DIR/trabajo    config.yml + keystore.p12 (llave del índice) + metadata/ + repo/
#   $FDROID_DIR/publicado  clon de jardin-fdroid (solo se sube repo/ generado, sin repo/status)
set -euo pipefail

APK="${1:?Uso: $0 ruta/al/jardin-de-habitos-X.Y.apk}"
FDROID_DIR="${FDROID_DIR:-$HOME/.local/share/jardin-de-habitos-fdroid}"
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
APP=org.jardindehabitos.app
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
AAPT="$(ls -d "$ANDROID_HOME"/build-tools/*/ | sort -V | tail -1)aapt"

[ -f "$APK" ] || { echo "No existe $APK" >&2; exit 1; }
paquete=$("$AAPT" dump badging "$APK" | sed -n "s/^package: name='\([^']*\)' versionCode='\([0-9]*\)' versionName='\([^']*\)'.*/\1 \2 \3/p")
read -r nombre codigo version <<< "$paquete"
[ "$nombre" = "$APP" ] || { echo "El APK es de $nombre, no de $APP" >&2; exit 1; }
echo "Publicando $APP $version (versionCode $codigo)"

cd "$FDROID_DIR/trabajo"
cp "$APK" "repo/jardin-de-habitos-$version.apk"

# Textos, capturas y novedades salen del fastlane del repositorio principal.
rm -rf "metadata/$APP"
cp -r "$RAIZ/fastlane/metadata/android" "metadata/$APP"
# Metadatos completos. Una entrada en Builds por cada APK del repo: sin ella el índice v2 no lleva
# las novedades (changelogs/<versionCode>.txt) de esa versión. Nunca poner "disable": fdroid update
# borra los APK de las compilaciones desactivadas.
{
  cat <<'YML'
Categories:
  - Sports & Health
License: MIT
AuthorName: Jardín de Hábitos
WebSite: https://github.com/samuelvillaesco-a11y/JardinDeHabitos
SourceCode: https://github.com/samuelvillaesco-a11y/JardinDeHabitos
IssueTracker: https://github.com/samuelvillaesco-a11y/JardinDeHabitos/issues
Changelog: https://github.com/samuelvillaesco-a11y/JardinDeHabitos/releases
AutoName: Jardín de Hábitos

Builds:
YML
  for a in repo/*.apk; do
    "$AAPT" dump badging "$a" | sed -n "s/^package: name='[^']*' versionCode='\([0-9]*\)' versionName='\([^']*\)'.*/\1 \2/p"
  done | sort -n | while read -r vc vn; do
    printf "  - versionName: '%s'\n    versionCode: %s\n    commit: v%s\n\n" "$vn" "$vc" "$vn"
  done
  printf "CurrentVersion: '%s'\nCurrentVersionCode: %s\n" "$version" "$codigo"
} > "metadata/$APP.yml"

"$FDROID_DIR/venv/bin/fdroid" update --pretty

cd "$FDROID_DIR/publicado"
rsync -a --delete --exclude status/ "$FDROID_DIR/trabajo/repo/" repo/
# Ninguna ruta local ni nombre de equipo debe salir al repositorio público.
if grep -rIl -E "$HOME|$(hostname)" repo; then echo "Hay datos locales en repo/: no se publica." >&2; exit 1; fi
git add -A
git commit -qm "Jardín de Hábitos $version" || { echo "Nada que publicar."; exit 0; }
git push -q
echo "Listo: https://samuelvillaesco-a11y.github.io/jardin-fdroid/repo (GitHub Pages tarda ~1 min)."
