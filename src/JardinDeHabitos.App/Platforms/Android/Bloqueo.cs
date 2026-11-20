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
    /// Verdadero mientras el diálogo de bloqueo está abierto. La pantalla de PIN/patrón es otra
    /// actividad: la app pasa a segundo plano y no debe volver a cerrarse con llave por eso.
    /// </summary>
    public static bool EnCurso { get; private set; }

    /// <summary>¿El teléfono tiene huella, PIN o patrón configurado?</summary>
    public static bool TelefonoTieneBloqueo =>
        Platform.CurrentActivity?.GetSystemService(Context.KeyguardService) is KeyguardManager kg && kg.IsDeviceSecure;

    public static Task<Resultado> PedirAsync() => PedirAsync("Tu estante de trofeos", "Desbloquea para verlo");

    public static async Task<Resultado> PedirAsync(string titulo, string subtitulo)
    {
        EnCurso = true;
        try { return await Pedir(titulo, subtitulo); }
        finally
        {
            // La actividad del PIN tarda un instante en cerrarse y la app en volver al frente.
            await Task.Delay(800);
            EnCurso = false;
        }
    }

    private static Task<Resultado> Pedir(string titulo, string subtitulo)
    {
        var actividad = Platform.CurrentActivity;
        if (actividad is null) return Task.FromResult(Resultado.Cancelado);

        if (actividad.GetSystemService(Context.KeyguardService) is not KeyguardManager kg || !kg.IsDeviceSecure)
            return Task.FromResult(Resultado.SinBloqueo);
        // Android 9 o anterior no ofrece PIN/patrón en este diálogo.
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
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
