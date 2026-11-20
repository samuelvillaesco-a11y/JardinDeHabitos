# JardinDeHabitos

App personal (no Play Store) para dejar malos hábitos: cada hábito es un árbol frutal en un jardín con cuadrícula; el tiempo evitando el hábito es tiempo real de crecimiento del árbol.

## Stack
- .NET 10 (LTS), C#
- `src/JardinDeHabitos.Core`: class library, lógica pura sin UI (SQLite llega después)
- `tests/JardinDeHabitos.Core.Tests`: xUnit
- MAUI (Android) se agrega hasta la Fase 1, cuando ya haya algo que mostrar
- Target inicial: solo Android, x86_64, JDK 21 (Microsoft build) + Android SDK API 36 ya instalados en el equipo

## Reglas de diseño confirmadas (Fase 2, 2026-11-19)

Spec completo: `docs/superpowers/specs/2026-11-19-jardin-secreto-logros-design.md`. Plan: `docs/superpowers/plans/2026-11-19-jardin-secreto-logros.md`.
**Todo tiempo/umbral debe tener fundamento médico** (tabla "Fundamentos" del spec: NIAAA, DSM-5, NCI/NHS, FDA, Lally 2010, Marlatt).

**Discreción:** fuera del estante nunca se muestra el hábito, solo la especie del árbol (jardín, árbol, notificaciones, diálogos). Notificación de logro: siempre "🏆 ¡Desbloqueaste un logro!" / "Abre tu jardín para verlo."

**Hábitos:** se eligen de `CatalogoHabitos` (alfabético, "Otros" al final); nunca se escriben. Solo abstinencia: Apuestas, Pornografía, Sustancias. El resto elige modo. Alcohol y Sustancias muestran aviso médico (Línea de la Vida 800 911 2000). Un árbol por hábito (salvo Otros) y una especie por árbol (10 especies en `EspeciesDeArbol`). Cuestionario opcional: tiempo previo comprobable (nace con esos días) y grupo/terapia (habilita registro de asistencia).

**Crecimiento:** `Habit.DiasDeCrecimiento` (no calendario). +1 por check-in Cumplido o día libre (si el último día normal tuvo check-in), solo si el árbol no está en cuidado. Sin check-in se pausa.

**Strikes declarados** (se acumulan, no tienen que ser seguidos): 1.º plaga 3 días; 2.º plaga + raíces = 8 días (si la plaga sigue activa: fin de plaga + 5); 3.º muerte (semilla, todo a 0).
**Abandono** (contador aparte, mismo efecto): días normales sin check-in; 7 → mini-evento (caracol/rama seca/tierra reseca, se resuelve en segundos); 14/21/28 → strike de abandono 1/2/3. `AvanceDiario.Avanzar` es idempotente y debe correr antes de cualquier check-in (GardenService lo hace en `Sincronizar`).
**Borrón** opcional a los 90 días sin ningún strike (remisión temprana DSM-5); borra ambos contadores.

**Logros** (`CatalogoLogros`, 1-3 estrellas): Generales (visibles siempre), Hábito (≥10 por hábito, visibles al plantar ese hábito; "días libres" solo si se moderó), Ocultos de mini-juegos (inexistentes hasta ganarse, ★≈3 semanas, ★★★≈3 meses), Malditos (debuff en mini-juegos hasta su redención) y Legendarios (Fénix, redenciones, 5 años). Estadísticas en tabla `Estadisticas`; niveles en `NivelesLogro`. Estante (LogrosPage) tras `BiometricPrompt` (huella/PIN/patrón del teléfono).

**Notificaciones** (`Notificaciones.cs`): 9:00 (días libres, etapas, emergencias, plagas por abandono, logros) y 20:00 (recordatorio rotativo si falta check-in).

**Estilo visual:** vectorial/plano, degradados cálidos, "lo-fi"; nada de pixel art.

## Historia (Fases 0-1)

## Estado actual (Fase 0, en progreso)

Ya existen y compilan limpio: `GrowthStage.cs`, `HabitType.cs`, `CheckInResult.cs`, `PenaltyLevel.cs` (enums), `Habit.cs`, `StrikeState.cs` (modelos).

Decisión tomada: `GrowthStage` NO se guarda como campo en `Habit` — se calcula a partir de `FechaDeInicioRachaActual` para que nunca se desincronice del tiempo real transcurrido.

