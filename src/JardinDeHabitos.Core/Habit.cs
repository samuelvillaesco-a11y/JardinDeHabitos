namespace JardinDeHabitos.Core;

public class Habit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nombre { get; set; }
    public required string EspecieDeArbol { get; set; }
    public required HabitType Tipo { get; set; }
    public DateOnly FechaDeInicioRachaActual { get; set; }
    public List<DayOfWeek> DiasDeCheat { get; set; } = [];
}
