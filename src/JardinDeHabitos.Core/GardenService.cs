namespace JardinDeHabitos.Core;

/// <summary>Lo que la UI necesita saber de un árbol, ya calculado para "hoy".</summary>
public sealed record VistaArbol(
    Guid Id,
    string Nombre,
    string Especie,
    HabitType Tipo,
    GrowthStage Etapa,
    int Dias,
    PenaltyLevel Penalizacion,
    DateOnly? FinPenalizacion,
    int Racha,
    int Cicatrices,
    bool PideCheckInHoy,
    bool EsCheatDayHoy,
    bool RegadoHoy,
    bool PodadoHoy,
    bool TratadoHoy,
    bool PuedeBorrarCicatrices,
    IReadOnlyList<DayOfWeek> DiasDeCheat);

public sealed record ResultadoCheckIn(CheckInOutcome Outcome, IReadOnlyList<Logro> NuevosLogros);

/// <summary>Punto de entrada de la app: orquesta reglas + persistencia.</summary>
public sealed class GardenService(IGardenStore store, GrowthStageCalculator calculadora)
{
    public const int MaxHabitos = 10;

    private readonly CheckInProcessor _procesador = new(calculadora);

    public DateOnly Hoy => calculadora.Hoy;

    public IReadOnlyList<VistaArbol> Jardin() => [.. store.Cargar().Select(Vista)];

    public VistaArbol Arbol(Guid id) => Vista(Buscar(id));

