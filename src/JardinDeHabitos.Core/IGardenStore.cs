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

    /// <summary>Datos opcionales del usuario (nunca nulo: vacío si no hay nada).</summary>
    Perfil Perfil();
    void GuardarPerfil(Perfil perfil);

    IReadOnlyList<Jardinero> Jardineros();
    void GuardarJardinero(Jardinero jardinero);
    void EliminarJardinero(Guid id);

    /// <summary>Entradas del blog botánico (una por día), de la más vieja a la más nueva.</summary>
    IReadOnlyList<EntradaDiario> Diario();
    void GuardarEntrada(EntradaDiario entrada);
    void EliminarEntrada(DateOnly dia);

    /// <summary>Ejecuta varios cambios como uno solo: o se guardan todos o ninguno.</summary>
    void EnTransaccion(Action accion) => accion();
}
