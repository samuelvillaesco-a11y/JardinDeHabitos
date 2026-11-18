namespace JardinDeHabitos.Core;

/// <summary>
/// Calcula la etapa de crecimiento de un árbol a partir de los días transcurridos
/// desde el inicio de la racha actual. La etapa nunca se guarda: siempre se deriva
/// del tiempo real para que no se desincronice.
/// </summary>
public class GrowthStageCalculator
{
    /// <summary>
    /// Día mínimo (desde el inicio de la racha) para alcanzar cada etapa, en orden ascendente.
    /// Días fijos, no meses de calendario: 1 mes = 30 días, 6 meses = 180, 1 año = 365.
    /// </summary>
    public static IReadOnlyList<(int DiaMinimo, GrowthStage Etapa)> Umbrales { get; } =
    [
        (0, GrowthStage.Semilla),
        (1, GrowthStage.Brote),
        (3, GrowthStage.Arbusto),
        (5, GrowthStage.ArbolJoven),
        (7, GrowthStage.Flor),
        (30, GrowthStage.FrutaInmadura),
        (180, GrowthStage.FrutaComestible),
        (365, GrowthStage.FrutaLegendaria),
    ];

    private readonly TimeProvider _reloj;

    /// <param name="reloj">
    /// Fuente de tiempo. Por defecto el reloj del sistema; para pruebas se puede pasar
    /// un <see cref="RelojAcelerado"/>.
    /// </param>
    public GrowthStageCalculator(TimeProvider? reloj = null)
    {
        _reloj = reloj ?? TimeProvider.System;
    }

    /// <summary>Fecha local "de hoy" según el reloj configurado.</summary>
    public DateOnly Hoy => DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);

    public int DiasTranscurridos(Habit habito) =>
        DiasTranscurridos(habito.FechaDeInicioRachaActual, Hoy);

    public GrowthStage CalcularEtapa(Habit habito) =>
        EtapaParaDias(DiasTranscurridos(habito));

    /// <summary>Días completos entre el inicio de la racha y hoy (nunca negativo).</summary>
    public static int DiasTranscurridos(DateOnly inicioRacha, DateOnly hoy) =>
        Math.Max(0, hoy.DayNumber - inicioRacha.DayNumber);

    public static GrowthStage EtapaParaDias(int dias)
    {
        var etapa = GrowthStage.Semilla;
        foreach (var (diaMinimo, candidata) in Umbrales)
        {
            if (dias < diaMinimo) break;
            etapa = candidata;
        }
        return etapa;
    }
}