Hecho: `GrowthStageCalculator` (etapa a partir de días transcurridos, umbrales en `Umbrales`) + `RelojAcelerado` (TimeProvider que acelera el tiempo para pruebas, p. ej. `RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1))`). Tests en `GrowthStageCalculatorTests.cs`.

Decisión tomada: umbrales largos en **días fijos**, no meses de calendario: 1 mes = 30 días, 6 meses = 180, 1 año = 365. Toda la lógica recibe un `TimeProvider` inyectado (nunca `DateTime.Now` directo).

Hecho: `CheckInProcessor` + `CheckInOutcome` (tests en `CheckInProcessorTests.cs`). Plan en `docs/superpowers/plans/2026-11-18-checkin-strikes.md`. Decisiones:
- `FechaFinDePenalizacionActiva` es exclusiva (activa mientras hoy < fin); Strike1 = hoy+2, Strike2 = fin+5.
- Strikes 1-2 NO reinician el crecimiento; solo la muerte pone `FechaDeInicioRachaActual = hoy`.
- CheatDay es neutral (no suma ni reinicia racha); en `AbstinenciaTotal` lanza excepción.
- Un solo check-in por día (`StrikeState.FechaUltimoCheckIn`); repetir o con reloj hacia atrás lanza excepción.
- Si hay un hueco sin check-in entre dos rotos, ya no son "seguidos": la racha vuelve a 0 (las cicatrices sí suman).
- LeyendaCaida = etapa FrutaLegendaria justo antes de morir (sin campo extra). Persistir logros a nivel usuario queda pendiente.
- Cicatrices: `PuedeBorrarCicatrices`/`BorrarCicatrices` desde 180 días, nunca automático.
- Días sin check-in (huecos) aún no cuentan como rotos: pendiente decidir.
- PENDIENTE: CheatDay hoy se acepta cualquier día y sin límite. Falta modelar los 1-2 días fijos por semana (plantilla editable cada sábado) y validar contra ellos.

## Checkpoint 2026-11-18 (Fase 1 terminada, unida a `main`)

Hecho (todo commiteado, 86/86 tests Core verdes):
- Core: calendario de cheat days (edición del sábado rige desde el domingo), `GardenService`, logros (LeyendaCaida secreta), `SqliteGardenStore`.
- App MAUI Android (`src/JardinDeHabitos.App`): jardín en cuadrícula, árbol con cielo, check-in, mini-juegos con gestos (bichos, raíces, regar, podar), logros, aviso de día libre 9:00, galería de etapas solo en Debug.
- Probado en emulador `Jardin_API36` (arrancar CON ventana; sin ventana el emulador se cae con exit 139).
- APK Release firmado: `src/JardinDeHabitos.App/bin/Release/net10.0-android/publish/org.jardindehabitos.app-Signed.apk`. NO instalado en el teléfono (teléfono): falta permiso del usuario.

Pendiente (en este orden):
1. ~~Probar en emulador refresco al volver y `Intentar(...)`~~ (hecho 2026-11-18: jardín y árbol se releen al cambiar de día en segundo plano; doble toque en check-in no cierra la app).
2. ~~Menores diferidos~~ (hecho 2026-11-18, probado en emulador): doble toque ya no duplica (navegar, plantar, check-in); check-in + logros en una transacción SQLite (`IGardenStore.EnTransaccion`); esquema versionado con `PRAGMA user_version` (`SqliteGardenStore.VersionActual = 1`, cada cambio de esquema agrega un paso en `Migrar()`; base más nueva que la app lanza error); receptor de aviso con try/catch; aviso se reprograma al encender (`ReprogramarAvisoReceiver`, permiso `RECEIVE_BOOT_COMPLETED`); texto de muerte corregido; timer del árbol a 1 Hz y 30 Hz solo durante mini-juego. 84/84 tests.
   - Modo acelerado (solo Debug): botón en el jardín, 1 día = 1 min, base aparte `jardin-acelerado.db3` que se borra al encender; ancla guardada en Preferences (`RelojAcelerado` acepta `ancla`), así el tiempo sigue tras reiniciar. Release no lo incluye.
3. ~~Merge a `main` e instalar en el teléfono~~ (hecho 2026-11-18: `main` fast-forward; APK Release instalado y abierto en el teléfono). Firmado con la llave debug de .NET (`~/.local/share/Xamarin/Mono for Android/debug.keystore`): las actualizaciones futuras deben firmarse con la misma llave o habrá que desinstalar (y se pierden los datos).

