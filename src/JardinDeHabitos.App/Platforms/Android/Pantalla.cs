using Android.Views;

namespace JardinDeHabitos.App;

/// <summary>Una página con datos personales: nunca se deja capturar ni se ve en "apps recientes".</summary>
public interface IPrivada;

/// <summary>
/// Una página bajo llave: al volver de segundo plano (pasado el periodo de gracia) se cierra sola.
/// Las páginas apiladas se quitan de la navegación; una pestaña raíz (el blog) se cierra con <see cref="Cerrar"/>.
/// </summary>
public interface IConLlave
{
    void Cerrar() { }
}

/// <summary>
/// Protección de capturas (FLAG_SECURE). No la decide cada página: se calcula a partir de lo que
/// de verdad está en pantalla (seguro general activo o página visible privada). Así no importa en
/// qué orden aparecen y desaparecen las páginas.
/// </summary>
public static class Pantalla
{
    /// <summary>
    /// Vuelve a calcular la protección cuando termine la navegación en curso. Se encola con el
    /// Dispatcher (MainThread.BeginInvokeOnMainThread correría al instante, antes de cambiar de página).
    /// </summary>
    public static void Actualizar()
    {
        if (Application.Current?.Dispatcher is { } d) d.Dispatch(Aplicar);
        else Aplicar();
    }

    private static void Aplicar()
    {
        var ventana = Platform.CurrentActivity?.Window;
        if (ventana is null) return;
        bool privada = Seguridad.PantallaDebeSerPrivada || PaginaVisible() is IPrivada;
        if (privada) ventana.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        else ventana.ClearFlags(WindowManagerFlags.Secure);
        // Detrás de la barra de estado se ve la ventana: que siga la paleta (papel o noche).
        ventana.DecorView.SetBackgroundColor(Microsoft.Maui.Platform.ColorExtensions.ToPlatform(Tema.Musgo));
        new AndroidX.Core.View.WindowInsetsControllerCompat(ventana, ventana.DecorView).AppearanceLightStatusBars = !Tema.Oscuro;
    }

    /// <summary>La página que el usuario ve: el modal de arriba o la página actual de la pestaña actual.</summary>
    public static Page? PaginaVisible()
    {
        var raiz = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (raiz is null) return null;
        return Hoja(raiz.Navigation.ModalStack.LastOrDefault() ?? raiz);
    }

    private static Page Hoja(Page p) => p switch
    {
        TabbedPage t when t.CurrentPage is { } actual => Hoja(actual),
        NavigationPage n when n.CurrentPage is { } actual => Hoja(actual),
        _ => p,
    };
}
