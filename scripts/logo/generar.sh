#!/usr/bin/env bash
# Genera ícono, splash e imágenes de tienda desde scripts/logo/semilla.svg (ImageMagick 7 + librsvg).
set -euo pipefail
R="$(cd "$(dirname "$0")/../.." && pwd)"; L="$R/scripts/logo"; A="$R/src/JardinDeHabitos.App"
T=$(mktemp -d); mkdir -p "$A/Resources/Splash"
# Marca con grano de papel (solo en el color, no en la transparencia).
magick -background none -density 192 "$L/semilla.svg" -resize 1024x1024 -channel RGB -attenuate 0.25 +noise Gaussian +channel "$T/marca.png"
# Frente del ícono adaptable: la semilla cabe en el círculo seguro (66 % central).
magick -size 1024x1024 xc:none \( "$T/marca.png" -resize 600x600 \) -gravity center -geometry +0+10 -composite "$A/Resources/AppIcon/appiconfg.png"
# Pantalla de inicio (Android 12+ la recorta en círculo).
magick -size 1024x1024 xc:none \( "$T/marca.png" -resize 720x720 \) -gravity center -composite "$A/Resources/Splash/splash.png"
# Ícono para tiendas (F-Droid/README): papel y semilla.
magick -size 512x512 radial-gradient:'#F7F1E2-#E9DFC6' \( "$T/marca.png" -resize 400x400 \) -gravity center -composite \
  -channel RGB -attenuate 0.15 +noise Gaussian +channel "$R/fastlane/metadata/android/es-MX/images/icon.png"
cp "$R/fastlane/metadata/android/es-MX/images/icon.png" "$R/docs/capturas/icono.png"
rm -rf "$T"
echo listo
