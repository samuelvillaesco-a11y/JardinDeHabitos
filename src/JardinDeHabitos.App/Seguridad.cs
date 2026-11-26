using JardinDeHabitos.App.Paginas;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

public enum ResultadoClave { Correcta, Incorrecta, Esperar, SoloRecuperacion }

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
        Pantalla.Actualizar();
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

    /// <summary>
    /// Había un PIN, patrón o clave propios (Preferences lo recuerda), pero el almacenamiento seguro
    /// ya no los entrega (Keystore dañado o borrado). No debe tratarse como "sin candado".
    /// </summary>
    private static async Task<bool> SecretoPerdidoAsync() =>
        Preferences.Default.Get(ClaveTipo, (int)TipoCandado.Telefono) != (int)TipoCandado.Telefono
        && !await TieneClaveAsync();

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
            (ClaveLocal.Crear(clave), ClaveLocal.Crear(CodigoDeRecuperacion.Normalizar(codigo), ClaveLocal.IteracionesCodigo)));
        await SecureStorage.Default.SetAsync(ClaveHash, hClave.Serializar());
        await SecureStorage.Default.SetAsync(ClaveRecuperacion, hCodigo.Serializar());
        Preferences.Default.Set(ClaveTipo, (int)tipo);
        ReiniciarFallos();
        return codigo;
    }

    public static Task QuitarClaveAsync()
    {
        SecureStorage.Default.Remove(ClaveHash);
        SecureStorage.Default.Remove(ClaveRecuperacion);
        ReiniciarFallos();
        Cambiar(Seguro.DobleVerificacion, false);
        return Task.CompletedTask;
    }

    public static async Task<ResultadoClave> ProbarClaveAsync(string? clave)
    {
        int fallidos = int.TryParse(await LeerAsync(ClaveFallidos), out int f) ? f : 0;
        if (Intentos.SoloRecuperacion(fallidos)) return ResultadoClave.SoloRecuperacion;
        return await ProbarAsync(ClaveHash, clave);
    }

    public static Task<ResultadoClave> ProbarRecuperacionAsync(string? codigo) =>
        ProbarAsync(ClaveRecuperacion, CodigoDeRecuperacion.Normalizar(codigo));

    /// <summary>Cuánto falta para poder volver a intentar tras varios fallos.</summary>
    public static async Task<TimeSpan> EsperaRestanteAsync()
    {
        int fallidos = int.TryParse(await LeerAsync(ClaveFallidos), out int f) ? f : 0;
        var ultimo = long.TryParse(await LeerAsync(ClaveUltimoFallo), out long u) ? DateTimeOffset.FromUnixTimeSeconds(u) : DateTimeOffset.MinValue;
        return Intentos.Restante(fallidos, ultimo, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Un intento a la vez: si dos toques prueban al mismo tiempo, los dos leerían el mismo número de
    /// fallos y contarían como uno solo.
    /// </summary>
    private static readonly SemaphoreSlim _unIntento = new(1, 1);

    private static async Task<ResultadoClave> ProbarAsync(string clave, string? texto)
    {
        await _unIntento.WaitAsync();
        try { return await ProbarUnoAsync(clave, texto); }
        finally { _unIntento.Release(); }
    }

    private static async Task<ResultadoClave> ProbarUnoAsync(string clave, string? texto)
    {
        if (await EsperaRestanteAsync() > TimeSpan.Zero) return ResultadoClave.Esperar;
        var guardado = HashDeClave.Leer(await LeerAsync(clave));
        if (guardado is null) return ResultadoClave.Incorrecta;
        bool ok = await Task.Run(() => ClaveLocal.Verificar(texto, guardado));
        if (ok)
        {
            ReiniciarFallos();
            return ResultadoClave.Correcta;
        }
        int fallidos = (int.TryParse(await LeerAsync(ClaveFallidos), out int f) ? f : 0) + 1;
        await SecureStorage.Default.SetAsync(ClaveFallidos, fallidos.ToString());
        await SecureStorage.Default.SetAsync(ClaveUltimoFallo, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return ResultadoClave.Incorrecta;
    }

    private static void ReiniciarFallos()
    {
        SecureStorage.Default.Remove(ClaveFallidos);
        SecureStorage.Default.Remove(ClaveUltimoFallo);
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
    public static Task<bool> VerificarAsync(Page pagina, string titulo)
    {
        // Un doble toque no abre dos verificaciones: se comparte la que ya está en curso.
        if (_verificacion is { IsCompleted: false } enCurso) return enCurso;
        return _verificacion = Verificar(pagina, titulo);
    }

    private static Task<bool>? _verificacion;

    private static async Task<bool> Verificar(Page pagina, string titulo)
    {
        var tipo = await TipoAsync();
        bool tieneClave = tipo != TipoCandado.Telefono;
        if (tieneClave)
        {
            if (!await PedirClavePage.MostrarAsync(pagina.Navigation, titulo, tipo)) return false;
            if (!Activo(Seguro.DobleVerificacion)) return true;
        }

        bool perdido = !tieneClave && await SecretoPerdidoAsync();
        var r = await Bloqueo.PedirAsync(titulo, tieneClave ? "Segundo paso: el bloqueo del teléfono" : "Confirma que eres tú");
        if (r == Bloqueo.Resultado.Abierto) return true;
        if (r == Bloqueo.Resultado.Cancelado) return false;
        if (perdido)
        {
            // Sin candado propio legible y sin bloqueo del teléfono: no se abre (antes dejaba pasar).
            await pagina.DisplayAlertAsync("No se pudo leer tu candado",
                "Android ya no entrega tu PIN, patrón o clave. Para entrar, activa un PIN, patrón o huella en el teléfono.",
                "Entendido");
            return false;
        }
        // El teléfono no tiene bloqueo: no hay segundo factor posible.
        await pagina.DisplayAlertAsync("Tu teléfono no tiene bloqueo",
            tieneClave
                ? "La doble verificación necesita el PIN, patrón o huella del teléfono. Por ahora se entró solo con tu clave."
                : "Para que nadie más pueda entrar, crea una clave de la app o activa un PIN, patrón o huella en el teléfono.",
            "Entendido");
        return true;
    }
}
