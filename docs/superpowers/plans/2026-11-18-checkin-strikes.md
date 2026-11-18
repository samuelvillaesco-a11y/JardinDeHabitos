# Check-in diario y Strikes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Procesar el check-in diario de un hábito, actualizar `StrikeState` (racha de rotos + cicatrices), aplicar penalizaciones y decidir la muerte del árbol.

**Architecture:** Clase pura `CheckInProcessor` en `JardinDeHabitos.Core`. Recibe `GrowthStageCalculator` (que ya trae el `TimeProvider`) para saber "hoy" y la etapa actual. Muta `Habit` y `StrikeState` y devuelve un `CheckInOutcome` inmutable que describe qué pasó.

**Tech Stack:** .NET 10, C#, xUnit 2.9.

**Spec:** `CLAUDE.md` (secciones "Check-in diario", "Strikes", "Cheat days", "Logros").

## Global Constraints

- Lógica pura, sin UI ni persistencia. Nunca `DateTime.Now`: el tiempo viene del `TimeProvider` del `GrowthStageCalculator`.
- Strike 1 (1 día roto) = 2 días de cuidado. Strike 2 (2 seguidos) = 5 días adicionales (total 1 semana). Strike 3 (3 seguidos) = el árbol muere, TODO se resetea a 0.
- Un día Cumplido reinicia la racha a 0; el periodo de cuidado en curso se completa igual.
- Strikes totales ("cicatrices") NO se borran con días limpios; solo al morir, o de forma opcional (no automática) cuando el árbol llega a 6 meses (180 días fijos).
- Cheat days solo en hábitos `Moderable`.
- LeyendaCaida: árbol que ya llegó a FrutaLegendaria y muere.
- Nombres de dominio en español, como el código existente.

## Review Focus

1. Doble check-in el mismo día → no debe contar dos strikes (si no, 3 toques = muerte en un día). Rechazar con `InvalidOperationException`. Test en Task 2.
2. CheatDay en hábito `AbstinenciaTotal` → rechazar. Test en Task 2.
3. Roto después de que la penalización ya expiró → nueva penalización desde ese día, no extender una vieja. Test en Task 1.
4. Roto–Cumplido–Roto con penalización aún activa → no acortar el fin de penalización existente. Test en Task 1.
5. Muerte en día 365+ → LeyendaCaida; muerte en día 364 → no. Test en Task 3.

---

### Task 1: Roto / Cumplido, penalización y muerte

**Files:**
- Create: `src/JardinDeHabitos.Core/CheckInProcessor.cs`, `src/JardinDeHabitos.Core/CheckInOutcome.cs`
- Test: `tests/JardinDeHabitos.Core.Tests/CheckInProcessorTests.cs`

**Interfaces:**
- Consumes: `GrowthStageCalculator(TimeProvider?)`, `.Hoy`, `.CalcularEtapa(Habit)`.
- Produces:
  - `public sealed record CheckInOutcome(bool ArbolMurio, PenaltyLevel Penalizacion, DateOnly? FinDePenalizacion, bool LeyendaCaida)`
  - `public class CheckInProcessor(GrowthStageCalculator calculadora)` con `public CheckInOutcome Procesar(Habit habito, StrikeState estado, CheckInResult resultado)`.

Semántica (decidida aquí): `FechaFinDePenalizacionActiva` es exclusiva — la penalización está activa mientras `hoy < fin`. Al inicio de `Procesar`, si `fin <= hoy`, se limpia (`Ninguna`, `null`).
- Roto: racha++, totales++.
  - racha 1 → nivel = max(nivel, Strike1); fin = max(fin, hoy+2).
  - racha 2 → nivel = Strike2; fin = (fin ?? hoy) + 5.
  - racha 3 → muerte: `estado` vuelve a todo 0/`Ninguna`/`null`; `habito.FechaDeInicioRachaActual = hoy`.
- Cumplido: racha = 0; nivel/fin no cambian.
- Strikes 1–2 NO reinician `FechaDeInicioRachaActual`.

Tests (reloj manual en UTC, inicio 2026-01-01, hábito AbstinenciaTotal):

- [ ] **Step 1: Write failing tests**
  - `Cumplido_NoCambiaNada`: racha 0, totales 0, `Penalizacion == Ninguna`, `ArbolMurio == false`.
  - `UnRoto_Strike1_DosDiasDeCuidado`: hoy D → racha 1, totales 1, nivel Strike1, fin D+2.
  - `DosRotosSeguidos_Strike2_UnaSemanaTotal`: Roto en D, Roto en D+1 → nivel Strike2, fin D+7, totales 2.
  - `TresRotosSeguidos_ArbolMuereYResetea`: D, D+1, D+2 → `ArbolMurio`, racha 0, totales 0, nivel Ninguna, fin null, `FechaDeInicioRachaActual == D+2`.
  - `DiaLimpioEnMedio_ReiniciaRachaPeroNoCicatrices`: Roto D, Cumplido D+1, Roto D+2 → no muere, racha 1, totales 2, nivel Strike1, fin D+4 (= max(D+2, D+2+2)).
  - `Cumplido_NoAcortaPenalizacionEnCurso`: Roto D, Cumplido D+1 → nivel Strike1, fin D+2.
  - `PenalizacionExpira`: Roto D, Cumplido D+2 → nivel Ninguna, fin null.
  - `RotoTrasExpirar_NuevaPenalizacionDesdeHoy`: Roto D, Cumplido D+1, Cumplido D+5, Roto D+6 → fin D+8.
  - `StrikeNoReiniciaCrecimiento`: inicio racha = D-10, Roto D → `FechaDeInicioRachaActual` sigue D-10.

