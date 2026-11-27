# Recursos y licencias

| Recurso | Archivo | Autor | Licencia | Fuente |
|---|---|---|---|---|
| Música "Forest Ambience" | `src/JardinDeHabitos.App/Resources/Raw/bosque.ogg` | TinyWorlds | CC0 (dominio público) | https://opengameart.org/content/forest-ambience |
| Escenas de fondo: bosque, jardín en flor, casa de campo, suburbio al atardecer | `src/JardinDeHabitos.App/Resources/Images/escena_*.png` | Compuestas para el proyecto con piezas de "Background Elements Redux" de Kenney (www.kenney.nl), recoloreadas a la paleta | CC0 1.0 (piezas; texto en `licencias/Kenney-BackgroundElementsRedux-CC0.txt`) | https://opengameart.org/content/background-elements-redux · se regeneran con `scripts/escenas/componer.py` |
| Ícono de la app y logo de la pantalla de inicio (árbol sobre atardecer) | `src/JardinDeHabitos.App/Resources/AppIcon/*.svg`, `Resources/Splash/splash.svg` | Proyecto Jardín de Hábitos | PolyForm Noncommercial 1.0.0 | Dibujos vectoriales propios |
| Ícono del widget (jardinero con sombrero de paja y teléfono) | `src/JardinDeHabitos.App/Platforms/Android/Resources/drawable/ic_jardinero.xml` | Proyecto Jardín de Hábitos | PolyForm Noncommercial 1.0.0 | Dibujo vectorial propio |
| Fuente Nunito (Regular, Bold) | `src/JardinDeHabitos.App/Resources/Fonts/Nunito-*.ttf` | The Nunito Project Authors | SIL Open Font License 1.1 (texto en `licencias/Nunito-OFL.txt`) | https://github.com/googlefonts/nunito |
| Fuente Lora SemiBold (títulos; instancia fija wght=600 de la variable) | `src/JardinDeHabitos.App/Resources/Fonts/Lora-SemiBold.ttf` | The Lora Project Authors | SIL Open Font License 1.1 (texto en `licencias/Lora-OFL.txt`) | https://github.com/cyrealtype/Lora-Cyrillic |
| Íconos Lucide (20 íconos recortados de lucide-static 1.81.0) | `src/JardinDeHabitos.App/Resources/Fonts/Lucide.ttf` | Lucide Contributors | ISC (texto en `licencias/Lucide-ISC.txt`) | https://lucide.dev |
| Logo e ícono: semilla abierta con brote-corazón (textura de tinta y papel) | `scripts/logo/semilla.svg` → `Resources/AppIcon/appiconfg.png`, `Resources/Splash/splash.png`, `fastlane/.../icon.png` (con `scripts/logo/generar.sh`) | Proyecto Jardín de Hábitos | PolyForm Noncommercial 1.0.0 | Dibujo propio |
| Título "Jardín de Hábitos" de la pantalla de inicio | `src/JardinDeHabitos.App/Platforms/Android/Resources/drawable/marca_titulo.xml` | Proyecto Jardín de Hábitos (letras de Lora SemiBold convertidas a trazos) | SIL OFL 1.1 (derivado de Lora) | Generado con hb-shape + fontTools |
| Botón "Consíguelo en Obtainium" (solo en el README) | `docs/insignias/obtainium.png` | Proyecto Obtainium | Gráfico oficial para enlazar a Obtainium (su wiki pide alojar una copia) | https://wiki.obtainium.imranr.dev/deep_links/ |

Las únicas imágenes de terceros son las piezas CC0 de Kenney (dominio público, hechas a mano, sin IA). Antes se descartaron fondos "libres" de estilo anime porque eran generados o asistidos por IA con procedencia dudosa.
