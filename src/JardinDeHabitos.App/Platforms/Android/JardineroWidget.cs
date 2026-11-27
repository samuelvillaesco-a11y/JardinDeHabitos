using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>
/// Widget "Llama a tu jardinero": muestra el nombre del primer jardinero y, al tocarlo, abre el
/// marcador del teléfono con su número listo (ACTION_DIAL: sin permiso de llamadas, el usuario confirma).
/// Sin jardineros, invita a agregar uno y abre la app.
///
/// El widget no abre la base cifrada: guarda solo nombre y número del primer jardinero en unas
/// preferencias privadas de la app (sin respaldos), que se actualizan cada vez que cambian los
/// jardineros. Así se pinta rápido y sin tocar el Keystore; el nombre ya está a la vista en el widget.
/// Esa copia sin cifrar solo existe mientras haya un widget puesto: al quitarlo se borra.
/// </summary>
[BroadcastReceiver(Label = "@string/widget_jardinero_nombre", Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_jardinero_info")]
public class JardineroWidget : AppWidgetProvider
{
    private const string Archivo = "widget_jardinero";
    private const string ClaveNombre = "nombre";
    private const string ClaveNumero = "numero";
    private const string ClaveSincronizado = "sincronizado";

    public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? ids)
    {
        if (context is null || manager is null || ids is null) return;
        try
        {
            // Primera vez (jardineros agregados antes de existir el widget): se lee la base una sola vez.
            if (!Preferencias(context).GetBoolean(ClaveSincronizado, false))
                Guardar(context, Servicios.Jardin.Jardineros().FirstOrDefault());
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("JardinDeHabitos", $"Widget: no se pudieron leer los jardineros: {ex.Message}");
        }
        manager.UpdateAppWidget(ids, Vista(context));
    }

    /// <summary>Lo llama la app al agregar o quitar jardineros: guarda el primero y repinta los widgets.</summary>
    public static void Sincronizar(Jardinero? primero)
    {
        var context = Android.App.Application.Context;
        var manager = AppWidgetManager.GetInstance(context);
        var ids = manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(JardineroWidget))));
        if (manager is null || ids is not { Length: > 0 })
        {
            Borrar(context); // sin widget no hace falta la copia sin cifrar
            return;
        }
        Guardar(context, primero);
        manager.UpdateAppWidget(ids, Vista(context));
    }

    /// <summary>Se quitó el último widget: se borra el nombre y número guardados sin cifrar.</summary>
    public override void OnDisabled(Context? context)
    {
        base.OnDisabled(context);
        if (context is not null) Borrar(context);
    }

    private static void Borrar(Context c) => Preferencias(c).Edit()!.Clear()!.Apply();

    private static ISharedPreferences Preferencias(Context c) => c.GetSharedPreferences(Archivo, FileCreationMode.Private)!;

    private static void Guardar(Context c, Jardinero? j)
    {
        var editor = Preferencias(c).Edit()!;
        if (j is null) editor.Remove(ClaveNombre)!.Remove(ClaveNumero);
        else editor.PutString(ClaveNombre, j.Nombre)!.PutString(ClaveNumero, j.Telefono);
        editor.PutBoolean(ClaveSincronizado, true)!.Apply();
    }

    private static RemoteViews Vista(Context c)
    {
        var p = Preferencias(c);
        string? nombre = p.GetString(ClaveNombre, null);
        string? numero = p.GetString(ClaveNumero, null);
        var vista = new RemoteViews(c.PackageName, Resource.Layout.widget_jardinero);

        Intent intento;
        if (nombre is null || numero is null)
        {
            vista.SetTextViewText(Resource.Id.widget_nombre, "Agrega un jardinero");
            vista.SetTextViewText(Resource.Id.widget_detalle, "Toca para abrir");
            intento = c.PackageManager!.GetLaunchIntentForPackage(c.PackageName!)!;
        }
        else
        {
            vista.SetTextViewText(Resource.Id.widget_nombre, nombre);
            vista.SetTextViewText(Resource.Id.widget_detalle, "Toca para llamar");
            intento = new Intent(Intent.ActionDial, Android.Net.Uri.Parse("tel:" + Uri.EscapeDataString(numero)));
        }
        vista.SetContentDescription(Resource.Id.widget_raiz, nombre is null ? "Agrega un jardinero" : $"Llamar a {nombre}");
        var alTocar = PendingIntent.GetActivity(c, 0, intento, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        vista.SetOnClickPendingIntent(Resource.Id.widget_raiz, alTocar);
        return vista;
    }
}
