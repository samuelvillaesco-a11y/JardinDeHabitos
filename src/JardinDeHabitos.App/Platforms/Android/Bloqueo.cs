using Android.App;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Java.Lang;

namespace JardinDeHabitos.App;

/// <summary>
/// Pide el bloqueo del teléfono (huella, PIN o patrón). Usa el mismo bloqueo del celular:
/// no hay otra contraseña que recordar ni nada que guardar. Solo biometría fuerte (clase 3)
/// o la credencial del dispositivo: un desbloqueo facial 2D no basta.
/// </summary>
public static class Bloqueo
{
    public enum Resultado { Abierto, Cancelado, SinBloqueo }

    /// <summary>
    /// Cuándo fue la última verificación exitosa. La pantalla de PIN del sistema manda la app al
    /// fondo: si el usuario se verificó mientras tanto, volver no cuenta como haber salido.
    /// </summary>
    public static DateTime UltimoExito { get; private set; } = DateTime.MinValue;

    /// <summary>¿El teléfono tiene huella, PIN o patrón configurado?</summary>
    public static bool TelefonoTieneBloqueo =>
        Platform.CurrentActivity?.GetSystemService(Context.KeyguardService) is KeyguardManager kg && kg.IsDeviceSecure;

    public static async Task<Resultado> PedirAsync(string titulo, string subtitulo)
    {
        var r = await Pedir(titulo, subtitulo);
        if (r == Resultado.Abierto) UltimoExito = DateTime.UtcNow;
        return r;
    }

    private static Task<Resultado> Pedir(string titulo, string subtitulo)
    {
        var actividad = Platform.CurrentActivity;
        if (actividad is null) return Task.FromResult(Resultado.Cancelado);

        // La app pide Android 10 o más (minSdk 29): este diálogo siempre ofrece PIN/patrón del teléfono.
        if (actividad.GetSystemService(Context.KeyguardService) is not KeyguardManager kg || !kg.IsDeviceSecure)
            return Task.FromResult(Resultado.SinBloqueo);

        var tcs = new TaskCompletionSource<Resultado>();
        var builder = new BiometricPrompt.Builder(actividad)
            .SetTitle(titulo)
            .SetSubtitle(subtitulo);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            builder.SetAllowedAuthenticators((int)(BiometricManagerAuthenticators.BiometricStrong | BiometricManagerAuthenticators.DeviceCredential));
        else
#pragma warning disable CA1422 // obsoleto en 30+, pero es la forma correcta en Android 10
            builder.SetDeviceCredentialAllowed(true);
#pragma warning restore CA1422
        try
        {
            builder.Build().Authenticate(new CancellationSignal(), actividad.MainExecutor!, new Callback(tcs));
        }
        catch (Java.Lang.Exception ex)
        {
            // Si el sistema no deja pedir el bloqueo, el estante se queda cerrado (nunca se abre sin permiso).
            Android.Util.Log.Warn("JardinDeHabitos", $"Bloqueo falló: {ex}");
            tcs.TrySetResult(Resultado.Cancelado);
        }
        return tcs.Task;
    }

    private sealed class Callback(TaskCompletionSource<Resultado> tcs) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result) =>
            tcs.TrySetResult(Resultado.Abierto);

        public override void OnAuthenticationError(BiometricErrorCode errorCode, ICharSequence? errString) =>
            tcs.TrySetResult(Resultado.Cancelado);

        // Un intento fallido no cierra el diálogo: el sistema deja reintentar.
        public override void OnAuthenticationFailed() { }
    }
}
