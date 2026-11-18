using Android.App;
using Android.Content;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>Aviso diario a las 9:00: si hoy es día libre de algún hábito, recuerda la moderación.</summary>
public static class AvisoCheatDay
{
    private const string Canal = "dias-libres";
    private const int IdAviso = 1001;

    public static void Programar(Context context)
    {
        CrearCanal(context);

        var intent = new Intent(context, typeof(AvisoCheatDayReceiver));
        var pendiente = PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        if (pendiente is null || context.GetSystemService(Context.AlarmService) is not AlarmManager alarmas) return;

        var ahora = DateTime.Now;
        var proxima = ahora.Date.AddHours(9);
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
        nm.Notify(IdAviso, aviso);
    }

    private static void CrearCanal(Context context)
    {
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager nm) return;
        var canal = new NotificationChannel(Canal, "Días libres", NotificationImportance.Default)
        {
            Description = "Recordatorio de moderación en tus días libres.",
        };
        nm.CreateNotificationChannel(canal);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class AvisoCheatDayReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context?.FilesDir?.AbsolutePath is not { } dir) return;
        var ruta = Path.Combine(dir, Servicios.NombreBaseDeDatos);
        if (!File.Exists(ruta)) return;

        List<string> libres;
        using (var store = new SqliteGardenStore(ruta))
        {
            var jardin = new GardenService(store, new GrowthStageCalculator());
            libres = [.. jardin.Jardin().Where(v => v.EsCheatDayHoy).Select(v => v.Nombre)];
        }
        if (libres.Count == 0) return;

        AvisoCheatDay.Mostrar(context, "Hoy es día libre",
            $"Día libre de {string.Join(" y ", libres)}. Disfrútalo con moderación: un día libre no es un día sin medida.");
    }
}
