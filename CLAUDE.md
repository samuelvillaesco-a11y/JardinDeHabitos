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

## Siguiente paso

1. ~~Clase que calcula `GrowthStage` + modo acelerado~~ (hecho).
2. Clase que procesa un check-in diario, actualiza `StrikeState` (ambos contadores) y decide si el árbol muere.
3. Tests xUnit cubriendo: crecimiento normal por cada etapa, 1 strike, 2 strikes seguidos, 3 strikes seguidos (muerte + reset), un día limpio en medio, y el hito de 6 meses.
