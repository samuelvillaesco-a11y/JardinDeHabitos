namespace JardinDeHabitos.Core;

/// <summary>Almacén en memoria (pruebas y vista previa).</summary>
public sealed class InMemoryGardenStore : IGardenStore
{
    private readonly Dictionary<Guid, HabitRecord> _habitos = [];
    private readonly List<LogroDesbloqueado> _logros = [];

    public IReadOnlyList<HabitRecord> Cargar() => [.. _habitos.Values.OrderBy(r => r.Orden)];
    public void Guardar(HabitRecord registro) => _habitos[registro.Habito.Id] = registro;
    public void Eliminar(Guid habitoId) => _habitos.Remove(habitoId);
    public IReadOnlyList<LogroDesbloqueado> Logros() => [.. _logros];
    public void AgregarLogro(LogroDesbloqueado logro) => _logros.Add(logro);
}
