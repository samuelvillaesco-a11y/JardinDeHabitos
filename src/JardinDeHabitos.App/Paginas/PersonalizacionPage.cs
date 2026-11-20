using JardinDeHabitos.Core;
using Microsoft.Maui.Controls.Shapes;
using ColorMaui = Microsoft.Maui.Graphics.Color;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Personalización del fondo: colores calmantes recomendados, degradados, escenas dibujadas,
/// un degradado propio de 2 colores y un selector de color libre.
/// </summary>
public sealed class PersonalizacionPage : ContentPage
{
    private enum Destino { Solido, ColorA, ColorB }

    private readonly CapaFondo _muestra;
    private readonly VerticalStackLayout _selector = new() { Spacing = 10, IsVisible = false };
    private readonly Slider _tono = new(0, 360, 90) { MinimumTrackColor = Tema.Miel, ThumbColor = Tema.Hueso };
    private readonly Slider _saturacion = new(0, 1, 0.4) { MinimumTrackColor = Tema.Miel, ThumbColor = Tema.Hueso };
    private readonly Slider _brillo = new(0, 1, 0.6) { MinimumTrackColor = Tema.Miel, ThumbColor = Tema.Hueso };
    private readonly Entry _hex = new() { FontFamily = Tema.Fuente, FontSize = 18, TextColor = Tema.Hueso, BackgroundColor = Tema.MusgoClaro, MaxLength = 7, Keyboard = Keyboard.Plain };
    private readonly BoxView _vistaColor = new() { HeightRequest = 48, CornerRadius = 12 };
    private readonly Label _tituloSelector = Tema.Texto("", 17, Tema.FuenteNegrita);
    private readonly Border _botonA;
    private readonly Border _botonB;
    private readonly Border _barra;
    private string _colorA = "#F4C76E";
    private string _colorB = "#6E7E3A";
    private Destino _destino;
    private bool _moviendo;

    public PersonalizacionPage()
    {
        Title = "Personalización";
        BackgroundColor = Tema.Musgo;

        var actual = Apariencia.Actual;
        if (actual.Tipo == TipoFondo.Degradado) (_colorA, _colorB) = (actual.ColorA, actual.ColorB);

        _muestra = new CapaFondo(new VerticalStackLayout
        {
            Padding = 16,
            VerticalOptions = LayoutOptions.Center,
            Children = { Tema.Titulo("Tu jardín", 26), Tema.Texto("Así se verá el fondo.", 15, color: Tema.HuesoTenue) },
        }) { HeightRequest = 140 };
        _muestra.Pintar(actual);

        _botonA = Muestra(_colorA, 56, 56, 28);
        _botonB = Muestra(_colorB, 56, 56, 28);
        _barra = new Border { HeightRequest = 40, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 20 } };
        PintarDegradadoPropio();
        Tocable(_botonA, () => AbrirSelector(Destino.ColorA, _colorA));
        Tocable(_botonB, () => AbrirSelector(Destino.ColorB, _colorB));
        SemanticProperties.SetDescription(_botonA, "Color de arriba del degradado");
        SemanticProperties.SetDescription(_botonB, "Color de abajo del degradado");

        ArmarSelector();

        var usarDegradado = Tema.Boton("Usar mi degradado", Tema.MusgoClaro, Tema.Hueso);
        usarDegradado.Clicked += (_, _) => Elegir(Fondo.Degradado(_colorA, _colorB));
        var colorLibre = Tema.Boton("Elegir cualquier color", Tema.MusgoClaro, Tema.Hueso);
        colorLibre.Clicked += (_, _) => AbrirSelector(Destino.Solido, actual.ColorA);
        var restablecer = Tema.Boton("Volver al verde musgo", Colors.Transparent, Tema.HuesoTenue);
        restablecer.Clicked += (_, _) => Elegir(Fondo.PorDefecto);

