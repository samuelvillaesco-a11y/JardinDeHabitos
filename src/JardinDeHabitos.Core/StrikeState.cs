namespace JardinDeHabitos.Core;

/// <summary>Mini-eventos de emergencia cuando el jardín lleva 7+ días sin visita.</summary>
public enum MiniEvento
{
    Ninguno,
    Caracol,
    RamaSeca,
    TierraReseca,
}

public class StrikeState
{
    /// <summary>Strikes por "Lo rompí". Se acumulan; 3 = el árbol muere.</summary>
    public int StrikesDeclarados { get; set; }

    /// <summary>Strikes por abandono (14/21/28 días sin check-in). Contador aparte, mismo efecto.</summary>
    public int StrikesAbandono { get; set; }

    /// <summary>Fin exclusivo de la plaga (activa mientras hoy &lt; fin).</summary>
    public DateOnly? FinPlaga { get; set; }

    /// <summary>Fin exclusivo del cuidado de raíces.</summary>
    public DateOnly? FinRaices { get; set; }

    public DateOnly? FechaUltimoCheckIn { get; set; }
    public DateOnly? FechaUltimoStrike { get; set; }

    /// <summary>Días de crecimiento que tenía el árbol en su último strike (o al plantar).</summary>
    public int DiasSinStrikeBase { get; set; }

    /// <summary>Primer strike tras 365 días limpios: si el árbol sana, es la Fruta del Fénix.</summary>
    public bool FenixPendiente { get; set; }

    /// <summary>Días normales (no libres) seguidos sin check-in, ya cerrados.</summary>
    public int DiasNormalesSinCheckIn { get; set; }

    /// <summary>Último día cerrado que ya revisó el avance diario.</summary>
    public DateOnly UltimoDiaEvaluado { get; set; }

    public MiniEvento MiniEvento { get; set; }
}
