namespace JardinDeHabitos.Core;

/// <summary>Todo lo que se guarda de un hábito: el hábito, sus strikes y el cuidado diario.</summary>
public sealed class HabitRecord
{
    public required Habit Habito { get; init; }
    public StrikeState Estado { get; init; } = new();
    public DateOnly? UltimoRiego { get; set; }
    public DateOnly? UltimaPoda { get; set; }
    public DateOnly? UltimoTratamiento { get; set; }

    /// <summary>Posición en la cuadrícula del jardín.</summary>
    public int Orden { get; set; }
}
