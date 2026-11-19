using Android.Views;

namespace JardinDeHabitos.App;

/// <summary>Marca la ventana como segura: sin capturas de pantalla y en blanco en "apps recientes".</summary>
public static class Pantalla
{
    public static void Privada(bool privada)
    {
        var ventana = Platform.CurrentActivity?.Window;
        if (ventana is null) return;
        if (privada) ventana.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        else ventana.ClearFlags(WindowManagerFlags.Secure);
    }
}
