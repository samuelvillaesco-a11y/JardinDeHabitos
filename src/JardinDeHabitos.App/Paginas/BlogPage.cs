using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Blog botánico: tu diario personal y el registro de cómo te sientes. Va bajo llave.
/// (El diario y el registro de emociones llegan en la parte 3; por ahora, la puerta y el candado.)
/// </summary>
public sealed class BlogPage : ContentPage, IPrivada, IConLlave
{
    private bool _abierto;

    // La pantalla del candado tapa esta página: eso no cuenta como salir ni como volver.
    private bool _verificando;
    private readonly VerticalStackLayout _pila = new() { Padding = new Thickness(18, 28, 18, 28), Spacing = 14 };

    public BlogPage()
    {
        Title = "Blog botánico";
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);
        Content = new ScrollView { Content = _pila };
        PintarCerrado();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
        Pantalla.Actualizar();
        if (!_abierto && !_verificando) await Abrir();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Al salir de la pestaña, se vuelve a cerrar con llave.
        if (_verificando) return;
        Cerrar();
    }

    /// <summary>Vuelve a cerrar el blog con llave (al salir de la pestaña o al volver de segundo plano).</summary>
    public void Cerrar()
    {
        _abierto = false;
        PintarCerrado();
    }

    private async Task Abrir()
    {
        if (_verificando) return;
        _verificando = true;
        bool ok;
        try { ok = await Seguridad.EntrarAsync(this, Seguro.Blog, "Tu blog botánico"); }
        finally { _verificando = false; }
        if (!ok) return;
        _abierto = true;
        _pila.Clear();
        _pila.Add(Tema.Titulo("📖 Blog botánico", 34));
        _pila.Add(Tema.Texto("Tu diario personal, solo para ti.", 16, color: Tema.HuesoTenue));
        _pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Tema.Texto("Muy pronto", 17, Tema.FuenteNegrita, Tema.Miel),
                Tema.Texto("Aquí podrás escribir tu diario y registrar cada día cómo te sentiste: emociones, ganas de recaer y síntomas físicos, con gráficas de tu avance.", 15),
            },
        }));
    }

    private void PintarCerrado()
    {
        _pila.Clear();
        _pila.Add(Tema.Titulo("📖 Blog botánico", 34));
        var abrir = Tema.Boton("Abrir mi blog", Tema.Miel);
        abrir.Clicked += async (_, _) => await Abrir();
        _pila.Add(Tema.Texto("Tu diario está bajo llave.", 16, color: Tema.HuesoTenue));
        _pila.Add(abrir);
    }
}