- [ ] **Step 2: Run, expect FAIL** — `dotnet test` → compilation error (`CheckInProcessor` no existe).
- [ ] **Step 3: Implement** `CheckInOutcome` y `CheckInProcessor.Procesar` según semántica de arriba.
- [ ] **Step 4: Run, expect PASS** — `dotnet test` → todos verdes.
- [ ] **Step 5: Commit** `feat: procesador de check-in con strikes y muerte del árbol`.

### Task 2: Cheat day y guardas

**Files:**
- Modify: `src/JardinDeHabitos.Core/StrikeState.cs` (agregar `public DateOnly? FechaUltimoCheckIn { get; set; }`)
- Modify: `src/JardinDeHabitos.Core/CheckInProcessor.cs`
- Test: `tests/JardinDeHabitos.Core.Tests/CheckInProcessorTests.cs`

**Interfaces:**
- Consumes: `CheckInProcessor.Procesar` de Task 1.
- Produces: `StrikeState.FechaUltimoCheckIn` (se fija a `hoy` en cada check-in aceptado; la muerte NO lo borra).

Semántica: CheatDay es neutral — no suma ni reinicia la racha, no toca cicatrices.

- [ ] **Step 1: Write failing tests**
  - `CheatDay_EnAbstinenciaTotal_Lanza`: `InvalidOperationException`.
  - `CheatDay_EnModerable_EsNeutral`: Roto D, CheatDay D+1 → racha 1, totales 1. Luego Roto D+2 → Strike2 (racha 2).
  - `DobleCheckInMismoDia_Lanza`: Roto D, Roto D → `InvalidOperationException`; totales sigue 1.
  - `CheckInDiaSiguiente_Permitido`: Cumplido D, Cumplido D+1 → sin excepción.
- [ ] **Step 2: Run, expect FAIL** — tests nuevos fallan (no lanza / CheatDay no manejado).
- [ ] **Step 3: Implement** guardas al inicio de `Procesar`; fijar `FechaUltimoCheckIn = hoy` al final.
- [ ] **Step 4: Run, expect PASS** — `dotnet test`.
- [ ] **Step 5: Commit** `feat: cheat days y guarda de doble check-in`.

### Task 3: LeyendaCaida y cicatrices a los 6 meses

**Files:**
- Modify: `src/JardinDeHabitos.Core/CheckInProcessor.cs`
- Test: `tests/JardinDeHabitos.Core.Tests/CheckInProcessorTests.cs`

**Interfaces:**
- Produces: `public bool PuedeBorrarCicatrices(Habit habito, StrikeState estado)` y `public void BorrarCicatrices(Habit habito, StrikeState estado)` (lanza `InvalidOperationException` si no puede).

LeyendaCaida: la etapa solo baja al morir, así que basta calcular la etapa ANTES de reiniciar `FechaDeInicioRachaActual`. Sin campo nuevo en `Habit`.
Cicatrices: se puede borrar si etapa ≥ `FrutaComestible` (180 días) y `StrikesTotalesDelArbolActual > 0`. Nunca automático.

- [ ] **Step 1: Write failing tests**
  - `Muerte_TrasFrutaLegendaria_DisparaLeyendaCaida`: inicio racha D-365, 3 rotos D..D+2 → `LeyendaCaida == true`.
  - `Muerte_AntesDeLegendaria_NoDispara`: inicio racha D-362, 3 rotos D..D+2 (día 364 al morir) → `false`.
  - `Cicatrices_NoBorrablesAntesDe180Dias`: día 179, totales 1 → `PuedeBorrar == false`; `BorrarCicatrices` lanza.
  - `Cicatrices_BorrablesA180Dias`: día 180, totales 2 → `true`; tras borrar totales 0, racha intacta.
  - `Cicatrices_NoSeBorranSolas`: día 180 + Cumplido → totales siguen igual.
- [ ] **Step 2: Run, expect FAIL**.
- [ ] **Step 3: Implement**.
- [ ] **Step 4: Run, expect PASS** — `dotnet test`.
- [ ] **Step 5: Commit** `feat: logro LeyendaCaida y borrado opcional de cicatrices`, y actualizar `CLAUDE.md` (Estado actual / Siguiente paso).
