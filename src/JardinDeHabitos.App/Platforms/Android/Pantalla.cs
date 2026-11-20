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
    /// <summary>Vuelve a calcular la protección cuando termine la navegación en curso.</summary>
    public static void Actualizar() => MainThread.BeginInvokeOnMainThread(Aplicar);

    private static void Aplicar()
    {
        var ventana = Platform.CurrentActivity?.Window;
        if (ventana is null) return;
        bool privada = Seguridad.PantallaDebeSerPrivada || PaginaVisible() is IPrivada;
        if (privada) ventana.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        else ventana.ClearFlags(WindowManagerFlags.Secure);
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
