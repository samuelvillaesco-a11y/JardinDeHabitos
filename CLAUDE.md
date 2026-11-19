# JardinDeHabitos

App personal (no Play Store) para dejar malos hábitos: cada hábito es un árbol frutal en un jardín con cuadrícula; el tiempo evitando el hábito es tiempo real de crecimiento del árbol.

## Stack
- .NET 10 (LTS), C#
- `src/JardinDeHabitos.Core`: class library, lógica pura sin UI (SQLite llega después)
- `tests/JardinDeHabitos.Core.Tests`: xUnit
- MAUI (Android) se agrega hasta la Fase 1, cuando ya haya algo que mostrar
- Target inicial: solo Android, x86_64, JDK 21 (Microsoft build) + Android SDK API 36 ya instalados en el equipo

## Reglas de diseño confirmadas

**Hábitos:** hasta 9-10, cada uno `AbstinenciaTotal` (alcohol, pornografía, apuestas, cigarro — sin cheat days) o `Moderable` (dieta: azúcar, harinas, café — sí permite cheat days).

**Check-in diario:** una vez al día el usuario marca Cumplido / Roto / CheatDay.

**Strikes (dos contadores separados):**
- Racha consecutiva de días rotos: escala la penalización. Strike 1 (1 día roto) = 2 días de cuidado de plaga simple. Strike 2 (2 seguidos) = 5 días adicionales por raíces podridas (total acumulado 1 semana). Strike 3 (3 seguidos) = el árbol muere, TODO se resetea a 0. Un día cumplido reinicia esta racha a 0 (el periodo de cuidado ya en curso se completa igual).
- Strikes totales del árbol actual: histórico visible ("cicatrices"), NO se borra con días limpios — solo al morir el árbol, o de forma opcional cuando el árbol llega a 6 meses (ahí se da la opción, no automática, de borrarlo).

**Cheat days** (solo hábitos Moderable): 1-2 días fijos por semana (domingo-sábado), editable cada sábado o reutilizable como plantilla. No piden check-in; el árbol se muestra "en paz". Notificación de disclaimer recordando moderación.

**Etapas de crecimiento (8, ligadas a tiempo real desde el inicio de la racha actual):** Semilla (día 0) → Brote (día 1-2) → Arbusto (día 3-4) → ÁrbolJoven (día 5-6) → Flor (día 7) → FrutaInmadura (1 mes) → FrutaComestible (6 meses) → FrutaLegendaria (1 año). Los umbrales de los primeros días son propuestos/ajustables; semana/mes/6 meses/año están confirmados.

**Cuidado activo diario:** regar/podar libremente en cualquier momento, no solo durante penalización — loop principal tipo "mascota virtual".

**Logros (a nivel de usuario, NO del árbol — persisten aunque el árbol muera):**
- FrutaLegendaria: normal, se registra al llegar a esa etapa.
- LeyendaCaida: SECRETO — se dispara si un árbol que ya llegó a FrutaLegendaria vuelve a morir. Placa dorada permanente. No anunciar ni listar antes de desbloquearse.

**Tratamientos:** mini-juegos con gestos (tocar, arrastrar) — nunca solo botones de confirmación.

**V1 (alcance actual):** solo temporizadores + registro manual. Detección automática de apps/webs queda fuera, fase futura. Un solo dispositivo (SQLite local). Funciones sociales (apadrinaje, contactos de emergencia) son ideas futuras.

**Estilo visual (para cuando lleguemos al arte):** vectorial/plano, degradados cálidos (verdes, cafés, amarillos), sensación "lo-fi" de bajo renderizado — nada de pixel art retro.

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

## Siguiente paso

1. ~~Clase que calcula `GrowthStage` + modo acelerado~~ (hecho).
2. ~~Procesador de check-in diario + strikes + muerte~~ (hecho).
3. ~~Tests xUnit de crecimiento, strikes, muerte y hito de 6 meses~~ (hecho).
4. Decidir qué pasa con días sin check-in; modelo de logros de usuario; luego persistencia SQLite.
