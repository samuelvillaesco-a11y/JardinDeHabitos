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
        ExpirarPenalizacion(estado, hoy);

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
                Morir(habito, estado, hoy);
                return Resultado(estado, murio: true);
        }

        return Resultado(estado, murio: false);
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

    private static CheckInOutcome Resultado(StrikeState estado, bool murio) =>
        new(murio, estado.NivelDePenalizacion, estado.FechaFinDePenalizacionActiva, LeyendaCaida: false);

    private static PenaltyLevel Max(PenaltyLevel a, PenaltyLevel b) => a >= b ? a : b;

    private static DateOnly Max(DateOnly? a, DateOnly b) => a is { } x && x > b ? x : b;
}