    public VistaArbol CrearHabito(string nombre, string especie, HabitType tipo, IEnumerable<DayOfWeek> diasDeCheat)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El hábito necesita un nombre.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(especie))
            throw new ArgumentException("El árbol necesita una especie.", nameof(especie));

        var existentes = store.Cargar();
        if (existentes.Count >= MaxHabitos)
            throw new InvalidOperationException($"El jardín ya tiene {MaxHabitos} árboles.");

        var habito = new Habit
        {
            Nombre = nombre.Trim(),
            EspecieDeArbol = especie.Trim(),
            Tipo = tipo,
            FechaDeInicioRachaActual = Hoy,
            DiasDeCheat = [.. diasDeCheat],
        };
        CheatDaySchedule.Validar(habito);

        var registro = new HabitRecord
        {
            Habito = habito,
            Orden = existentes.Count == 0 ? 0 : existentes.Max(r => r.Orden) + 1,
        };
        store.Guardar(registro);
        return Vista(registro);
    }

    public ResultadoCheckIn CheckIn(Guid id, CheckInResult resultado)
    {
        var r = Buscar(id);
        var outcome = _procesador.Procesar(r.Habito, r.Estado, resultado);

        // Árbol y logros juntos: si el árbol legendario muere sin guardar LeyendaCaida, el logro se perdería para siempre.
        var nuevos = new List<Logro>();
        store.EnTransaccion(() =>
        {
            store.Guardar(r);
            if (outcome.LeyendaCaida)
            {
                // Un árbol legendario que muere también cuenta como haber llegado a legendario.
                Desbloquear(Logro.FrutaLegendaria, r.Habito.Nombre, nuevos);
                Desbloquear(Logro.LeyendaCaida, r.Habito.Nombre, nuevos);
            }
        });
        return new ResultadoCheckIn(outcome, nuevos);
    }

    /// <summary>Revisa logros que dependen solo del paso del tiempo. Devuelve los nuevos.</summary>
    public IReadOnlyList<Logro> Refrescar()
    {
        var nuevos = new List<Logro>();
        foreach (var r in store.Cargar())
        {
            if (calculadora.CalcularEtapa(r.Habito) == GrowthStage.FrutaLegendaria)
                Desbloquear(Logro.FrutaLegendaria, r.Habito.Nombre, nuevos);
        }
        return nuevos;
    }

    public void Regar(Guid id) => Modificar(id, r => r.UltimoRiego = Hoy);

    public void Podar(Guid id) => Modificar(id, r => r.UltimaPoda = Hoy);

    public void RegistrarTratamiento(Guid id) => Modificar(id, r =>
    {
        if (PenalizacionVigente(r) == PenaltyLevel.Ninguna)
            throw new InvalidOperationException("Este árbol no necesita tratamiento.");
        r.UltimoTratamiento = Hoy;
    });

    public void BorrarCicatrices(Guid id) => Modificar(id, r => _procesador.BorrarCicatrices(r.Habito, r.Estado));

    public void EditarDiasDeCheat(Guid id, IEnumerable<DayOfWeek> dias) => Modificar(id, r =>
    {
        if (!CheatDaySchedule.PuedeEditar(Hoy))
            throw new InvalidOperationException("Los cheat days se editan los sábados.");

        var nuevo = new Habit
        {
            Nombre = r.Habito.Nombre,
            EspecieDeArbol = r.Habito.EspecieDeArbol,
            Tipo = r.Habito.Tipo,
            DiasDeCheat = [.. dias],
        };
        CheatDaySchedule.Validar(nuevo);

        // La plantilla que rige hoy se conserva hasta mañana (domingo).
        r.Habito.DiasDeCheatAnteriores = [.. CheatDaySchedule.PlantillaVigente(r.Habito, Hoy)];
        r.Habito.DiasDeCheat = nuevo.DiasDeCheat;
        r.Habito.CambioDeCheatDesde = Hoy.AddDays(1);
    });

    public void EliminarHabito(Guid id) => store.Eliminar(Buscar(id).Habito.Id);

    public IReadOnlyList<LogroDesbloqueado> LogrosDesbloqueados() => store.Logros();

    /// <summary>Logros visibles aún no conseguidos. Los secretos nunca aparecen aquí.</summary>
    public IReadOnlyList<Logro> LogrosPorDesbloquear()
    {
        var tenidos = store.Logros().Select(l => l.Logro).ToHashSet();
        return [.. LogroInfo.Todos.Where(i => !i.EsSecreto && !tenidos.Contains(i.Logro)).Select(i => i.Logro)];
    }

    private void Desbloquear(Logro logro, string habito, List<Logro> nuevos)
    {
        if (store.Logros().Any(l => l.Logro == logro)) return;
        store.AgregarLogro(new LogroDesbloqueado(logro, Hoy, habito));
        nuevos.Add(logro);
    }

    private HabitRecord Buscar(Guid id) =>
        store.Cargar().FirstOrDefault(r => r.Habito.Id == id)
        ?? throw new KeyNotFoundException("No existe ese hábito.");

    private void Modificar(Guid id, Action<HabitRecord> cambio)
    {
        var r = Buscar(id);
        cambio(r);
        store.Guardar(r);
    }

    private PenaltyLevel PenalizacionVigente(HabitRecord r) =>
        r.Estado.FechaFinDePenalizacionActiva is { } fin && Hoy < fin ? r.Estado.NivelDePenalizacion : PenaltyLevel.Ninguna;

    private VistaArbol Vista(HabitRecord r)
    {
        var hoy = Hoy;
        var penal = PenalizacionVigente(r);
        var esCheat = CheatDaySchedule.EsDiaDeCheat(r.Habito, hoy);
        return new VistaArbol(
            r.Habito.Id,
            r.Habito.Nombre,
            r.Habito.EspecieDeArbol,
            r.Habito.Tipo,
            calculadora.CalcularEtapa(r.Habito),
            calculadora.DiasTranscurridos(r.Habito),
            penal,
            penal == PenaltyLevel.Ninguna ? null : r.Estado.FechaFinDePenalizacionActiva,
            r.Estado.RachaConsecutivaDeDiasRotos,
            r.Estado.StrikesTotalesDelArbolActual,
            PideCheckInHoy: !esCheat && r.Estado.FechaUltimoCheckIn != hoy,
            EsCheatDayHoy: esCheat,
            RegadoHoy: r.UltimoRiego == hoy,
            PodadoHoy: r.UltimaPoda == hoy,
            TratadoHoy: r.UltimoTratamiento == hoy,
            PuedeBorrarCicatrices: _procesador.PuedeBorrarCicatrices(r.Habito, r.Estado),
            DiasDeCheat: [.. r.Habito.DiasDeCheat]);
    }
}
