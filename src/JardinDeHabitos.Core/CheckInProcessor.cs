namespace JardinDeHabitos.Core;

/// <summary>
/// Reglas del árbol: check-in diario, strikes acumulados (declarados o por abandono),
/// cuidados de plaga y raíces, muerte y borrón y cuenta nueva.
/// </summary>
public class CheckInProcessor(GrowthStageCalculator calculadora)
{
    public const int DiasPlaga = 3;
    public const int DiasRaicesTrasPlaga = 5;
    public const int DiasCuidadoStrike2 = DiasPlaga + DiasRaicesTrasPlaga;
    public const int StrikesParaMorir = 3;

    /// <summary>DSM-5: remisión temprana a partir de 3 meses sin criterios.</summary>
    public const int DiasParaBorron = 90;

    /// <summary>Un año limpio sin strikes: requisito de la Fruta del Fénix.</summary>
    public const int DiasFenix = 365;

    public CheckInOutcome Procesar(Habit habito, StrikeState estado, CheckInResult resultado)
    {
        var hoy = calculadora.Hoy;

        if (estado.FechaUltimoCheckIn is { } ultimo && hoy <= ultimo)
            throw new InvalidOperationException("Ya hubo check-in hoy (o el reloj retrocedió) para este árbol.");
        if (resultado == CheckInResult.CheatDay && habito.Tipo != HabitType.Moderable)
            throw new InvalidOperationException("Los días libres solo aplican a hábitos que se moderan.");
        var esDiaDeCheat = CheatDaySchedule.EsDiaDeCheat(habito, hoy);
        if (resultado == CheckInResult.CheatDay && !esDiaDeCheat)
            throw new InvalidOperationException("Hoy no es día libre para este árbol.");
        if (resultado != CheckInResult.CheatDay && esDiaDeCheat)
            throw new InvalidOperationException("Hoy es día libre: no se pide check-in.");

        estado.FechaUltimoCheckIn = hoy;
        estado.DiasNormalesSinCheckIn = 0;

        switch (resultado)
        {
            case CheckInResult.CheatDay:
                // El día libre crece al cerrarse (avance diario), no aquí.
                return new CheckInOutcome(false, 0, false);
            case CheckInResult.Cumplido:
                if (!EnCuidado(estado, hoy)) habito.DiasDeCrecimiento++;
                return new CheckInOutcome(false, 0, false);
            default:
                estado.StrikesDeclarados++;
                return AplicarStrike(habito, estado, hoy, estado.StrikesDeclarados, estado.StrikesDeclarados - 1 + estado.StrikesAbandono);
        }
    }

    /// <summary>
    /// Aplica el strike número <paramref name="numero"/> de su contador (ya incrementado).
    /// 1 = plaga 3 días; 2 = plaga + raíces (8 días, o fin de plaga + 5 si la plaga sigue); 3 = muerte.
    /// </summary>
    public static CheckInOutcome AplicarStrike(Habit habito, StrikeState estado, DateOnly dia, int numero, int strikesPrevios)
    {
        if (numero >= StrikesParaMorir)
        {
            int antes = habito.DiasDeCrecimiento;
            Morir(habito, estado, dia);
            return new CheckInOutcome(true, antes, false);
        }

        if (strikesPrevios == 0 && habito.DiasDeCrecimiento - estado.DiasSinStrikeBase >= DiasFenix)
            estado.FenixPendiente = true;
        estado.FechaUltimoStrike = dia;
        estado.DiasSinStrikeBase = habito.DiasDeCrecimiento;

        if (numero == 1)
        {
            estado.FinPlaga = Max(estado.FinPlaga, dia.AddDays(DiasPlaga));
        }
        else if (estado.FinPlaga is { } finPlaga && finPlaga > dia)
        {
            // Cae encima de la plaga en curso: solo se agregan las raíces (los días se suman).
            estado.FinRaices = Max(estado.FinRaices, finPlaga.AddDays(DiasRaicesTrasPlaga));
        }
        else
        {
            estado.FinPlaga = dia.AddDays(DiasPlaga);
            estado.FinRaices = Max(estado.FinRaices, dia.AddDays(DiasCuidadoStrike2));
        }
        return new CheckInOutcome(false, 0, true);
    }

    public static bool TienePlaga(StrikeState e, DateOnly hoy) => e.FinPlaga is { } f && hoy < f;

    public static bool TieneRaices(StrikeState e, DateOnly hoy) => e.FinRaices is { } f && hoy < f;

    public static bool EnCuidado(StrikeState e, DateOnly hoy) => TienePlaga(e, hoy) || TieneRaices(e, hoy);

    public static DateOnly? FinCuidado(StrikeState e, DateOnly hoy) =>
        !EnCuidado(e, hoy) ? null : Max(e.FinPlaga, e.FinRaices ?? DateOnly.MinValue);


    /// <summary>Borrón y cuenta nueva: opcional, tras 90 días sin ningún strike.</summary>
    public bool PuedeBorron(StrikeState e) =>
        e.StrikesDeclarados + e.StrikesAbandono > 0
        && e.FechaUltimoStrike is { } f
        && calculadora.Hoy.DayNumber - f.DayNumber >= DiasParaBorron;

    public void Borron(StrikeState e)
    {
        if (!PuedeBorron(e))
            throw new InvalidOperationException("El borrón se ofrece tras 90 días sin strikes.");
        e.StrikesDeclarados = 0;
        e.StrikesAbandono = 0;
    }

    private static void Morir(Habit habito, StrikeState estado, DateOnly dia)
    {
        estado.StrikesDeclarados = 0;
        estado.StrikesAbandono = 0;
        estado.FinPlaga = null;
        estado.FinRaices = null;
        estado.FenixPendiente = false;
        estado.DiasSinStrikeBase = 0;
        estado.FechaUltimoStrike = null;
        estado.MiniEvento = MiniEvento.Ninguno;
        habito.DiasDeCrecimiento = 0;
        habito.FechaDeInicioRachaActual = dia;
    }

    private static DateOnly Max(DateOnly? a, DateOnly b) => a is { } x && x > b ? x : b;
}
