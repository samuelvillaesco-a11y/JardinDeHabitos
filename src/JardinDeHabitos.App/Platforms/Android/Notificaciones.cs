using Android.App;
using Android.Content;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>
/// Notificaciones del jardín: juguetonas e inofensivas a simple vista. Nunca nombran el hábito,
/// solo la especie del árbol. Dos visitas al día: 9:00 (novedades y días libres) y 20:00 (recordatorio).
/// </summary>
public static class Notificaciones
{
    private const string Canal = "tu-jardin";
    private const string ExtraMomento = "momento";
    private const int Manana = 9;
    private const int Tarde = 20;
    private static int _siguienteId = 2000;

    public static void Programar(Context context)
    {
        CrearCanal(context);
        ProgramarA(context, Manana);
        ProgramarA(context, Tarde);
    }

    private static void ProgramarA(Context context, int hora)
    {
        var intent = new Intent(context, typeof(NotificacionesReceiver)).PutExtra(ExtraMomento, hora);
        var pendiente = PendingIntent.GetBroadcast(context, hora, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        if (pendiente is null || context.GetSystemService(Context.AlarmService) is not AlarmManager alarmas) return;

        var ahora = DateTime.Now;
        var proxima = ahora.Date.AddHours(hora);
        if (proxima <= ahora) proxima = proxima.AddDays(1);
        long msEpoch = new DateTimeOffset(proxima).ToUnixTimeMilliseconds();
        alarmas.SetInexactRepeating(AlarmType.RtcWakeup, msEpoch, AlarmManager.IntervalDay, pendiente);
    }

    public static void Mostrar(Context context, string titulo, string texto)
    {
        CrearCanal(context);
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager nm) return;

        var abrir = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var alTocar = abrir is null ? null : PendingIntent.GetActivity(context, 0, abrir, PendingIntentFlags.Immutable);

        var aviso = new Notification.Builder(context, Canal)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(titulo)
            .SetContentText(texto)
            .SetStyle(new Notification.BigTextStyle().BigText(texto))
            .SetAutoCancel(true)
            .SetContentIntent(alTocar)
            .Build();
        nm.Notify(Interlocked.Increment(ref _siguienteId), aviso);
    }

    private static void CrearCanal(Context context)
    {
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager nm) return;
        nm.CreateNotificationChannel(new NotificationChannel(Canal, "Tu jardín", NotificationImportance.Default)
        {
            Description = "Novedades y recordatorios de tu jardín.",
        });
    }

    internal static void Revisar(Context context, int momento)
    {
        if (context.FilesDir?.AbsolutePath is not { } dir) return;
        var ruta = Path.Combine(dir, Servicios.NombreBaseDeDatos);
        if (!File.Exists(ruta)) return;

        var mensajes = new List<(string Titulo, string Texto)>();
        using (var store = new SqliteGardenStore(ruta))
        {
            var jardin = new GardenService(store, new GrowthStageCalculator());
            var arboles = jardin.Jardin();

            foreach (var a in jardin.TomarAvisos()) mensajes.Add(Texto(a));

            if (momento == Manana)
            {
                foreach (var v in arboles.Where(v => v.EsCheatDayHoy))
                    mensajes.Add(($"☀️ Hoy tu {Min(v.Especie)} descansa", "Disfruta tu día libre con medida: un día libre no es un día sin freno."));
            }
            else if (arboles.Any(v => v.PideCheckInHoy))
            {
                mensajes.Add(Recordatorios[jardin.Hoy.DayNumber % Recordatorios.Length]);
            }

            if (jardin.TomarLogrosNuevos() > 0) mensajes.Add((Avisos.TituloLogro, Avisos.CuerpoLogro));
        }

        foreach (var (titulo, texto) in mensajes) Mostrar(context, titulo, texto);
    }

    private static readonly (string, string)[] Recordatorios =
    [
        ("🌧️ Tu jardín tiene sed", "¿Pasas a regarlo?"),
        ("🌱 Tus árboles te extrañan un poquito", "Una vuelta rápida por el jardín y listo."),
        ("🪴 Hora de dar una vuelta por el jardín", "Tus árboles esperan su visita de hoy."),
        ("🐝 Las abejas preguntan por ti", "Tu jardín te espera."),
    ];

    private static (string, string) Texto(Aviso a)
    {
        string e = Min(a.Especie);
        return a.Tipo switch
        {
            TipoAviso.Etapa => a.Etapa switch
            {
                GrowthStage.Flor => ($"🌸 ¡Tu {e} floreció!", "Pasa a verlo, está precioso."),
                GrowthStage.FrutaInmadura => ($"🍏 ¡Tu {e} dio sus primeros frutos!", "Todavía están verdes, pero ahí van."),
                GrowthStage.FrutaComestible => ($"🍎 ¡Los frutos de tu {e} ya están maduros!", "Tu jardín está de fiesta."),
                _ => ($"✨ ¡Tu {e} dio una fruta legendaria!", "Algo muy especial creció en tu jardín."),
            },
            TipoAviso.MiniEvento => ($"🐌 Algo pasa en tu {e}…", "Un pequeño visitante se coló en el jardín. ¿Pasas a ver?"),
            TipoAviso.PlagaPorAbandono => ($"🐛 ¡Oh no! Algo le pasa a tu {e}", "Necesita que lo cuides unos días."),
            _ => ($"🍂 Tu {e} volvió a ser semilla", "Siempre se puede volver a plantar. Tu jardín te espera."),
        };
    }

    private static string Min(string especie) => especie.ToLower(Tema.Cultura);
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificacionesReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;
        // Un fallo aquí (base ocupada o dañada) no debe cerrar la app en segundo plano.
        try { Notificaciones.Revisar(context, intent?.GetIntExtra("momento", 9) ?? 9); }
        catch (Exception ex) { Android.Util.Log.Warn("JardinDeHabitos", $"Notificación falló: {ex}"); }
    }
}

/// <summary>Android borra las alarmas al reiniciar: se vuelven a programar al encender.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class ReprogramarAvisoReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent?.Action != Intent.ActionBootCompleted) return;
        try { Notificaciones.Programar(context); }
        catch (Exception ex) { Android.Util.Log.Warn("JardinDeHabitos", $"Reprogramar aviso falló: {ex}"); }
    }
}
