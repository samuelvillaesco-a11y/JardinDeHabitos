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

    public JardinPage()
    {
        Title = "";
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);

        var logros = Tema.Boton("Ver logros", Tema.MusgoClaro, Tema.Hueso);
        logros.Clicked += async (_, _) => await Navigation.PushAsync(new LogrosPage());

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 28, 18, 28),
                Spacing = 14,
                Children =
                {
                    Tema.Titulo("Tu jardín", 34),
                    _fecha,
                    _aviso,
                    _cuadricula,
                    logros,
#if DEBUG
                    BotonGaleria(),
#endif
                },
            },
        };
    }

#if DEBUG
    private Button BotonGaleria()
    {
        var b = Tema.Boton("Galería de etapas (debug)", Colors.Transparent, Tema.HuesoTenue);
        b.Clicked += async (_, _) => await Navigation.PushAsync(new GaleriaPage());
        return b;
    }
#endif

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AlVolver();
    }

    public async void AlVolver()
    {
        try
        {
            var jardin = Servicios.Jardin;
            var nuevos = jardin.Refrescar();
            Pintar(jardin.Jardin(), jardin.Hoy);

            foreach (var logro in nuevos)
            {
                var info = LogroInfo.De(logro);
                await DisplayAlertAsync($"Logro: {info.Nombre}", info.Descripcion, "Seguir");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("No se pudo leer el jardín", ex.Message, "Entendido");
        }
    }

    private void Pintar(IReadOnlyList<VistaArbol> arboles, DateOnly hoy)
    {
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
            { EsCheatDayHoy: true } => "Día libre",
            { Penalizacion: PenaltyLevel.Strike2 } => "Raíces podridas",
            { Penalizacion: PenaltyLevel.Strike1 } => "Con plaga",
            _ => $"Día {v.Dias}, {Tema.NombreEtapa(v.Etapa).ToLower(Tema.Cultura)}",
        };

        var pie = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
        };
        pie.Add(new VerticalStackLayout
        {
            Spacing = 0,
            Children = { Tema.Texto(v.Nombre, 16, Tema.FuenteNegrita), Tema.Texto(estado, 13, color: Tema.HuesoTenue) },
        });
        if (v.PideCheckInHoy)
        {
            pie.Add(new Ellipse { Fill = Tema.Miel, WidthRequest = 12, HeightRequest = 12, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) }, 1);
        }

        var parcela = Tema.Parcela(new VerticalStackLayout { Spacing = 6, Children = { dibujo, pie } });
        SemanticProperties.SetDescription(parcela, $"{v.Nombre}. {estado}.");
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await Navigation.PushAsync(new ArbolPage(v.Id));
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
                Tema.Texto("Un árbol por cada hábito que quieras dejar.", 13, color: Tema.HuesoTenue),
            },
        };
        var parcela = Tema.Parcela(contenido, Colors.Transparent, Tema.MusgoClaro);
        parcela.MinimumHeightRequest = 190;
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await Navigation.PushAsync(new NuevoHabitoPage());
        parcela.GestureRecognizers.Add(toque);
        return parcela;
    }
}
