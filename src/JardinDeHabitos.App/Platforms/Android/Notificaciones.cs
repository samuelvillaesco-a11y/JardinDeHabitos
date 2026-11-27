using Android.App;
using Android.Content;
using JardinDeHabitos.Core;
using Dibujo_ = JardinDeHabitos.App.Dibujo;

namespace JardinDeHabitos.App;

/// <summary>
/// Notificaciones del jardín: juguetonas e inofensivas a simple vista. Nunca nombran el hábito,
/// solo la especie del árbol. Dos visitas al día: 9:00 (novedades y días libres) y 20:00 (recordatorio).
/// </summary>
public static class Notificaciones
{
    private const string Canal = "tu-jardin";
    internal const string ExtraMomento = "momento";
    internal const int Manana = 9;
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

    public static void Mostrar(Context context, string titulo, string texto, Android.Graphics.Bitmap? imagen = null)
    {
        CrearCanal(context);
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager nm) return;

        var abrir = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var alTocar = abrir is null ? null : PendingIntent.GetActivity(context, 0, abrir, PendingIntentFlags.Immutable);

        var aviso = new Notification.Builder(context, Canal)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(titulo)
            .SetContentText(texto)
            .SetStyle(imagen is null
                ? new Notification.BigTextStyle().BigText(texto)
                : new Notification.BigPictureStyle().BigPicture(imagen).SetSummaryText(texto))
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

    internal static void Revisar(int momento)
    {
        // Sin jardín todavía (la app nunca se abrió) no hay nada que avisar.
        if (!File.Exists(Path.Combine(FileSystem.AppDataDirectory, Servicios.NombreBaseDeDatos))) return;

        // El receptor corre en el hilo principal del mismo proceso: usa el mismo jardín que la app
        // (una sola conexión, misma llave, mismo reloj) en vez de abrir otra copia de la base.
        var mensajes = new List<(string Titulo, string Texto)>();
        var conImagen = new List<(string Titulo, string Texto, VistaArbol Arbol, GrowthStage Etapa)>();
        {
            var jardin = Servicios.Jardin;
            var arboles = jardin.Jardin();

            foreach (var a in jardin.TomarAvisos()) mensajes.Add(Texto(a));

            if (momento == Manana)
            {
                // Anticipa lo bonito: si el check-in de hoy lo hace florecer o dar fruto, se avisa con el dibujo.
                foreach (var v in arboles)
                    if (v.EtapaConCheckInDeHoy is { } nueva && nueva >= GrowthStage.Flor)
                        conImagen.Add((TituloAnticipo(nueva, Min(v.Especie)), "Haz tu check-in de hoy y míralo pasar.", v, nueva));
                foreach (var v in arboles.Where(v => v.EsCheatDayHoy))
                    mensajes.Add(($"☀️ Hoy tu {Min(v.Especie)} descansa", "Disfruta tu día libre con medida: un día libre no es un día sin freno."));
            }
            else if (arboles.Any(v => v.PideCheckInHoy))
            {
                mensajes.Add(Recordatorios[jardin.Hoy.DayNumber % Recordatorios.Length]);
            }

            if (jardin.TomarLogrosNuevos() > 0) mensajes.Add((Avisos.TituloLogro, Avisos.CuerpoLogro));
        }

        var context = Android.App.Application.Context;
        foreach (var (titulo, texto) in mensajes) Mostrar(context, titulo, texto);
        foreach (var (titulo, texto, arbol, etapa) in conImagen) Mostrar(context, titulo, texto, Dibujo(arbol, etapa));
    }

    private static string TituloAnticipo(GrowthStage etapa, string e) => etapa switch
    {
        GrowthStage.Flor => $"🌸 Hoy puede florecer tu {e}",
        GrowthStage.FrutaInmadura => $"🍏 Hoy tu {e} puede dar sus primeros frutos",
        GrowthStage.FrutaComestible => $"🍎 Hoy maduran los frutos de tu {e}",
        _ => $"✨ Hoy tu {e} puede dar una fruta legendaria",
    };

    /// <summary>El árbol ya en su etapa nueva, para la imagen grande del aviso. Null si no se pudo dibujar.</summary>
    private static Android.Graphics.Bitmap? Dibujo(VistaArbol v, GrowthStage etapa)
    {
        try
        {
            var estado = Dibujo_.EstadoArbol.De(v);
            estado.Etapa = etapa;
            var ctx = new Microsoft.Maui.Graphics.Platform.PlatformBitmapExportContext(720, 360, 1f, disposeBitmap: false);
            ctx.Canvas.FillColor = Color.FromArgb("#F4EEDF");
            ctx.Canvas.FillRectangle(0, 0, 720, 360);
            new Dibujo_.ArbolPequeno(estado).Draw(ctx.Canvas, new RectF(180, 20, 360, 330));
            return ctx.Bitmap;
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("JardinDeHabitos", $"No se pudo dibujar el aviso: {ex.Message}");
            return null;
        }
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
        try { Notificaciones.Revisar(intent?.GetIntExtra(Notificaciones.ExtraMomento, Notificaciones.Manana) ?? Notificaciones.Manana); }
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
