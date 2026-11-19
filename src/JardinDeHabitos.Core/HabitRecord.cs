namespace JardinDeHabitos.Core;

/// <summary>Todo lo que se guarda de un árbol: el hábito, sus strikes y el cuidado diario.</summary>
public sealed class HabitRecord
{
    public required Habit Habito { get; init; }
    public StrikeState Estado { get; init; } = new();
    public DateOnly? UltimoRiego { get; set; }
    public DateOnly? UltimaPoda { get; set; }
    public DateOnly? UltimoTratamiento { get; set; }
    public DateOnly? UltimaAsistencia { get; set; }
    public DateOnly? UltimoCumplido { get; set; }

    /// <summary>Un strike declarado abrió un cuidado que aún no se cierra.</summary>
    public bool CuidadoPendiente { get; set; }

    /// <summary>Días tratados durante el cuidado actual.</summary>
    public int TratamientosEnCuidado { get; set; }

    /// <summary>Días seguidos con check-in de este árbol.</summary>
    public int ConstanciaActual { get; set; }

    /// <summary>Posición en la cuadrícula del jardín.</summary>
    public int Orden { get; set; }
}
