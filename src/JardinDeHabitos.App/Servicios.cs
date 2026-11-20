using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>Una sola instancia del jardín para toda la app.</summary>
public static class Servicios
{
    public const string NombreBaseDeDatos = "jardin.db3";

    private static GardenService? _jardin;
    private static SqliteGardenStore? _store;

    public static GardenService Jardin
    {
        get
        {
            if (_jardin is not null) return _jardin;
            var (archivo, reloj) = Configuracion();
            _store = new SqliteGardenStore(Path.Combine(FileSystem.AppDataDirectory, archivo), llave: LlaveDeDatos());
            return _jardin = new GardenService(_store, new GrowthStageCalculator(reloj));
        }
    }

    private const string ClaveLlave = "datos.llave";

    /// <summary>
    /// Llave de cifrado de la base: 32 bytes al azar creados la primera vez y guardados en SecureStorage
    /// (protegido por el Keystore de Android). Nunca sale del teléfono ni depende del PIN.
    /// </summary>
    public static string LlaveDeDatos()
    {
        // SecureStorage es asíncrono; Task.Run evita bloquear el hilo de la UI esperándolo.
        return Task.Run(async () =>
        {
            var llave = await SecureStorage.Default.GetAsync(ClaveLlave);
            if (llave is not null) return llave;
            llave = SqliteGardenStore.NuevaLlave();
            await SecureStorage.Default.SetAsync(ClaveLlave, llave);
            return llave;
        }).GetAwaiter().GetResult();
    }

#if DEBUG
    // Modo acelerado (solo Debug): 1 día = 1 minuto, en una base aparte para no ensuciar el jardín real.
    private const string BaseAcelerada = "jardin-acelerado.db3";
    private const string ClaveAncla = "debug.acelerado.ancla";

    public static bool Acelerado => Preferences.Default.ContainsKey(ClaveAncla);

    public static void CambiarAcelerado(bool encendido)
    {
        if (encendido == Acelerado) return;
        if (encendido) Preferences.Default.Set(ClaveAncla, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        else Preferences.Default.Remove(ClaveAncla);

        // La base acelerada empieza de cero cada vez que se enciende.
        _store?.Dispose();
        _store = null;
        _jardin = null;
        if (encendido) File.Delete(Path.Combine(FileSystem.AppDataDirectory, BaseAcelerada));
    }

    private static (string Archivo, TimeProvider Reloj) Configuracion()
    {
        if (!Acelerado) return (NombreBaseDeDatos, TimeProvider.System);
        var ancla = DateTimeOffset.FromUnixTimeMilliseconds(Preferences.Default.Get(ClaveAncla, 0L));
        if (ancla > DateTimeOffset.UtcNow) ancla = DateTimeOffset.UtcNow; // reloj del teléfono atrasado
        return (BaseAcelerada, RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1), ancla: ancla));
    }
#else
    private static (string Archivo, TimeProvider Reloj) Configuracion() => (NombreBaseDeDatos, TimeProvider.System);
#endif
}
