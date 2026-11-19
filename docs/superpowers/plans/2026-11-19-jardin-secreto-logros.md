# Jardín secreto y logros personalizados — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hábitos elegidos de una lista, jardín anónimo, reglas nuevas de crecimiento/strikes/abandono, logros en 3 niveles + malditos/legendarios, estante con bloqueo y notificaciones juguetonas.

**Architecture:** Toda regla vive en `JardinDeHabitos.Core` (sin MAUI) con tests xUnit y `TimeProvider` inyectado. Un procesador diario idempotente (`Avanzar`) evalúa los días pasados (crecimiento de días libres, abandono, mini-eventos). Los logros son datos (catálogo) + un evaluador sobre estadísticas persistidas. La App solo pinta y reporta acciones.

**Tech Stack:** .NET 10, MAUI Android, sqlite-net, xUnit, `Android.Hardware.Biometrics.BiometricPrompt` (API 30+ con credencial del dispositivo; `KeyguardManager` en API < 30).

**Spec:** `docs/superpowers/specs/2026-11-19-jardin-secreto-logros-design.md`

## Global Constraints

- Ningún texto visible fuera del estante (jardín, árbol, notificaciones, diálogos) nombra el hábito; solo la especie.
- Notificación de logro, siempre exacta: "🏆 ¡Desbloqueaste un logro!" / "Abre tu jardín para verlo."
- Plaga = 3 días; raíces hasta 8 días desde el strike (o fin de plaga + 5 si la plaga sigue activa); 3.º strike = muerte.
- Abandono: 7 → mini-evento; 14/21/28 días normales sin check-in → strike de abandono 1/2/3.
- Borrón: 90 días de calendario desde el último strike (cualquier contador).
- Ocultos/malditos/legendarios: ninguna API de vista los devuelve antes de desbloquearse.
- Todo umbral de logro sale de la tabla "Fundamentos" del spec.
- Esquema SQLite pasa a `VersionActual = 2` sin perder datos de v1.

## Review Focus

1. Abrir la app tras 30+ días sin entrar: el abandono debe aplicarse en orden (plaga 14, raíces 21, muerte 28) una sola vez, aunque `Avanzar` corra varias veces el mismo día.
2. Base v1 real del teléfono (nombres libres, especies repetidas, `Logros` viejos) debe migrar sin cerrar la app.
3. Un logro oculto/maldito no puede filtrarse por conteos ("3 de 40 logros") ni por huecos en la lista.
4. Dos strikes en días seguidos ⇒ fin de cuidado exactamente 8 días después del primero.
5. Día libre durante cuidado o durante abandono no da crecimiento.

---

### Task 1: Catálogo de hábitos y especies (Core)

**Files:** Create `src/JardinDeHabitos.Core/MalHabito.cs`, `src/JardinDeHabitos.Core/Especies.cs`; Test `tests/JardinDeHabitos.Core.Tests/CatalogoTests.cs`

**Produces:**
- `enum MalHabito { Alcohol, Apuestas, Azucares, Cafeina, Cigarro, Compras, Harinas, Pornografia, Pantallas, Sal, Sustancias, Otros }`
- `record HabitoInfo(MalHabito Habito, string Nombre, bool PermiteModerar, bool PermiteAbstinencia, string? AvisoMedico)`; `static class CatalogoHabitos { IReadOnlyList<HabitoInfo> Todos /* orden alfabético por Nombre es-MX, Otros al final */; HabitoInfo De(MalHabito); MalHabito DesdeNombreLibre(string) }`
- `static class EspeciesDeArbol { IReadOnlyList<string> Todas /* 10 */ }`

- [ ] Tests: `Todos_AlfabeticoYOtrosAlFinal`; `Apuestas_Pornografia_Sustancias_SoloAbstinencia`; `Alcohol_Cigarro_Otros_PermitenAmbos`; `Alcohol_Y_Sustancias_TienenAvisoMedico` (contiene "800 911 2000"); `DesdeNombreLibre("cigarros")==Cigarro`, `("azúcar")==Azucares`, `("xyz")==Otros`; `Especies_SonDiezYUnicas`.
- [ ] Implementar; dieta/cafeína/compras/pantallas: `PermiteModerar` y `PermiteAbstinencia` true.
- [ ] `dotnet test` verde; commit.

