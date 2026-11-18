using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>Una sola instancia del jardín para toda la app.</summary>
public static class Servicios
{
    public const string NombreBaseDeDatos = "jardin.db3";

    private static GardenService? _jardin;

    public static GardenService Jardin => _jardin ??= new GardenService(
        new SqliteGardenStore(Path.Combine(FileSystem.AppDataDirectory, NombreBaseDeDatos)),
        new GrowthStageCalculator());
}
