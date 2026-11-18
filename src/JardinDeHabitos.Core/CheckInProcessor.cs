namespace JardinDeHabitos.Core;

/// <summary>
/// Procesa el check-in diario: actualiza la racha de días rotos, las cicatrices,
/// la penalización activa y decide si el árbol muere.
/// </summary>
public class CheckInProcessor(GrowthStageCalculator calculadora)
{
    private const int DiasCuidadoStrike1 = 2;
    private const int DiasCuidadoStrike2 = 5;
    private const int RotosParaMorir = 3;

    public CheckInOutcome Procesar(Habit habito, StrikeState estado, CheckInResult resultado)
    {
        var hoy = calculadora.Hoy;

        if (estado.FechaUltimoCheckIn is { } ultimo && hoy <= ultimo)
            throw new InvalidOperationException("Ya hubo check-in hoy (o el reloj retrocedió) para este hábito.");
        if (resultado == CheckInResult.CheatDay && habito.Tipo != HabitType.Moderable)
            throw new InvalidOperationException("Los cheat days solo aplican a hábitos Moderable.");
        var esDiaDeCheat = CheatDaySchedule.EsDiaDeCheat(habito, hoy);
        if (resultado == CheckInResult.CheatDay && !esDiaDeCheat)
            throw new InvalidOperationException("Hoy no es cheat day para este hábito.");
        if (resultado != CheckInResult.CheatDay && esDiaDeCheat)
            throw new InvalidOperationException("Hoy es cheat day: no se pide check-in.");

        if (estado.FechaUltimoCheckIn is { } previo && CheatDaySchedule.HayDiaNormalEntre(habito, previo, hoy))
        {
            // Hubo un día normal sin check-in: los rotos ya no son "seguidos".
            // Los cheat days no cuentan como hueco.
            estado.RachaConsecutivaDeDiasRotos = 0;
        }

        estado.FechaUltimoCheckIn = hoy;
        ExpirarPenalizacion(estado, hoy);

        if (resultado == CheckInResult.CheatDay)
        {
            // Neutral: ni suma ni reinicia la racha.
            return Resultado(estado, murio: false);
        }

        if (resultado == CheckInResult.Cumplido)
        {
            // La penalización en curso se completa igual.
            estado.RachaConsecutivaDeDiasRotos = 0;
            return Resultado(estado, murio: false);
        }

        estado.RachaConsecutivaDeDiasRotos++;
        estado.StrikesTotalesDelArbolActual++;

        switch (estado.RachaConsecutivaDeDiasRotos)
        {
            case 1:
                estado.NivelDePenalizacion = Max(estado.NivelDePenalizacion, PenaltyLevel.Strike1);
                estado.FechaFinDePenalizacionActiva = Max(estado.FechaFinDePenalizacionActiva, hoy.AddDays(DiasCuidadoStrike1));
                break;
            case 2:
                estado.NivelDePenalizacion = PenaltyLevel.Strike2;
                estado.FechaFinDePenalizacionActiva = (estado.FechaFinDePenalizacionActiva ?? hoy).AddDays(DiasCuidadoStrike2);
                break;
            case >= RotosParaMorir:
                // La etapa solo baja al morir: se mide antes de reiniciar la racha.
                var eraLegendaria = calculadora.CalcularEtapa(habito) == GrowthStage.FrutaLegendaria;
                Morir(habito, estado, hoy);
                return Resultado(estado, murio: true, leyendaCaida: eraLegendaria);
        }

        return Resultado(estado, murio: false);
    }

    /// <summary>
    /// Opción (nunca automática) de borrar las cicatrices cuando el árbol llega a 6 meses.
    /// </summary>
    public bool PuedeBorrarCicatrices(Habit habito, StrikeState estado) =>
        estado.StrikesTotalesDelArbolActual > 0
        && calculadora.CalcularEtapa(habito) >= GrowthStage.FrutaComestible;

    public void BorrarCicatrices(Habit habito, StrikeState estado)
    {
        if (!PuedeBorrarCicatrices(habito, estado))
            throw new InvalidOperationException("Las cicatrices solo se pueden borrar a partir de los 6 meses.");

        estado.StrikesTotalesDelArbolActual = 0;
    }

    /// <summary>La fecha fin es exclusiva: la penalización vale mientras hoy &lt; fin.</summary>
    private static void ExpirarPenalizacion(StrikeState estado, DateOnly hoy)
    {
        if (estado.FechaFinDePenalizacionActiva is { } fin && fin <= hoy)
        {
            estado.NivelDePenalizacion = PenaltyLevel.Ninguna;
            estado.FechaFinDePenalizacionActiva = null;
        }
    }

    private static void Morir(Habit habito, StrikeState estado, DateOnly hoy)
    {
        estado.RachaConsecutivaDeDiasRotos = 0;
        estado.StrikesTotalesDelArbolActual = 0;
        estado.NivelDePenalizacion = PenaltyLevel.Ninguna;
        estado.FechaFinDePenalizacionActiva = null;
        habito.FechaDeInicioRachaActual = hoy;
    }

    private static CheckInOutcome Resultado(StrikeState estado, bool murio, bool leyendaCaida = false) =>
        new(murio, estado.NivelDePenalizacion, estado.FechaFinDePenalizacionActiva, leyendaCaida);

    private static PenaltyLevel Max(PenaltyLevel a, PenaltyLevel b) => a >= b ? a : b;

    private static DateOnly Max(DateOnly? a, DateOnly b) => a is { } x && x > b ? x : b;
}
