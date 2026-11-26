using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Portada del seguro general: tapa todo el jardín hasta que se verifica al dueño
/// (clave de la app y/o bloqueo del teléfono). No muestra nada del jardín.
/// </summary>
public sealed class CerrojoPage : ContentPage, IPrivada
{
    private readonly Action _alAbrir;
    private bool _pidiendo;
    private bool _yaPidio;

    public CerrojoPage(Action alAbrir)
    {
        _alAbrir = alAbrir;
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);

        var abrir = Tema.Boton("Desbloquear", Tema.Miel);
        abrir.Clicked += async (_, _) => await Pedir();

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24, 0),
            Spacing = 16,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Tema.Icono(Iconos.Candado, 52),
                Tema.Titulo("Tu jardín está bajo llave", 28),
                Tema.Texto("Verifica que eres tú para entrar.", 16, color: Tema.HuesoTenue),
                abrir,
            },
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Actualizar();
        if (_yaPidio) return;
        _yaPidio = true;
        await Pedir();
    }

    // "Atrás" no salta el cerrojo.
    protected override bool OnBackButtonPressed() => true;

    private async Task Pedir()
    {
        if (_pidiendo) return;
        _pidiendo = true;
        try
        {
            if (await Seguridad.VerificarAsync(this, "Jardín de Hábitos"))
            {
                Pantalla.Actualizar();
                _alAbrir();
            }
        }
        finally { _pidiendo = false; }
    }
}
