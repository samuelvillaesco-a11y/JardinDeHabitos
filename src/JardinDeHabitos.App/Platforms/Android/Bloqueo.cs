using Android.App;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Java.Lang;

namespace JardinDeHabitos.App;

/// <summary>
/// Pide el bloqueo del teléfono (huella, PIN o patrón) antes de abrir el estante.
/// Usa el mismo bloqueo del celular: no hay otra contraseña que recordar.
/// </summary>
public static class Bloqueo
{
    public enum Resultado { Abierto, Cancelado, SinBloqueo }

    public static Task<Resultado> PedirAsync()
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
            .SetTitle("Tu estante de trofeos")
            .SetSubtitle("Desbloquea para verlo");
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            builder.SetAllowedAuthenticators((int)(BiometricManagerAuthenticators.BiometricWeak | BiometricManagerAuthenticators.DeviceCredential));
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
