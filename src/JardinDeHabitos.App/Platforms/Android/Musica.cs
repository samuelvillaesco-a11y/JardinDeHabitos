using Android.Media;

namespace JardinDeHabitos.App;

/// <summary>
/// Música de fondo del jardín, en bucle y a volumen bajo. Se pausa al salir de la app y vuelve al
/// regresar. Se apaga y prende en Ajustes. Pista: "Forest Ambience" de TinyWorlds (CC0, dominio público).
/// </summary>
public static class Musica
{
    private const string ClaveEncendida = "ajustes.musica";
    private const string Archivo = "bosque.mp3";
    private const float Volumen = 0.35f;
    private static MediaPlayer? _reproductor;

    public static bool Encendida
    {
        get => Preferences.Default.Get(ClaveEncendida, true);
        set
        {
            Preferences.Default.Set(ClaveEncendida, value);
            if (value) Sonar();
            else Pausar();
        }
    }

    public static void Sonar()
    {
        if (!Encendida) return;
        try
        {
            _reproductor ??= Crear();
            if (!_reproductor.IsPlaying) _reproductor.Start();
        }
        catch (Exception ex)
        {
            // Sin música no pasa nada: el jardín sigue funcionando.
            Android.Util.Log.Warn("JardinDeHabitos", $"Música no disponible: {ex.Message}");
            _reproductor?.Release();
            _reproductor = null;
        }
    }

    public static void Pausar()
    {
        try
        {
            if (_reproductor?.IsPlaying == true) _reproductor.Pause();
        }
        catch (Java.Lang.IllegalStateException) { }
    }

    private static MediaPlayer Crear()
    {
        var mp = new MediaPlayer();
        using (var fd = Android.App.Application.Context.Assets!.OpenFd(Archivo))
            mp.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
        mp.SetAudioAttributes(new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Game)!
            .SetContentType(AudioContentType.Music)!
            .Build()!);
        mp.Looping = true;
        mp.SetVolume(Volumen, Volumen);
        mp.Prepare();
        return mp;
    }
}
