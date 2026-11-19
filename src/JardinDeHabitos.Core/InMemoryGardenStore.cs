namespace JardinDeHabitos.Core;

/// <summary>Almacén en memoria (pruebas y vista previa).</summary>
public sealed class InMemoryGardenStore : IGardenStore
{
    private readonly Dictionary<Guid, HabitRecord> _habitos = [];
    private readonly Dictionary<string, int> _stats = [];
    private readonly List<NivelLogro> _niveles = [];

    public IReadOnlyList<HabitRecord> Cargar() => [.. _habitos.Values.OrderBy(r => r.Orden)];
    public void Guardar(HabitRecord registro) => _habitos[registro.Habito.Id] = registro;
    public void Eliminar(Guid habitoId) => _habitos.Remove(habitoId);
    public IReadOnlyDictionary<string, int> Estadisticas() => new Dictionary<string, int>(_stats);

    public void GuardarEstadisticas(IReadOnlyDictionary<string, int> cambios)
    {
        foreach (var (k, v) in cambios) _stats[k] = v;
    }

    public IReadOnlyList<NivelLogro> NivelesLogro() => [.. _niveles];
    public void AgregarNivel(NivelLogro nivel) => _niveles.Add(nivel);
}
