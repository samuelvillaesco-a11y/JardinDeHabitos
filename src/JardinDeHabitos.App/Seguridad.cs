using JardinDeHabitos.App.Paginas;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

public enum ResultadoClave { Correcta, Incorrecta, Esperar }

/// <summary>
/// Seguridad 100 % local. Los interruptores viven en Preferences; la clave de la app y el código
/// de recuperación solo como hash PBKDF2 con sal, dentro de SecureStorage (cifrado con el Keystore
/// de Android). No hay cuentas, correo, servidores ni internet.
/// </summary>
public static class Seguridad
{
    private const string ClaveBienvenida = "seguridad.bienvenida";
    private const string ClaveHash = "seguridad.clave";
    private const string ClaveRecuperacion = "seguridad.recuperacion";
    private const string ClaveFallidos = "seguridad.fallidos";
    private const string ClaveUltimoFallo = "seguridad.ultimoFallo";
    private const string ClaveTipo = "seguridad.tipo";

    private static string Clave(Seguro s) => $"seguridad.seguro.{s}";

    public static bool Activo(Seguro s) => Preferences.Default.Get(Clave(s), Seguros.PorDefecto(s));

    /// <summary>Cambia un seguro. Quien llama ya verificó al dueño.</summary>
    public static void Cambiar(Seguro s, bool activo)
    {
        Preferences.Default.Set(Clave(s), activo);
        Pantalla.Privada(PantallaDebeSerPrivada);
    }

    /// <summary>Con el seguro general, la app no sale en capturas ni en la vista de apps recientes.</summary>
    public static bool PantallaDebeSerPrivada => Activo(Seguro.General);

    /// <summary>La pantalla inicial (crear clave, opcional) se muestra una sola vez.</summary>
    public static bool BienvenidaHecha => Preferences.Default.Get(ClaveBienvenida, false);

    public static void MarcarBienvenida() => Preferences.Default.Set(ClaveBienvenida, true);

    // ---------- Candado: bloqueo del teléfono, PIN propio o patrón propio ----------

    /// <summary>
    /// Con qué se abre lo que está bajo llave. Por defecto, el bloqueo del teléfono. Una clave de
    /// texto de versiones anteriores sigue valiendo (tipo Clave).
    /// </summary>
    public static async Task<TipoCandado> TipoAsync()
    {
        if (!await TieneClaveAsync()) return TipoCandado.Telefono;
        var t = Preferences.Default.Get(ClaveTipo, (int)TipoCandado.Clave);
        return Enum.IsDefined(typeof(TipoCandado), t) && t != (int)TipoCandado.Telefono ? (TipoCandado)t : TipoCandado.Clave;
    }

    public static string Nombre(TipoCandado t) => t switch
    {
        TipoCandado.Pin => "PIN propio",
        TipoCandado.Patron => "Patrón propio",
        TipoCandado.Clave => "Clave propia",
        _ => "Bloqueo del teléfono",
    };

    /// <summary>Usar el mismo bloqueo del teléfono: se borra el secreto propio (y su código de recuperación).</summary>
    public static async Task UsarBloqueoDelTelefonoAsync()
    {
        await QuitarClaveAsync();
        Preferences.Default.Set(ClaveTipo, (int)TipoCandado.Telefono);
    }

    public static async Task<bool> TieneClaveAsync() => HashDeClave.Leer(await LeerAsync(ClaveHash)) is not null;

    /// <summary>
    /// Guarda un PIN, patrón o clave propios (solo su hash PBKDF2 con sal) y devuelve un código de
    /// recuperación nuevo para mostrar UNA vez.
    /// </summary>
    public static async Task<string> CrearSecretoAsync(TipoCandado tipo, string clave)
    {
        if (ClaveLocal.Problema(tipo, clave) is { } p) throw new ArgumentException(p);
        var codigo = CodigoDeRecuperacion.Generar();
        var (hClave, hCodigo) = await Task.Run(() =>
            (ClaveLocal.Crear(clave), ClaveLocal.Crear(CodigoDeRecuperacion.Normalizar(codigo))));
        await SecureStorage.Default.SetAsync(ClaveHash, hClave.Serializar());
        await SecureStorage.Default.SetAsync(ClaveRecuperacion, hCodigo.Serializar());
        Preferences.Default.Set(ClaveTipo, (int)tipo);
        await ReiniciarFallosAsync();
        return codigo;
    }

