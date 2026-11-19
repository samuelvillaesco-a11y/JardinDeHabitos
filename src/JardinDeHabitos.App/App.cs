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

    private static NavigationPage Jardin() =>
        new(new JardinPage())
        {
            BarBackgroundColor = Tema.Musgo,
            BarTextColor = Tema.Hueso,
        };

    protected override void OnSleep()
    {
        base.OnSleep();
        // La pantalla del PIN también manda la app al fondo: eso no cuenta como salir.
        if (!Bloqueo.EnCurso) _salioAlFondo = DateTime.UtcNow;
    }

    /// <summary>
    /// Al volver: con el seguro general, se cierra con llave otra vez. Si no, se relee todo
    /// (la app suele quedar en segundo plano de un día a otro).
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();
        bool salio = _salioAlFondo is not null;
        _salioAlFondo = null;
        if (Windows.FirstOrDefault() is not { } ventana) return;

        if (salio && Seguridad.BienvenidaHecha && Seguridad.Activo(Seguro.General) && ventana.Page is not CerrojoPage)
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

    private static void Refrescar(Page? pagina)
    {
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
                Pantalla.Privada(Seguridad.PantallaDebeSerPrivada);
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