## Pruebas preliminares 2026-11-19 (emulador, modo acelerado, 4 árboles, ~30 días simulados)

Funciona sin cierres: plantar (2 tipos, límite de 2 días libres), check-in, plaga → raíces → muerte, mini-juegos (bichos, raíces, regar, podar), día libre, edición del sábado, refresco del árbol al cambiar de día.

Hallazgos (pendientes):
1. IMPORTANTE: los árboles crecen aunque no se haga check-in (Alcohol llegó a flor sin ningún check-in tras morir). Es la decisión pendiente de "días sin check-in".
2. El error de nombre vacío muestra texto técnico: "(Parameter 'nombre')".
3. La pantalla del jardín no cambia de fecha si se queda abierta al pasar la medianoche (la del árbol sí).
4. Solo hay un logro visible (Fruta Legendaria, a 1 año): falta motivación temprana.
5. Elegir un 3.º día libre quita el primero sin avisar (aceptable, pero puede confundir).

## Siguiente paso

1. ~~Clase que calcula `GrowthStage` + modo acelerado~~ (hecho).
2. ~~Procesador de check-in diario + strikes + muerte~~ (hecho).
3. ~~Tests xUnit de crecimiento, strikes, muerte y hito de 6 meses~~ (hecho).
4. Decidir qué pasa con días sin check-in; modelo de logros de usuario; luego persistencia SQLite.

## Checkpoint 2026-11-19 (Fase 2 en rama `fase2/jardin-secreto`)

Hecho: todo lo de "Reglas de diseño confirmadas (Fase 2)". Esquema SQLite v2 (migra v1: nombre libre → categoría, cicatrices → strikes máx. 2, logros viejos → niveles). 123/123 tests Core.
Probado en emulador: migración de la base real v1, plantar (lista, aviso médico, tiempo previo, especies libres), estante con PIN, check-in roto + plaga + mini-juego, notificación de las 20:00 (disparada a mano con `adb root` + `am broadcast -n org.jardindehabitos.app/crc64003d8bdd0e7a181d.NotificacionesReceiver --ei momento 20`).
Notas: el manifiesto necesita `USE_BIOMETRIC`; la pantalla de PIN sale negra en capturas (FLAG_SECURE).
Hallazgos del 2026-11-19 resueltos en Fase 2: 1 (árboles crecían sin check-in), 2 (ya no hay campo de nombre), 3 (el jardín se relee al pasar la medianoche), 4 (logros tempranos y por hábito). El 5 (3.er día libre quitaba el primero) también quedó resuelto: ahora avisa y no quita nada.
Las reglas de "Historia (Fases 0-1)" quedan reemplazadas por las de Fase 2 donde choquen.
Estante de emblemas (commit 0b4bfa8): bronce/plata/oro según estrellas, malditos negros, legendarios rojos/morados/arcoíris; detalle solo al tocar; punto rojo y "Recién conseguidos"; filtros por categoría/rango/fecha que solo muestran Mini-juegos/Malditos/Legendarios después de ganar uno. 132/132 tests.
Probado también en modo acelerado: 8 días sin visita → "¡Necesita ayuda!" + caracoles; el rescate desbloquea el oculto "Rescate a tiempo" con aviso genérico.
OJO: en el emulador la app PhoneBox (net.oxog.phonebox.deskpanel) puede pasar al frente y recibir los toques; el teléfono puede estar conectado por USB: usar siempre `adb -s emulator-5554`.

Fase 2 unida a `main` (fast-forward, 46856b7) e instalada en el teléfono el 2026-11-19 con `adb install -r` del APK Release firmado (datos conservados, abre sin cierres).

## Seguridad local (2026-11-19)
- Sin correo, sin cuentas de terceros, sin internet: enviar códigos por correo exigía credenciales dentro del APK o un servidor, y un mailto no autentica. Decisión del usuario: quitar el correo por completo.
- Clave de la app opcional (PBKDF2-SHA256, 600 000 iteraciones, sal de 16 bytes, en SecureStorage), código de recuperación de un solo uso, espera creciente tras 5 intentos fallidos.
- Seguros: logros (ON), blog botánico (ON), general (OFF), doble verificación (OFF). Cada cambio y la entrada al panel piden verificación.
- Release solo pide POST_NOTIFICATIONS, RECEIVE_BOOT_COMPLETED y USE_BIOMETRIC (Debug agrega INTERNET por el depurador). Copias de seguridad desactivadas.
- Pantallas con FLAG_SECURE salen negras en capturas: para probar en el emulador leer la UI con `uiautomator dump`. Instalación limpia en el emulador: `-p:EmbedAssembliesIntoApk=true`.