    public static async Task QuitarClaveAsync()
    {
        SecureStorage.Default.Remove(ClaveHash);
        SecureStorage.Default.Remove(ClaveRecuperacion);
        await ReiniciarFallosAsync();
        Cambiar(Seguro.DobleVerificacion, false);
    }

    public static Task<ResultadoClave> ProbarClaveAsync(string? clave) => ProbarAsync(ClaveHash, clave);

    public static Task<ResultadoClave> ProbarRecuperacionAsync(string? codigo) =>
        ProbarAsync(ClaveRecuperacion, CodigoDeRecuperacion.Normalizar(codigo));

    /// <summary>Cuánto falta para poder volver a intentar tras varios fallos.</summary>
    public static async Task<TimeSpan> EsperaRestanteAsync()
    {
        int fallidos = int.TryParse(await LeerAsync(ClaveFallidos), out int f) ? f : 0;
        var ultimo = long.TryParse(await LeerAsync(ClaveUltimoFallo), out long u) ? DateTimeOffset.FromUnixTimeSeconds(u) : DateTimeOffset.MinValue;
        return Intentos.Restante(fallidos, ultimo, DateTimeOffset.UtcNow);
    }

    private static async Task<ResultadoClave> ProbarAsync(string clave, string? texto)
    {
        if (await EsperaRestanteAsync() > TimeSpan.Zero) return ResultadoClave.Esperar;
        var guardado = HashDeClave.Leer(await LeerAsync(clave));
        if (guardado is null) return ResultadoClave.Incorrecta;
        bool ok = await Task.Run(() => ClaveLocal.Verificar(texto, guardado));
        if (ok)
        {
            await ReiniciarFallosAsync();
            return ResultadoClave.Correcta;
        }
        int fallidos = (int.TryParse(await LeerAsync(ClaveFallidos), out int f) ? f : 0) + 1;
        await SecureStorage.Default.SetAsync(ClaveFallidos, fallidos.ToString());
        await SecureStorage.Default.SetAsync(ClaveUltimoFallo, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return ResultadoClave.Incorrecta;
    }

    private static async Task ReiniciarFallosAsync()
    {
        SecureStorage.Default.Remove(ClaveFallidos);
        SecureStorage.Default.Remove(ClaveUltimoFallo);
        await Task.CompletedTask;
    }

    private static async Task<string?> LeerAsync(string clave)
    {
        try { return await SecureStorage.Default.GetAsync(clave); }
        catch (Exception) { return null; } // Keystore dañado: se trata como si no hubiera nada.
    }

    // ---------- Verificación ----------

    /// <summary>Entrar a una parte con llave. Si su seguro está apagado, deja pasar.</summary>
    public static async Task<bool> EntrarAsync(Page pagina, Seguro seccion, string titulo) =>
        !Activo(seccion) || await VerificarAsync(pagina, titulo);

    /// <summary>
    /// Verifica al dueño: con clave de la app, se pide la clave; con doble verificación, la clave
    /// y además el bloqueo del teléfono; sin clave, el bloqueo del teléfono.
    /// </summary>
    public static async Task<bool> VerificarAsync(Page pagina, string titulo)
    {
        var tipo = await TipoAsync();
        bool tieneClave = tipo != TipoCandado.Telefono;
        if (tieneClave)
        {
            if (!await PedirClavePage.MostrarAsync(pagina.Navigation, titulo, tipo)) return false;
            if (!Activo(Seguro.DobleVerificacion)) return true;
        }

        var r = await Bloqueo.PedirAsync(titulo, tieneClave ? "Segundo paso: el bloqueo del teléfono" : "Confirma que eres tú");
        if (r == Bloqueo.Resultado.Abierto) return true;
        if (r == Bloqueo.Resultado.Cancelado) return false;
        // El teléfono no tiene bloqueo: no hay segundo factor posible.
        await pagina.DisplayAlertAsync("Tu teléfono no tiene bloqueo",
            tieneClave
                ? "La doble verificación necesita el PIN, patrón o huella del teléfono. Por ahora se entró solo con tu clave."
                : "Para que nadie más pueda entrar, crea una clave de la app o activa un PIN, patrón o huella en el teléfono.",
            "Entendido");
        return true;
    }
}