### Task 2: Modelo de árbol y check-in nuevo (Core)

**Files:** Modify `Habit.cs`, `StrikeState.cs`, `HabitRecord.cs`, `CheckInOutcome.cs`; Rewrite `CheckInProcessor.cs`; Rewrite tests `CheckInProcessorTests.cs`; Delete `PenaltyLevel` uses that no longer apply (keep enum: computed).

**Produces (campos):**
- `Habit`: `MalHabito Categoria`, `int DiasDeCrecimiento`, `DateOnly FechaDeInicioRachaActual` (fecha de plantado/última muerte), `bool ApoyoGrupo`, `bool ApoyoTerapia`; se elimina `Nombre` (la vista usa la especie).
- `StrikeState`: `int StrikesDeclarados`, `int StrikesAbandono`, `DateOnly? FinPlaga`, `DateOnly? FinRaices`, `DateOnly? FechaUltimoCheckIn`, `DateOnly? FechaUltimoStrike`, `int DiasSinStrikeBase` (días de crecimiento al último strike), `bool FenixPendiente`, `int DiasNormalesSinCheckIn`, `DateOnly UltimoDiaEvaluado`, `MiniEvento MiniEvento`, `int AbandonoAplicado` (0..3 en la ausencia actual).
- `enum MiniEvento { Ninguno, Caracol, RamaSeca, TierraReseca }`
- `CheckInOutcome(bool ArbolMurio, int DiasAntesDeMorir, bool EmpezoCuidado)`
- `CheckInProcessor`: `CheckInOutcome Procesar(Habit, StrikeState, CheckInResult)`, `bool EnCuidado(StrikeState, DateOnly)`, `bool TienePlaga(...)`, `bool TieneRaices(...)`, `bool PuedeBorron(StrikeState, DateOnly)`, `void Borron(StrikeState, DateOnly)`, `static void AplicarStrike(StrikeState, DateOnly hoy, int numero)`.

- [ ] Tests:
  - `Cumplido_SanoSumaUnDia`; `Cumplido_EnCuidadoNoSuma`.
  - `Strike1_Plaga3Dias` (FinPlaga = hoy+3, FinRaices null).
  - `Strike2_ConPlagaActiva_SoloRaices_TotalOchoDias` (día0 strike, día1 strike ⇒ FinPlaga=d0+3, FinRaices=d0+8).
  - `Strike2_SinPlagaActiva_PlagaYRaices` (FinPlaga=hoy+3, FinRaices=hoy+8).
  - `StrikesNoConsecutivosSeAcumulan` (strike día 0, cumplidos 10 días, strike ⇒ StrikesDeclarados=2).
  - `Strike3_Muere_TodoACero` (DiasDeCrecimiento 0, contadores 0, FechaDeInicioRachaActual=hoy, `DiasAntesDeMorir` = días previos).
  - `Borron_Antes90Dias_NoSePuede`, `Borron_A90Dias_BorraAmbosContadores`.
  - `Fenix_365SinStrikes_PrimerStrike_MarcaPendiente`.
  - Conserva: un check-in por día; CheatDay solo moderable y solo en día libre.
- [ ] Implementar. Cuidado vigente mientras `hoy < max(FinPlaga, FinRaices)`. `FenixPendiente = (DiasDeCrecimiento - DiasSinStrikeBase) >= 365 && StrikesDeclarados+StrikesAbandono == 0` al recibir strike 1.
- [ ] Tests verdes; commit.

### Task 3: Avance diario: días libres, abandono, mini-eventos (Core)

**Files:** Create `src/JardinDeHabitos.Core/AvanceDiario.cs`; Test `tests/JardinDeHabitos.Core.Tests/AvanceDiarioTests.cs`