        var filaPropio = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        filaPropio.Add(_botonA, 0);
        _barra.VerticalOptions = LayoutOptions.Center;
        filaPropio.Add(_barra, 1);
        filaPropio.Add(_botonB, 2);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 20, 18, 28),
                Spacing = 14,
                Children =
                {
                    Tema.Titulo("🎨 Personalización", 30),
                    new Border { Content = _muestra, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 20 } },
                    Seccion("Colores recomendados", "Tonos calmantes de bosque, otoño y miel."),
                    Rejilla(Paleta.Solidos.Select(s => (View)Ficha(Muestra(s.Color, 56, 56, 28), s.Nombre, Fondo.Solido(s.Color)))),
                    Seccion("Degradados", null),
                    Rejilla(Paleta.Degradados.Select(d => (View)Ficha(Muestra(d.A, 96, 64, 16, d.B), d.Nombre, Fondo.Degradado(d.A, d.B)))),
                    Seccion("Escenas", "Dibujadas para el jardín."),
                    Rejilla(Enum.GetValues<EscenaFondo>().Select(e => (View)Ficha(Escenario(e), Paleta.NombreEscena(e), Fondo.DeEscena(e)))),
                    Seccion("Tu propio degradado", "Toca cada círculo para elegir sus colores."),
                    filaPropio,
                    usarDegradado,
                    Seccion("Color libre", null),
                    colorLibre,
                    _selector,
                    restablecer,
                },
            },
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
    }

    private void Elegir(Fondo f)
    {
        Apariencia.Elegir(f);
        _muestra.Pintar(f);
        Apariencia.Aplicar(this);
    }

    // ---------- Piezas ----------

    private static Label Seccion(string titulo, string? detalle)
    {
        var l = Tema.Texto(titulo, 19, Tema.FuenteTitulo, Tema.Hoja);
        if (detalle is not null) l.Text = $"{titulo}\n";
        if (detalle is null) return l;
        l.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = titulo + "\n", FontFamily = Tema.FuenteTitulo, FontSize = 19, TextColor = Tema.Hoja },
                new Span { Text = detalle, FontFamily = Tema.Fuente, FontSize = 14, TextColor = Tema.HuesoTenue },
            },
        };
        return l;
    }

    private static FlexLayout Rejilla(IEnumerable<View> fichas)
    {
        var f = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var v in fichas) f.Children.Add(v);
        return f;
    }

    private static Border Muestra(string a, double ancho, double alto, double radio, string? b = null) => new()
    {
        WidthRequest = ancho,
        HeightRequest = alto,
        Stroke = Tema.HuesoTenue,
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = radio },
        Background = b is null
            ? new SolidColorBrush(ColorMaui.FromArgb(a))
            : new LinearGradientBrush([new GradientStop(ColorMaui.FromArgb(a), 0), new GradientStop(ColorMaui.FromArgb(b), 1)], new Point(0, 0), new Point(0, 1)),
    };

    private static Border Escenario(EscenaFondo e) => new()
    {
        WidthRequest = 96,
        HeightRequest = 150,
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Content = new Image { Source = Apariencia.ImagenDe(e), Aspect = Aspect.AspectFill },
    };

    private View Ficha(View muestra, string nombre, Fondo fondo)
    {
        var etiqueta = Tema.Texto(nombre, 12, color: Tema.HuesoTenue);
        etiqueta.HorizontalTextAlignment = TextAlignment.Center;
        etiqueta.WidthRequest = Math.Max(muestra.WidthRequest, 72);
        var pila = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 0, 10, 10), Children = { muestra, etiqueta } };
        muestra.HorizontalOptions = LayoutOptions.Center;
        SemanticProperties.SetDescription(pila, nombre);
        Tocable(pila, () => Elegir(fondo));
        return pila;
    }

    private static void Tocable(View v, Action accion)
    {
        var t = new TapGestureRecognizer();
        t.Tapped += (_, _) => accion();
        v.GestureRecognizers.Add(t);
    }

    private void PintarDegradadoPropio()
    {
        _botonA.Background = new SolidColorBrush(ColorMaui.FromArgb(_colorA));
        _botonB.Background = new SolidColorBrush(ColorMaui.FromArgb(_colorB));
        _barra.Background = new LinearGradientBrush(
            [new GradientStop(ColorMaui.FromArgb(_colorA), 0), new GradientStop(ColorMaui.FromArgb(_colorB), 1)],
            new Point(0, 0.5), new Point(1, 0.5));
    }

    // ---------- Selector de color (tono, saturación, brillo o hexadecimal) ----------

    private void ArmarSelector()
    {
        var listo = Tema.Boton("Listo", Tema.Miel);
        var cancelar = Tema.Boton("Cancelar", Colors.Transparent, Tema.HuesoTenue);
        listo.Clicked += (_, _) => AceptarSelector();
        cancelar.Clicked += (_, _) => _selector.IsVisible = false;
        _tono.ValueChanged += (_, _) => DesdeDeslizadores();
        _saturacion.ValueChanged += (_, _) => DesdeDeslizadores();
        _brillo.ValueChanged += (_, _) => DesdeDeslizadores();
        _hex.TextChanged += (_, e) => DesdeHex(e.NewTextValue);
        SemanticProperties.SetDescription(_tono, "Tono");
        SemanticProperties.SetDescription(_saturacion, "Saturación");
        SemanticProperties.SetDescription(_brillo, "Brillo");
        SemanticProperties.SetDescription(_hex, "Color en hexadecimal");

        var arcoiris = new Border
        {
            HeightRequest = 10,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            Background = new LinearGradientBrush(
                [.. Enumerable.Range(0, 7).Select(i => new GradientStop(ColorMaui.FromArgb(ColorHex.DesdeHsv(i * 60, 0.6, 0.85)), i / 6f))],
                new Point(0, 0.5), new Point(1, 0.5)),
        };

        _selector.Children.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _tituloSelector,
                _vistaColor,
                Tema.Texto("Tono", 14, color: Tema.HuesoTenue), arcoiris, _tono,
                Tema.Texto("Saturación", 14, color: Tema.HuesoTenue), _saturacion,
                Tema.Texto("Brillo", 14, color: Tema.HuesoTenue), _brillo,
                Tema.Texto("Código (#RRGGBB)", 14, color: Tema.HuesoTenue), _hex,
                listo, cancelar,
            },
        }));
    }

    private void AbrirSelector(Destino destino, string inicial)
    {
        _destino = destino;
        _tituloSelector.Text = destino switch
        {
            Destino.ColorA => "Color de arriba",
            Destino.ColorB => "Color de abajo",
            _ => "Color del fondo",
        };
        PonerColor(inicial);
        _selector.IsVisible = true;
    }

    private void PonerColor(string hex)
    {
        _moviendo = true;
        var (h, s, v) = ColorHex.Hsv(hex);
        _tono.Value = h;
        _saturacion.Value = s;
        _brillo.Value = v;
        _hex.Text = hex;
        _vistaColor.Color = ColorMaui.FromArgb(hex);
        _moviendo = false;
    }

    private void DesdeDeslizadores()
    {
        if (_moviendo) return;
        var hex = ColorHex.DesdeHsv(_tono.Value, _saturacion.Value, _brillo.Value);
        _moviendo = true;
        _hex.Text = hex;
        _vistaColor.Color = ColorMaui.FromArgb(hex);
        _moviendo = false;
    }

    private void DesdeHex(string? texto)
    {
        if (_moviendo || !ColorHex.EsValido(texto)) return;
        PonerColor(ColorHex.Normalizar(texto!));
    }

    private void AceptarSelector()
    {
        if (!ColorHex.EsValido(_hex.Text)) return;
        var hex = ColorHex.Normalizar(_hex.Text!);
        switch (_destino)
        {
            case Destino.ColorA: _colorA = hex; PintarDegradadoPropio(); break;
            case Destino.ColorB: _colorB = hex; PintarDegradadoPropio(); break;
            default: Elegir(Fondo.Solido(hex)); break;
        }
        _selector.IsVisible = false;
    }
}
