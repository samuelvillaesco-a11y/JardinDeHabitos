using Microsoft.Maui.Controls.Shapes;
using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>El jardín: una parcela por hábito, en cuadrícula de dos columnas.</summary>
public sealed class JardinPage : ContentPage, IRefrescable
{
    private readonly Grid _cuadricula = new()
    {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
        ColumnSpacing = 12,
        RowSpacing = 12,
    };
    private readonly Label _fecha = Tema.Texto("", 15, color: Tema.HuesoTenue);
    private readonly Label _aviso = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private IDispatcherTimer? _timer;
    private DateOnly _hoyMostrado;

    public JardinPage()
    {
        Title = "";
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);

        // Solo un trofeo: nada dice qué hay adentro.
        var logros = Tema.Boton(Iconos.Trofeo, Tema.MusgoClaro, Tema.Hueso);
        logros.FontFamily = Tema.FuenteIconos;
        logros.FontSize = 24;
        logros.WidthRequest = 60;
        logros.HeightRequest = 56;
        logros.Padding = 0;
        logros.HorizontalOptions = LayoutOptions.End;
        logros.VerticalOptions = LayoutOptions.Start;
        SemanticProperties.SetDescription(logros, "Estante de trofeos");
        logros.Clicked += async (_, _) => await this.Verificado("Tu estante de trofeos", () => new LogrosPage(), Seguro.Logros);

        // El trofeo vive en la esquina, junto al título.
        var encabezado = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        encabezado.Add(Tema.Titulo("Tu jardín", 34), 0);
        encabezado.Add(logros, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 28, 18, 28),
                Spacing = 14,
                Children =
                {
                    encabezado,
                    _fecha,
                    _aviso,
                    _cuadricula,
                },
            },
        };
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
        AlVolver();
        // Si la pantalla queda abierta al pasar la medianoche, se relee sola.
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(20);
        _timer.Tick += (_, _) => { if (Servicios.Jardin.Hoy != _hoyMostrado) AlVolver(); };
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    public async void AlVolver()
    {
        try
        {
            var jardin = Servicios.Jardin;
            Pintar(jardin.Jardin(), jardin.Hoy);
            if (Servicios.TomarAvisoDeDatosApartados())
                await DisplayAlertAsync("Tu jardín empezó de cero",
                    "Android perdió la llave que protegía tus datos y ya no se podían abrir. Para que la app siga funcionando, se guardaron aparte en el teléfono y empezaste con un jardín nuevo.",
                    "Entendido");
            await Avisos.AnunciarLogros(this);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("No se pudo leer el jardín", ex.Message, "Entendido");
        }
    }

    private void Pintar(IReadOnlyList<VistaArbol> arboles, DateOnly hoy)
    {
        _hoyMostrado = hoy;
        _fecha.Text = char.ToUpper(Tema.Fecha(hoy)[0]) + Tema.Fecha(hoy)[1..];
        int pendientes = arboles.Count(a => a.PideCheckInHoy);
        _aviso.Text = pendientes switch
        {
            0 => "",
            1 => "Un árbol espera tu check-in de hoy.",
            _ => $"{pendientes} árboles esperan tu check-in de hoy.",
        };
        _aviso.IsVisible = pendientes > 0;

        _cuadricula.Children.Clear();
        _cuadricula.RowDefinitions.Clear();

        var celdas = arboles.Select(ParcelaArbol).ToList();
        if (arboles.Count < GardenService.MaxHabitos) celdas.Add(ParcelaVacia(arboles.Count == 0));

        for (int i = 0; i < celdas.Count; i++)
        {
            if (i % 2 == 0) _cuadricula.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _cuadricula.Add(celdas[i], i % 2, i / 2);
        }
    }

    private View ParcelaArbol(VistaArbol v)
    {
        var dibujo = new GraphicsView { Drawable = new ArbolPequeno(EstadoArbol.De(v)), HeightRequest = 130, InputTransparent = true };
        var estado = v switch
        {
            { MiniEvento: not MiniEvento.Ninguno } => "¡Necesita ayuda!",
            { EsCheatDayHoy: true } => "Día libre",
            { Raices: true } => "Raíces podridas",
            { Plaga: true } => "Con plaga",
            { DiasSinVisita: > 0 } => $"En pausa, día {v.Dias}",
            _ => $"Día {v.Dias}, {Tema.NombreEtapa(v.Etapa).ToLower(Tema.Cultura)}",
        };

        var pie = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
        };
        pie.Add(new VerticalStackLayout
        {
            Spacing = 0,
            Children = { Tema.Texto(v.Especie, 16, Tema.FuenteNegrita), Tema.Texto(estado, 14, color: Tema.HuesoTenue) },
        });
        if (v.PideCheckInHoy)
        {
            pie.Add(new Ellipse { Fill = Tema.Miel, WidthRequest = 12, HeightRequest = 12, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) }, 1);
        }

        var parcela = Tema.Parcela(new VerticalStackLayout { Spacing = 6, Children = { dibujo, pie } });
        SemanticProperties.SetDescription(parcela, $"{v.Especie}. {estado}.");
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await this.UnaVez(() => new ArbolPage(v.Id));
        parcela.GestureRecognizers.Add(toque);
        return parcela;
    }

    private View ParcelaVacia(bool primera)
    {
        var contenido = new VerticalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Tema.Texto("+", 40, Tema.FuenteTitulo, Tema.Hoja),
                Tema.Texto(primera ? "Planta tu primer árbol" : "Plantar otro árbol", 15, Tema.FuenteNegrita),
                Tema.Texto("Un árbol por cada hábito que quieras dejar. Solo tú sabrás cuál es cuál.", 14, color: Tema.HuesoTenue),
            },
        };
        // Un hueco en la página del cuaderno: borde punteado con contraste suficiente (≥ 3:1) para verse como botón.
        var parcela = Tema.Parcela(contenido, Colors.Transparent, Tema.HuesoTenue);
        parcela.StrokeThickness = 1.5;
        parcela.StrokeDashArray = [4, 3];
        parcela.MinimumHeightRequest = 190;
        SemanticProperties.SetDescription(parcela, primera ? "Planta tu primer árbol" : "Plantar otro árbol");
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await this.UnaVez(() => new NuevoHabitoPage());
        parcela.GestureRecognizers.Add(toque);
        return parcela;
    }
}
