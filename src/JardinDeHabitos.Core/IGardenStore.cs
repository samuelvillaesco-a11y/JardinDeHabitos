namespace JardinDeHabitos.Core;

public interface IGardenStore
{
    IReadOnlyList<HabitRecord> Cargar();
    void Guardar(HabitRecord registro);
    void Eliminar(Guid habitoId);

    /// <summary>Contadores que alimentan los logros (clave → valor).</summary>
    IReadOnlyDictionary<string, int> Estadisticas();
    void GuardarEstadisticas(IReadOnlyDictionary<string, int> cambios);

    IReadOnlyList<NivelLogro> NivelesLogro();
    void AgregarNivel(NivelLogro nivel);

    /// <summary>Ejecuta varios cambios como uno solo: o se guardan todos o ninguno.</summary>
    void EnTransaccion(Action accion) => accion();
}