**Produces:** `sealed record EventoDelDia(DateOnly Fecha, TipoEvento Tipo)`; `enum TipoEvento { CrecioPorDiaLibre, MiniEvento, StrikeAbandono, MurioPorAbandono }`; `static IReadOnlyList<EventoDelDia> AvanceDiario.Avanzar(Habit, StrikeState, DateOnly hoy)` — recorre `UltimoDiaEvaluado+1 .. hoy-1` (días ya cerrados) una sola vez.

Reglas por día cerrado `d`:
- Día libre (según `CheatDaySchedule.EsDiaDeCheat`): +1 crecimiento si `DiasNormalesSinCheckIn == 0` y no en cuidado en `d`.
- Día normal sin check-in en `d`: `DiasNormalesSinCheckIn++`; en 7 → `MiniEvento` (aleatorio determinista por fecha), en 14/21/28 → `AplicarStrike(estado, d+1, ++StrikesAbandono)` (abandono usa su contador; 3 ⇒ muerte).
- Un check-in pone `DiasNormalesSinCheckIn = 0` (en `Procesar`).

- [ ] Tests: `DiaLibre_TrasCheckIn_Crece`; `DiaLibre_EnCuidado_NoCrece`; `DiaLibre_DuranteAbandono_NoCrece`; `SieteDiasSinEntrar_MiniEvento`; `Abandono14_Plaga_21_PlagaYRaices_28_Muere` (abriendo el día 30 de una vez); `Avanzar_DosVecesMismoDia_NoDuplica`; `CheckIn_ReiniciaContadorDeAbandono`; `AbandonoNoSeMezclaConDeclarados` (2 declarados + 1 abandono ⇒ vive).
- [ ] Implementar; tests verdes; commit.

### Task 4: Estadísticas y catálogo de logros (Core)

**Files:** Create `Logros/Metrica.cs`, `Logros/LogroDef.cs`, `Logros/CatalogoLogros.cs` (+ `CatalogoLogros.Habitos.cs`), `Logros/EvaluadorLogros.cs`; Delete `Logro.cs`; Test `LogrosTests.cs`

**Produces:**
- `enum CategoriaLogro { General, Habito, Oculto, Maldito, Legendario }`
- `record LogroDef(string Id, CategoriaLogro Categoria, MalHabito? Habito, string Titulo, string Descripcion /* con {n} */, Metrica Metrica, int[] Niveles, Debuff? Debuff = null, string? Redime = null)`
- `enum Metrica` y claves de `Estadisticas` (diccionario `string→int`; las por hábito usan sufijo `:{MalHabito}`): `MejorRacha`, `MejorRachaTrasMuerte`, `CheckIns`, `RotosHonestos`, `CuidadosCompletados`, `Asistencias`, `DiasLibresRespetados`, `RachaCheckIns`, `Borrones`, `ArbolesPlantados`, `CheckInsTotales`, `RachaJardinCompleto`, `DiasRegados`, `DiasPodados`, `ArbolesEnFlorALaVez`, `RamasCortadas`, `BichosQuitados`, `RaicesSacadas`, `MiniEventosResueltos`, `RiegosNocturnos`, `RiegosMadrugada`, `DiasCuidadoCompleto` y las de malditos/legendarios (`LeyendaCaida`, `TierraBaldia`, `JardinOlvidado`, `Ceniza`, `Fenix`, `Redenciones…`, `ArbolesLegendariosALaVez`, `CincoInviernos`).
- `record NivelLogro(string Id, int Nivel, DateOnly Fecha)`
- `EvaluadorLogros.Nuevos(IReadOnlyDictionary<string,int> stats, IReadOnlyList<NivelLogro> tenidos, DateOnly hoy) → IReadOnlyList<NivelLogro>`
- `CatalogoLogros.Visibles(IReadOnlySet<MalHabito> elegidos, IReadOnlyList<NivelLogro> tenidos)` — Generales siempre; Hábito solo si elegido; Oculto/Maldito/Legendario solo si tienen nivel ≥1.

