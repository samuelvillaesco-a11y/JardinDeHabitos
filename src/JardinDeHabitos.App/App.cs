using JardinDeHabitos.App.Paginas;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

public class App : Application
{
    private DateTime? _salioAlFondo;

    public App()
    {
        UserAppTheme = AppTheme.Dark;
    }

    /// <summary>La app: tres pestañas abajo (Jardín, Blog botánico, Ajustes). Los logros se abren desde el jardín.</summary>
    private static Page Jardin()
    {
        var pestanas = new TabbedPage
        {
            BarBackgroundColor = Tema.MusgoClaro,
            BarTextColor = Tema.Hueso,
            SelectedTabColor = Tema.Miel,
            UnselectedTabColor = Tema.HuesoTenue,
            Children =
            {
                Pestana(new JardinPage(), "Jardín", "🌳"),
                Pestana(new BlogPage(), "Blog botánico", "📖"),
                Pestana(new AjustesPage(), "Ajustes", "⚙️"),
            },
        };
        pestanas.CurrentPageChanged += (_, _) => Pantalla.Actualizar();
        // Sin deslizar entre pestañas: el deslizamiento horizontal se robaba los arrastres de los
        // mini-juegos (regadera, raíces, poda). Las pestañas se cambian tocando la barra.
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetIsSwipePagingEnabled(pestanas, false);
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(
            pestanas, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
        return pestanas;
    }

    private static NavigationPage Pestana(Page raiz, string titulo, string icono) =>
        new(raiz)
        {
            Title = titulo,
            IconImageSource = new FontImageSource { Glyph = icono, Size = 22, Color = Tema.Hueso },
            BarBackgroundColor = Tema.Musgo,
            BarTextColor = Tema.Hueso,
        };

    protected override void OnStart()
    {
        base.OnStart();
        Musica.Sonar();
    }

    /// <summary>
    /// Ausencias breves (el diálogo de huella/PIN del sistema, elegir un contacto) no cierran nada.
    /// Más tiempo fuera sí: se cierran las secciones con llave y, con el seguro general, toda la app.
    /// </summary>
    private static readonly TimeSpan Gracia = TimeSpan.FromSeconds(10);

    protected override void OnSleep()
    {
        base.OnSleep();
        _salioAlFondo = DateTime.UtcNow;
        Musica.Pausar();
    }

    /// <summary>
    /// Al volver de una ausencia larga: se cierran las secciones bajo llave y, con el seguro
    /// general, toda la app. Si no, solo se relee (la app suele quedar abierta de un día a otro).
    /// </summary>
    protected override async void OnResume()
    {
        base.OnResume();
        Musica.Sonar();
        var salio = _salioAlFondo;
        _salioAlFondo = null;
        if (Windows.FirstOrDefault() is not { } ventana) return;

        // Si mientras estaba fuera el usuario se verificó con el bloqueo del teléfono, no fue una salida.
        bool larga = salio is { } t && DateTime.UtcNow - t > Gracia && !(Bloqueo.UltimoExito > t);
        if (larga && Seguridad.BienvenidaHecha)
            await CerrarSeccionesConLlave(ventana);

        if (larga && Seguridad.BienvenidaHecha && Seguridad.Activo(Seguro.General) && ventana.Page is not CerrojoPage)
        {
            var anterior = ventana.Page;
            ventana.Page = new CerrojoPage(() =>
            {
                ventana.Page = anterior;
                Refrescar(anterior);
            });
            return;
        }
        Refrescar(ventana.Page);
    }

    /// <summary>Cierra modales abiertos y saca de la navegación toda página bajo llave.</summary>
    private static async Task CerrarSeccionesConLlave(Window ventana)
    {
        var raiz = ventana.Page;
        if (raiz is null) return;
        while (raiz.Navigation.ModalStack.Count > 0) await raiz.Navigation.PopModalAsync(false);
        foreach (var pila in (raiz as TabbedPage)?.Children.OfType<NavigationPage>() ?? [])
        {
            // La pila de la pestaña se lee desde su página raíz (la del NavigationPage es la del padre).
            if (pila.RootPage.Navigation.NavigationStack.Skip(1).Any(p => p is IConLlave))
                await pila.PopToRootAsync(false);
            if (pila.RootPage is IConLlave raizConLlave) raizConLlave.Cerrar();
        }
        Pantalla.Actualizar();
    }

    private static void Refrescar(Page? pagina)
    {
        if (pagina is TabbedPage t) pagina = t.CurrentPage;
        if (pagina is NavigationPage nav && nav.CurrentPage is IRefrescable r) r.AlVolver();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var ventana = new Window();
        if (!Seguridad.BienvenidaHecha)
        {
            // Primera vez: crear clave de la app (opcional; "Ahora no" la salta).
            ventana.Page = new CrearClavePage(CrearClavePage.Motivo.PrimeraVez, () =>
            {
                Seguridad.MarcarBienvenida();
                Pantalla.Actualizar();
                ventana.Page = Jardin();
                return Task.CompletedTask;
            });
        }
        else if (Seguridad.Activo(Seguro.General))
            ventana.Page = new CerrojoPage(() => ventana.Page = Jardin());
        else
            ventana.Page = Jardin();
        return ventana;
    }
}
