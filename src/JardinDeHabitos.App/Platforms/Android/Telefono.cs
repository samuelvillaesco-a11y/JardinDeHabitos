using Android.App;
using Android.Content;
using Android.Provider;

namespace JardinDeHabitos.App;

/// <summary>
/// Llamadas, mensajes y selector de contactos, todo a través de las apps del propio teléfono.
/// No pide permisos: el marcador y la app de mensajes dejan al usuario confirmar, y el selector
/// de contactos solo comparte el contacto elegido (nunca la agenda completa).
/// </summary>
public static class Telefono
{
    public const string Emergencias = "911";
    public const string LineaDeLaVida = "8009112000";
    private const int CodigoContacto = 4711;
    private static TaskCompletionSource<(string Nombre, string Numero)?>? _eleccion;

    /// <summary>Abre el marcador con el número escrito; el usuario confirma la llamada.</summary>
    public static bool Marcar(string numero) =>
        Abrir(new Intent(Intent.ActionDial, Android.Net.Uri.Parse("tel:" + Uri.EscapeDataString(numero))));

    /// <summary>Abre la app de mensajes con el texto listo; el usuario decide si lo envía.</summary>
    public static bool Mensaje(string numero, string texto)
    {
        var i = new Intent(Intent.ActionSendto, Android.Net.Uri.Parse("smsto:" + Uri.EscapeDataString(numero)));
        i.PutExtra("sms_body", texto);
        return Abrir(i);
    }

    /// <summary>Abre el selector de contactos del sistema y devuelve el elegido (o null si se canceló).</summary>
    public static Task<(string Nombre, string Numero)?> ElegirContactoAsync()
    {
        var actividad = Platform.CurrentActivity;
        if (actividad is null) return Task.FromResult<(string, string)?>(null);
        _eleccion?.TrySetResult(null);
        _eleccion = new();
        try
        {
            actividad.StartActivityForResult(new Intent(Intent.ActionPick, ContactsContract.CommonDataKinds.Phone.ContentUri), CodigoContacto);
        }
        catch (ActivityNotFoundException)
        {
            _eleccion.TrySetResult(null);
        }
        return _eleccion.Task;
    }

    /// <summary>Lo llama MainActivity al volver del selector.</summary>
    public static void AlVolver(int codigo, Result resultado, Intent? datos)
    {
        if (codigo != CodigoContacto || _eleccion is null) return;
        var tcs = _eleccion;
        _eleccion = null;
        if (resultado != Result.Ok || datos?.Data is not { } uri)
        {
            tcs.TrySetResult(null);
            return;
        }
        try
        {
            string[] columnas = [ContactsContract.Contacts.InterfaceConsts.DisplayName, ContactsContract.CommonDataKinds.Phone.Number];
            using var c = Android.App.Application.Context.ContentResolver?.Query(uri, columnas, null, null, null);
            if (c is not null && c.MoveToFirst())
            {
                tcs.TrySetResult((c.GetString(0) ?? "", c.GetString(1) ?? ""));
                return;
            }
        }
        catch (Java.Lang.Exception) { }
        tcs.TrySetResult(null);
    }

    private static bool Abrir(Intent i)
    {
        try
        {
            i.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(i);
            return true;
        }
        catch (ActivityNotFoundException)
        {
            return false;
        }
    }
}
