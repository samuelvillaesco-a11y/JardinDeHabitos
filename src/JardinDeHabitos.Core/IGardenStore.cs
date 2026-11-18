namespace JardinDeHabitos.Core;

public interface IGardenStore
{
    IReadOnlyList<HabitRecord> Cargar();
    void Guardar(HabitRecord registro);
    void Eliminar(Guid habitoId);
    IReadOnlyList<LogroDesbloqueado> Logros();
    void AgregarLogro(LogroDesbloqueado logro);
}