Contenido: ≥12 generales; ≥10 por cada `MalHabito` (incl. Otros) con umbrales del spec (p. ej. Alcohol MejorRacha 3·14·30 / 90·180·365 / 730·1095·1825; Cigarro 3·7·28 / 90·180·365; Apuestas 30·90·180 / 365·510·730; Pornografía 7·30·90 / 180·365·730; Cafeína 3·9·30 / 66·180·365; Sal 28·56·84; resto 14·30·66 / 90·180·254); 8–10 ocultos con ★ ≈21 días y ★★★ ≈90 días de cuidado regular (ramas 60·135·270 a 3 por poda); 4 malditos con debuff y su legendario de redención; Fénix, Huerto eterno (3 legendarios a la vez), Cinco inviernos (1825). Títulos con juego de palabras de jardín; descripción honesta.

- [ ] Tests: `CadaHabitoTieneAlMenosDiez`; `IdsUnicos`; `NivelesAscendentesYDeUnoATres`; `Visibles_SinElegir_NoMuestraDelHabito`; `Visibles_OcultosNoAparecenHastaGanarse`; `Evaluador_SubeNivelesEnOrdenYNoRepite`; `Malditos_TienenDebuffYRedencion`.
- [ ] Implementar; tests verdes; commit.

### Task 5: GardenService + SQLite v2 (Core)

**Files:** Modify `GardenService.cs`, `IGardenStore.cs`, `InMemoryGardenStore.cs`, `SqliteGardenStore.cs`; Tests `GardenServiceTests.cs`, `SqliteGardenStoreTests.cs`

**Produces:**
- `IGardenStore`: + `IReadOnlyDictionary<string,int> Estadisticas()`, `void GuardarEstadistica(string clave, int valor)`, `IReadOnlyList<NivelLogro> NivelesLogro()`, `void AgregarNivel(NivelLogro)`.
- `VistaArbol(Guid Id, MalHabito Categoria, string Especie, HabitType Tipo, GrowthStage Etapa, int Dias, bool Plaga, bool Raices, DateOnly? FinCuidado, int Strikes, int StrikesAbandono, bool PideCheckInHoy, bool EsCheatDayHoy, bool RegadoHoy, bool PodadoHoy, bool TratadoHoy, bool PuedeBorron, bool Pausado, MiniEvento MiniEvento, bool PideAsistencia, bool AsistioHoy, IReadOnlyList<DayOfWeek> DiasDeCheat)`
- `GardenService`: `CrearHabito(MalHabito, string especie, HabitType, IEnumerable<DayOfWeek>, int diasPrevios = 0, bool grupo = false, bool terapia = false)`; `IReadOnlyList<string> EspeciesLibres()`; `IReadOnlyList<MalHabito> HabitosDisponibles()`; `ResultadoCheckIn CheckIn(Guid, CheckInResult)` → `(CheckInOutcome, int NuevosLogros)`; `int Refrescar()` (corre `Avanzar` en todos, evalúa logros, devuelve cantidad nueva); `RegistrarAsistencia(Guid)`; `ResolverMiniEvento(Guid)`; `Borron(Guid)`; `RegistrarJuego(Guid, MiniJuegoTipo, int piezas)`; `Estante()` → `IReadOnlyList<EntradaEstante(LogroDef Def, int Nivel, DateOnly? Fecha, int Progreso, string? Arbol)>`; `IReadOnlySet<Debuff> Debuffs()`; `IReadOnlyList<Aviso> AvisosPendientes()` (para notificaciones: etapas nuevas, mini-eventos, plaga por abandono) con marca de "ya avisado" en estadísticas.
- `enum MiniJuegoTipo { Regar, Podar, Plaga, Raices, MiniEvento }`; `enum Debuff { MasBichos, TierraSeca, MasRamas, RaicesTercas }`.
- Migración v1→v2: columnas nuevas con default; `Categoria = DesdeNombreLibre(Nombre)`; `DiasDeCrecimiento = hoy - InicioRacha`; `StrikesDeclarados = Cicatrices`; tabla `Estadisticas`, `NivelesLogro`; logro v1 FrutaLegendaria → `general.legendaria` nivel 1, LeyendaCaida → `maldito.leyenda_caida`.

