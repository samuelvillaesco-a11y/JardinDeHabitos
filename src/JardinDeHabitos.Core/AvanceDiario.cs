namespace JardinDeHabitos.Core;

public enum TipoEvento
{
    CrecioPorDiaLibre,
    MiniEvento,
    StrikeAbandono,
    MurioPorAbandono,
}

/// <param name="Valor">Para StrikeAbandono: número de strike; para MurioPorAbandono: días que tenía el árbol.</param>
public sealed record EventoDelDia(DateOnly Fecha, TipoEvento Tipo, int Valor = 0);

/// <summary>
/// Revisa los días ya cerrados desde la última vez: días libres que crecen, días sin check-in
/// (pausa, mini-evento al 7.º, strikes por abandono al 14.º, 21.º y 28.º). Idempotente:
/// cada día se evalúa una sola vez. Debe correr antes de cualquier check-in.
/// </summary>
public static class AvanceDiario
{
    public const int DiasParaMiniEvento = 7;
    public const int DiasPorStrikeAbandono = 7;
    public const int PrimerStrikeAbandono = 14;

    public static IReadOnlyList<EventoDelDia> Avanzar(Habit habito, StrikeState estado, DateOnly hoy)
    {
        var eventos = new List<EventoDelDia>();
        for (var d = estado.UltimoDiaEvaluado.AddDays(1); d < hoy; d = d.AddDays(1))
        {
            if (d < habito.FechaDeInicioRachaActual) continue;

            if (CheatDaySchedule.EsDiaDeCheat(habito, d))
            {
                // El día libre crece solo si el jardín está al día y el árbol sano.
                if (estado.DiasNormalesSinCheckIn == 0 && !CheckInProcessor.EnCuidado(estado, d))
                {
                    habito.DiasDeCrecimiento++;
                    eventos.Add(new(d, TipoEvento.CrecioPorDiaLibre));
                }
                continue;
            }
            if (estado.FechaUltimoCheckIn == d) continue;

            int faltas = ++estado.DiasNormalesSinCheckIn;
            if (faltas == DiasParaMiniEvento && estado.MiniEvento == MiniEvento.Ninguno)
            {
                estado.MiniEvento = (MiniEvento)(1 + d.DayNumber % 3);
                eventos.Add(new(d, TipoEvento.MiniEvento));
            }

            if (faltas >= PrimerStrikeAbandono && (faltas - PrimerStrikeAbandono) % DiasPorStrikeAbandono == 0)
            {
                // El cuidado empieza el día siguiente a la falta que completa el plazo.
                var dia = d.AddDays(1);
                int previos = estado.StrikesDeclarados + estado.StrikesAbandono;
                estado.StrikesAbandono++;
                var r = CheckInProcessor.AplicarStrike(habito, estado, dia, estado.StrikesAbandono, previos);
                if (r.ArbolMurio)
                {
                    estado.DiasNormalesSinCheckIn = 0;
                    eventos.Add(new(dia, TipoEvento.MurioPorAbandono, r.DiasAntesDeMorir));
                }
                else
                {
                    eventos.Add(new(dia, TipoEvento.StrikeAbandono, estado.StrikesAbandono));
                }
            }
        }
        if (hoy.AddDays(-1) > estado.UltimoDiaEvaluado) estado.UltimoDiaEvaluado = hoy.AddDays(-1);
        return eventos;
    }
}