## Parte 1 + candados + cifrado (2026-11-19)
- Pestañas abajo (TabbedPage, ToolbarPlacement.Bottom): Jardín · Blog botánico · Ajustes. El trofeo vive en la esquina del jardín.
- Ajustes: música (on/off), Seguridad (siempre pide verificación), Privacidad, Acerca de. Blog: por ahora solo la puerta con llave (diario y emociones llegan en la parte 3).
- Candado a elegir (primera vez y en Seguridad): bloqueo del teléfono (por defecto), PIN propio (4-8 dígitos) o patrón propio (3×3, mín. 4 puntos). Solo hash PBKDF2. Claves de texto viejas siguen valiendo (tipo Clave).
- Base cifrada con SQLCipher (`sqlite-net-sqlcipher`); llave aleatoria en SecureStorage (`datos.llave`). Una base en claro se cifra al abrirla.
- Música: "Forest Ambience" de TinyWorlds (CC0, OpenGameArt) en `Resources/Raw/bosque.mp3`, MediaPlayer en bucle al 35 %.
- OJO patrón: un GraphicsView dentro de ScrollView pierde el arrastre en Android (aunque Orientation = Neither): las pantallas de patrón no usan ScrollView.
- Licencia MIT en `LICENSE`. Repo privado hasta terminar README con capturas y pulir todo.
- IMPORTANTE (2026-11-20): un APK Release de compilación incremental cerró la app en el teléfono al abrir ("Invalid compressed assembly descriptor index"). Antes de instalar en el teléfono: `rm -rf src/*/bin src/*/obj` y `dotnet publish -c Release` desde cero; luego abrirla y revisar `adb logcat -b crash`.

## Personalización (2026-11-20)
- Ajustes › 🎨 Personalización: colores calmantes, degradados predefinidos, degradado propio de 2 colores, selector de color (tono/saturación/brillo o #RRGGBB) y 4 escenas SVG propias (`Resources/Images/escena_*.svg`, BaseSize 360×640).
- `Core/Apariencia/Fondo.cs`: `Fondo` (guardado en Preferences `apariencia.fondo`), `Paleta`, `ColorHex`, `Contraste` (WCAG). Encima del fondo va un velo verde musgo con la opacidad mínima para contraste ≥ 4.5:1 del texto claro.
- `Apariencia.Aplicar(page)` en OnAppearing de Jardín, Blog, Ajustes, Logros y Personalización: envuelve el contenido en `CapaFondo` (fondo hasta los bordes con `SafeAreaEdges.None`; contenido con `SafeAreaRegions.Container`, si no se mete bajo la barra de estado).
- Créditos de recursos en `ASSETS.md`.
- Emulador: si se cae con errores de GPU (gfxstream), reiniciarlo sin `-gpu swiftshader_indirect` (con swiftshader da segfault). En `adb shell input text` el `#` hay que escaparlo (`'\#RRGGBB'`).

## Parte 2: perfil, jardineros y ayuda (2026-11-20)
- Ajustes › 🆘 Pedir ayuda (SIN candado): 911, jardineros (Llamar / Mensaje con texto discreto que no menciona hábito ni app) y Línea de la Vida. Tras "Lo rompí" el aviso ofrece "Pedir ayuda".
- Ajustes › 👤 Perfil y jardineros (con verificación): nombre, correo, teléfono, peso, altura, actividad física (todo opcional) + hasta 5 jardineros.
- Sin permisos nuevos: llamadas por ACTION_DIAL (el usuario confirma), SMS por ACTION_SENDTO, contactos por ACTION_PICK (solo el contacto elegido; sin READ_CONTACTS). Resultado del selector: `MainActivity.OnActivityResult` → `Telefono.AlVolver`.
- Esquema SQLite v3: tablas `Perfil` (fila única) y `Jardineros`, cifradas como todo lo demás.
- Probar en emulador: contacto de prueba con `adb shell content insert --uri content://com.android.contacts/raw_contacts ...`; el marcador abre con 911 y `dumpsys telephony.registry` muestra mCallState=0 (no llama).
