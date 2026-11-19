namespace JardinDeHabitos.Core;

public class Habit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required MalHabito Categoria { get; set; }
    public required string EspecieDeArbol { get; set; }
    public required HabitType Tipo { get; set; }

    /// <summary>Día en que se plantó la semilla actual (al plantar o al morir).</summary>
    public DateOnly FechaDeInicioRachaActual { get; set; }

    /// <summary>
    /// Días que el árbol de verdad creció: solo días cumplidos (o libres) estando sano.
    /// Sin check-in, o en cuidado, el árbol se pausa.
    /// </summary>
    public int DiasDeCrecimiento { get; set; }

    public bool ApoyoGrupo { get; set; }
    public bool ApoyoTerapia { get; set; }

    public List<DayOfWeek> DiasDeCheat { get; set; } = [];

    /// <summary>Plantilla que rige antes de <see cref="CambioDeCheatDesde"/> (edición del sábado).</summary>
    public List<DayOfWeek> DiasDeCheatAnteriores { get; set; } = [];
    public DateOnly? CambioDeCheatDesde { get; set; }
}