- [ ] Tests: `CrearHabito_EspecieUsada_Lanza`; `CrearHabito_HabitoRepetido_Lanza` (salvo Otros); `CrearHabito_ModoNoPermitido_Lanza`; `CrearHabito_DiasPrevios_NaceCrecido`; `Vista_NoExponeNombreDeHabitoFueraDeCategoria`; `Refrescar_DosVeces_NoDuplicaLogros`; `Estante_HabitoAparecеAlPlantar` ; `Sqlite_MigraV1` (crea base v1 a mano con nombre "Cigarro", especie repetida, logro LeyendaCaida ⇒ abre en v2, categoría Cigarro, nivel maldito presente).
- [ ] Implementar; tests verdes; commit.

### Task 6: App — plantar, árbol anónimo, mini-eventos, borrón, debuffs

**Files:** Modify `Paginas/NuevoHabitoPage.cs`, `Paginas/ArbolPage.cs`, `Paginas/JardinPage.cs`, `Dibujo/Arbol.cs` (4 especies nuevas y color), `Dibujo/Escena.cs` (contadores por pieza, debuffs, mini-evento), `Tema.cs` (nombres de hábito).

- Plantar: `Picker` de hábitos disponibles (orden del catálogo), modo solo si ambos permitidos, aviso médico en recuadro si existe, cuestionario opcional (tiempo previo: número + unidad días/meses/años → días con 30/365; casillas grupo/terapia), chips solo con `EspeciesLibres()`.
- Árbol: título = especie; "¿Cómo te fue hoy?"; botón "Fui a mi grupo / sesión" si `PideAsistencia`; mini-evento con su juego corto (Caracol → tocar 1–2 bichos; RamaSeca → 1 corte; TierraReseca → regar 0.6 s); borrón con texto de remisión temprana; tratamiento: plaga y luego raíces según flags.
- Logros en pantalla: solo "🏆 ¡Desbloqueaste un logro! Abre tu estante para verlo."
- [ ] `dotnet build -f net10.0-android` sin errores; commit.

### Task 7: App — estante con bloqueo

**Files:** Create `Platforms/Android/Bloqueo.cs` (`static Task<bool> Bloqueo.PedirAsync()`), Rewrite `Paginas/LogrosPage.cs` (estante: secciones General / tus árboles (habito + especie) / descubiertos, estrellas ★☆☆, progreso al siguiente nivel; malditos con placa y su debuff; legendarios dorados).
- Botón del jardín "🏆" abre `LogrosPage` solo si `PedirAsync()` es true; si el teléfono no tiene bloqueo, se avisa y se abre.
- [ ] Build; commit.

### Task 8: App — notificaciones

**Files:** Rename/rewrite `Platforms/Android/AvisoCheatDay.cs` → `Avisos.cs` (canal "Tu jardín"; alarma 9:00 y 20:00; receptor llama `Refrescar()` + `AvisosPendientes()`).
- 9:00: días libres ("☀️ Hoy tu naranjo descansa. Disfruta con medida."), etapas nuevas ("🌸 ¡Tu durazno floreció!", "🍏 ¡Tu manzano dio sus primeros frutos!", "🍎 … frutos maduros", "✨ … fruta legendaria"), mini-eventos, plaga por abandono ("🐛 ¡Oh no! Algo le pasa a tu cerezo.").
- 20:00: si falta check-in, un texto rotativo ("🌧️ Tu jardín tiene sed. ¿Pasas a regarlo?", "🌱 Tus árboles te extrañan un poquito.", "🪴 Una vuelta rápida por el jardín y listo.").
- Logro nuevo (en receptor o en app en segundo plano): exacto el texto de Global Constraints.
- [ ] Build; commit.

### Task 9: Prueba en emulador, CLAUDE.md y push

- [ ] `dotnet test` completo verde.
- [ ] Emulador `Jardin_API36` con modo acelerado: plantar 3 hábitos (lista, especie única, aviso médico), check-ins, strike doble (8 días), abandono hasta mini-evento y plaga, estante con bloqueo, notificación genérica.
- [ ] Actualizar CLAUDE.md (reglas nuevas reemplazan las viejas); commit; `git push -u origin fase2/jardin-secreto`.
