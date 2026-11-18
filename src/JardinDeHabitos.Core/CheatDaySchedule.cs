namespace JardinDeHabitos.Core;

/// <summary>
/// Calendario semanal de cheat days. Solo hábitos Moderable, 1-2 días fijos.
/// La plantilla se reutiliza sola cada semana; se edita los sábados.
/// </summary>
public static class CheatDaySchedule
{
    public const int MaxDiasPorSemana = 2;

    public static bool EsDiaDeCheat(Habit habito, DateOnly dia) =>
        habito.Tipo == HabitType.Moderable && habito.DiasDeCheat.Contains(dia.DayOfWeek);

    public static void Validar(Habit habito)
    {
        if (habito.Tipo == HabitType.AbstinenciaTotal && habito.DiasDeCheat.Count > 0)
            throw new ArgumentException("Los hábitos de abstinencia total no tienen cheat days.");
        if (habito.DiasDeCheat.Count > MaxDiasPorSemana)
            throw new ArgumentException($"Máximo {MaxDiasPorSemana} cheat days por semana.");
        if (habito.DiasDeCheat.Distinct().Count() != habito.DiasDeCheat.Count)
            throw new ArgumentException("Días de cheat repetidos.");
    }

    /// <summary>El calendario se edita cada sábado (o al crear el hábito).</summary>
    public static bool PuedeEditar(DateOnly hoy) => hoy.DayOfWeek == DayOfWeek.Saturday;

    /// <summary>¿Hay algún día normal (no cheat) estrictamente entre las dos fechas?</summary>
    public static bool HayDiaNormalEntre(Habit habito, DateOnly desde, DateOnly hasta)
    {
        // Con máx. 2 cheat days por semana, 3+ días intermedios siempre incluyen uno normal.
        if (hasta.DayNumber - desde.DayNumber - 1 > MaxDiasPorSemana) return true;
        for (var d = desde.AddDays(1); d < hasta; d = d.AddDays(1))
        {
            if (!EsDiaDeCheat(habito, d)) return true;
        }
        return false;
    }
}
