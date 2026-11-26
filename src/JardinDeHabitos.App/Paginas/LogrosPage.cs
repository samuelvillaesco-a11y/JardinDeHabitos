using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// El estante de trofeos (se abre con el bloqueo del teléfono). Solo emblemas: lo que significa cada uno
/// se ve al tocarlo. Un estante por categoría (tu jardín, uno por árbol, y mini-juegos/malditos/legendarios
/// solo si ya hay alguno ganado), con los emblemas en orden alfabético. Los filtros viven en una hoja que
/// sube desde abajo solo cuando el usuario la pide.
/// </summary>
public sealed class LogrosPage : ContentPage, IPrivada, IConLlave
{
    // Mientras el estante está abierto: sin capturas y oculto en "apps recientes".
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
        Pantalla.Actualizar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Pantalla.Actualizar();
    }

    private enum Orden { Categoria, NuevosPrimero, Recientes, Antiguos }

    private const string Todas = "todas";
    private const string Principales = "principales";
    private const string MiniJuegos = "minijuegos";
    private const string Malditos = "malditos";
    private const string Legendarios = "legendarios";
    private const string PrefijoHabito = "hab:";
    private const string TodoRango = "todos";

    private static readonly Color Madera = Color.FromArgb("#6B4A2E");
    private static readonly Color MaderaOscura = Color.FromArgb("#4A3322");
    private static readonly StringComparer Alfabetico = StringComparer.Create(Tema.Cultura, ignoreCase: true);

    private IReadOnlyList<EntradaEstante> _estante = [];
    private readonly VerticalStackLayout _lista = new() { Padding = new Thickness(18, 8, 18, 28), Spacing = 6 };
    private readonly Button _botonFiltros = Tema.Boton(Iconos.Filtros, "Filtrar", Tema.MusgoClaro, Tema.Hueso);
    private readonly Label _resumenFiltro = Tema.Texto("", 14, color: Tema.HuesoTenue);

    // Hoja de filtros (se desliza desde abajo).
    private readonly Grid _hojaFiltros = new() { IsVisible = false };
    private readonly Border _panelFiltros;
    private readonly VerticalStackLayout _filtros = new() { Spacing = 8 };

    // Detalle de un emblema.
    private readonly Grid _detalle = new() { IsVisible = false };
    private readonly Border _panel;
    private readonly VerticalStackLayout _panelContenido = new() { Spacing = 6 };

    private string _categoria = Todas;
    private string _rango = TodoRango;
    private Orden _orden = Orden.Categoria;

    public LogrosPage()
    {
        Title = "Tu estante";
        BackgroundColor = Tema.Musgo;

        _panel = Hoja(_panelContenido, 620);
        _detalle.Add(Fondo(CerrarDetalle));
        _detalle.Add(_panel);

        _panelFiltros = Hoja(_filtros, 640);
        // Fondo musgo oscuro: así se ve el borde de los chips sin elegir.
        _panelFiltros.Background = Tema.Musgo;
        _panelFiltros.Stroke = Tema.MusgoClaro;
        _panelFiltros.StrokeThickness = 2;
        var deslizar = new SwipeGestureRecognizer { Direction = SwipeDirection.Down };
        deslizar.Swiped += async (_, _) => await CerrarFiltros();
        _panelFiltros.GestureRecognizers.Add(deslizar);
        _hojaFiltros.Add(Fondo(async () => await CerrarFiltros()));
        _hojaFiltros.Add(_panelFiltros);

        _botonFiltros.HorizontalOptions = LayoutOptions.Start;
        _botonFiltros.Clicked += async (_, _) => await AbrirFiltros();
        SemanticProperties.SetDescription(_botonFiltros, "Abrir filtros del estante");

        var barra = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 12,
            Padding = new Thickness(18, 12, 18, 4),
        };
        barra.Add(_botonFiltros, 0);
        _resumenFiltro.VerticalOptions = LayoutOptions.Center;
        barra.Add(_resumenFiltro, 1);

        var contenido = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new VerticalStackLayout
                    {
                        Padding = new Thickness(18, 20, 18, 0),
                        Spacing = 6,
                        Children =
                        {
                            Tema.Titulo("Tu estante", 30),
                            Tema.Texto("Toca un emblema para ver qué significa.", 15, color: Tema.HuesoTenue),
                        },
                    },
                    barra,
                    _lista,
                },
            },
        };

        var raiz = new Grid();
        raiz.Add(contenido);
        raiz.Add(_hojaFiltros);
        raiz.Add(_detalle);
        Content = raiz;

        Cargar();
    }

    private static Border Hoja(View contenido, double alto) => new()
    {
        Content = new ScrollView { Content = contenido },
        Background = Tema.MusgoClaro,
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
        Padding = new Thickness(22, 14, 22, 28),
        VerticalOptions = LayoutOptions.End,
        MaximumHeightRequest = alto,
    };

    private static BoxView Fondo(Action alTocar)
    {
        var fondo = new BoxView { Color = Colors.Black.WithAlpha(0.55f) };
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) => alTocar();
        fondo.GestureRecognizers.Add(toque);
        return fondo;
    }

    private void Cargar()
    {
        _estante = Servicios.Jardin.Estante();
        Pintar();
    }

    // ---------- Categorías ----------

    /// <summary>
    /// Las categorías que existen hoy, en orden estándar: tu jardín, un estante por árbol (alfabético),
    /// y mini-juegos, malditos y legendarios solo si ya hay al menos uno ganado.
    /// </summary>
    private List<(string Clave, string Nombre)> Categorias()
    {
        var lista = new List<(string, string)>();
        if (_estante.Any(e => e.Def.Categoria == CategoriaLogro.General)) lista.Add((Principales, "Tu jardín"));

        var arboles = _estante.Where(e => e.Def.Categoria == CategoriaLogro.Habito)
            .GroupBy(e => e.Def.Habito!.Value)
            .Select(g => (Clave: PrefijoHabito + g.Key, Nombre: NombreArbol(g.Select(e => e.Arbol).FirstOrDefault(a => a is not null))))
            .OrderBy(a => a.Nombre, Alfabetico);
        lista.AddRange(arboles);

        if (_estante.Any(e => e.Def.Categoria == CategoriaLogro.Oculto)) lista.Add((MiniJuegos, "Mini-juegos"));
        if (_estante.Any(e => e.Def.Categoria == CategoriaLogro.Maldito)) lista.Add((Malditos, "Malditos"));
        if (_estante.Any(e => e.Def.Categoria == CategoriaLogro.Legendario)) lista.Add((Legendarios, "Legendarios"));
        return lista;
    }

    /// <summary>El estante se nombra por el árbol, nunca por el hábito (eso solo se ve al tocar un emblema).</summary>
    private static string NombreArbol(string? arbol) =>
        arbol is null ? "Árbol que ya no está" : $"Estante del {arbol.ToLower(Tema.Cultura)}";

    private static string ClaveDe(EntradaEstante e) => e.Def.Categoria switch
    {
        CategoriaLogro.General => Principales,
        CategoriaLogro.Habito => PrefijoHabito + e.Def.Habito!.Value,
        CategoriaLogro.Oculto => MiniJuegos,
        CategoriaLogro.Maldito => Malditos,
        _ => Legendarios,
    };

    // ---------- Hoja de filtros ----------

    private async Task AbrirFiltros()
    {
        ArmarFiltros();
        _hojaFiltros.IsVisible = true;
        _panelFiltros.TranslationY = 700;
        await _panelFiltros.TranslateToAsync(0, 0, 220, Easing.CubicOut);
    }

    private async Task CerrarFiltros()
    {
        if (!_hojaFiltros.IsVisible) return;
        await _panelFiltros.TranslateToAsync(0, 700, 180, Easing.CubicIn);
        _hojaFiltros.IsVisible = false;
    }

    /// <summary>Las opciones de mini-juegos, malditos y legendarios aparecen solo si ya hay alguno ganado.</summary>
    private void ArmarFiltros()
    {
        var categorias = new List<(string, string)> { (Todas, "Todas") };
        categorias.AddRange(Categorias().Select(c => (c.Clave, c.Clave.StartsWith(PrefijoHabito) ? c.Nombre.Replace("Estante del ", "").ToUpperFirst() : c.Nombre)));
        if (!categorias.Any(c => c.Item1 == _categoria)) _categoria = Todas;

        var rangos = new List<(string, string)>
        {
            (TodoRango, "Todos"), (nameof(Rango.Bronce), "★ Bronce"), (nameof(Rango.Plata), "★★ Plata"),
            (nameof(Rango.Oro), "★★★ Oro"), (nameof(Rango.SinGanar), "Por ganar"),
        };
        if (_estante.Any(e => e.Rango == Rango.Maldito)) rangos.Add((nameof(Rango.Maldito), "Malditos"));
        if (_estante.Any(e => e.Rango == Rango.Legendario)) rangos.Add((nameof(Rango.Legendario), "Legendarios"));
        if (!rangos.Any(r => r.Item1 == _rango)) _rango = TodoRango;

        var cat = new Chips<string>(categorias, iniciales: [_categoria]);
        var ran = new Chips<string>(rangos, iniciales: [_rango]);
        var ord = new Chips<Orden>(
            [(Orden.Categoria, "Por categoría (A-Z)"), (Orden.NuevosPrimero, "Nuevos primero"), (Orden.Recientes, "Más recientes"), (Orden.Antiguos, "Más antiguos")],
            iniciales: [_orden]);
        cat.Cambio += () => { _categoria = cat.Elegido ?? Todas; Pintar(); };
        ran.Cambio += () => { _rango = ran.Elegido ?? TodoRango; Pintar(); };
        ord.Cambio += () => { _orden = ord.Elegido; Pintar(); };

        var limpiar = Tema.Boton("Quitar filtros", Colors.Transparent, Tema.HuesoTenue);
        limpiar.Clicked += async (_, _) =>
        {
            _categoria = Todas;
            _rango = TodoRango;
            _orden = Orden.Categoria;
            Pintar();
            await CerrarFiltros();
        };
        var listo = Tema.Boton("Listo", Tema.Hoja);
        listo.Clicked += async (_, _) => await CerrarFiltros();

        _filtros.Clear();
        _filtros.Add(new BoxView { Color = Tema.HuesoTenue, HeightRequest = 4, WidthRequest = 44, CornerRadius = 2, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 6) });
        _filtros.Add(Tema.Texto("Filtrar el estante", 20, Tema.FuenteTitulo));
        _filtros.Add(Tema.Texto("Categoría", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(cat);
        _filtros.Add(Tema.Texto("Rango", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(ran);
        _filtros.Add(Tema.Texto("Orden", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(ord);
        var botones = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
            Margin = new Thickness(0, 8, 0, 0),
        };
        botones.Add(limpiar, 0);
        botones.Add(listo, 1);
        _filtros.Add(botones);
    }

    // ---------- Estantes ----------

    private void Pintar()
    {
        Rango? ran = _rango != TodoRango ? Enum.Parse<Rango>(_rango) : null;
        var visibles = _estante
            .Where(e => _categoria == Todas || ClaveDe(e) == _categoria)
            .Where(e => ran is null || e.Rango == ran)
            .ToList();

        ActualizarResumen();
        _lista.Clear();
        if (visibles.Count == 0)
        {
            _lista.Add(Tema.Texto("No hay emblemas con estos filtros.", 15, color: Tema.HuesoTenue));
            return;
        }

        if (_orden is Orden.Recientes or Orden.Antiguos)
        {
            // Por fecha de adquisición: un solo estante; lo que aún no se gana va al final, en orden alfabético.
            var ganados = visibles.Where(e => e.Fecha is not null);
            ganados = _orden == Orden.Recientes
                ? ganados.OrderByDescending(e => e.Fecha).ThenBy(e => e.Def.Titulo, Alfabetico)
                : ganados.OrderBy(e => e.Fecha).ThenBy(e => e.Def.Titulo, Alfabetico);
            Estante("Por fecha", [.. ganados, .. Alfabeticos(visibles.Where(e => e.Fecha is null))]);
            return;
        }

        var resto = visibles;
        if (_orden == Orden.NuevosPrimero)
        {
            var nuevos = visibles.Where(e => e.Nuevo).ToList();
            Estante("Recién conseguidos", Alfabeticos(nuevos));
            resto = [.. visibles.Where(e => !e.Nuevo)];
        }

        // Orden estándar: una sección por categoría, emblemas en orden alfabético.
        foreach (var (clave, nombre) in Categorias())
            Estante(nombre, Alfabeticos(resto.Where(e => ClaveDe(e) == clave)));
    }

    private void ActualizarResumen()
    {
        var partes = new List<string>();
        if (_categoria != Todas)
            partes.Add(Categorias().FirstOrDefault(c => c.Clave == _categoria).Nombre ?? "");
        if (_rango != TodoRango) partes.Add(Nombre(Enum.Parse<Rango>(_rango)));
        if (_orden != Orden.Categoria)
            partes.Add(_orden switch { Orden.NuevosPrimero => "Nuevos primero", Orden.Recientes => "Más recientes", _ => "Más antiguos" });

        _botonFiltros.Text = partes.Count == 0 ? "Filtrar" : $"Filtrar · {partes.Count}";
        _resumenFiltro.Text = string.Join(" · ", partes);
    }

    private static IEnumerable<EntradaEstante> Alfabeticos(IEnumerable<EntradaEstante> entradas) =>
        entradas.OrderBy(e => e.Def.Titulo, Alfabetico);

    private void Estante(string titulo, IEnumerable<EntradaEstante> entradas)
    {
        var lista = entradas.ToList();
        if (lista.Count == 0) return;

        var fila = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
        };
        foreach (var e in lista) fila.Children.Add(Celda(e));

        _lista.Add(Tema.Texto(titulo, 18, Tema.FuenteTitulo, titulo == "Recién conseguidos" ? Tema.Miel : Tema.Hoja));
        _lista.Add(fila);
        // La tabla del estante.
        _lista.Add(new Border
        {
            HeightRequest = 10,
            Background = new LinearGradientBrush([new GradientStop(Madera, 0), new GradientStop(MaderaOscura, 1)], new Point(0, 0), new Point(0, 1)),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
            Margin = new Thickness(0, 0, 0, 14),
        });
    }

    private View Celda(EntradaEstante e)
    {
        const double Ancho = 84;
        var dibujo = new GraphicsView { Drawable = new Emblema(e), WidthRequest = Ancho, HeightRequest = 96, InputTransparent = true };
        var nombre = Tema.Texto(e.Def.Titulo, 12, Tema.FuenteNegrita, e.Nivel > 0 ? Tema.Hueso : Tema.HuesoTenue);
        nombre.HorizontalTextAlignment = TextAlignment.Center;
        nombre.MaxLines = 2;
        nombre.LineBreakMode = LineBreakMode.TailTruncation;
        nombre.LineHeight = 1.0;
        nombre.WidthRequest = Ancho;
        nombre.InputTransparent = true;
        var celda = new VerticalStackLayout
        {
            Spacing = 2,
            WidthRequest = Ancho,
            Margin = new Thickness(0, 4, 4, 8),
            Children = { dibujo, nombre },
        };
        SemanticProperties.SetDescription(celda, $"{e.Def.Titulo}. {(e.Nivel > 0 ? $"Emblema {Nombre(e.Rango)}{(e.Nuevo ? ", nuevo" : "")}" : "Por ganar")}");
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) => AbrirDetalle(e);
        celda.GestureRecognizers.Add(toque);
        return celda;
    }

    private void AbrirDetalle(EntradaEstante e)
    {
        var def = e.Def;
        bool ganado = e.Nivel > 0;
        _panelContenido.Clear();
        _panelContenido.Add(new GraphicsView
        {
            Drawable = new Emblema(e with { Nuevo = false }),
            WidthRequest = 130,
            HeightRequest = 160,
            HorizontalOptions = LayoutOptions.Center,
        });
        _panelContenido.Add(Tema.Texto(def.Titulo, 22, Tema.FuenteTitulo, ganado ? Tema.Miel : Tema.Hueso));
        _panelContenido.Add(Tema.Texto(ganado ? Nombre(e.Rango) : "Aún por ganar", 14, Tema.FuenteNegrita, Tema.HuesoTenue));

        if (def.Habito is { } h)
        {
            var nombre = CatalogoHabitos.De(h).Nombre;
            _panelContenido.Add(Tema.Texto(e.Arbol is null ? nombre : $"{e.Arbol} · {nombre}", 14, Tema.FuenteNegrita, Tema.Hoja));
        }

        _panelContenido.Add(Tema.Texto(def.TextoNivel(ganado ? e.Nivel : 1), 16));
        if (e.Nivel < def.Niveles.Length && def.Niveles.Length > 1)
        {
            int meta = def.Niveles[e.Nivel];
            _panelContenido.Add(Tema.Texto(ganado
                ? $"Siguiente estrella: {def.TextoNivel(e.Nivel + 1)} Llevas {Math.Min(e.Progreso, meta)} de {meta}."
                : $"Llevas {Math.Min(e.Progreso, meta)} de {meta}.", 14, color: Tema.HuesoTenue));
        }
        if (def.Debuff is { } d && e.Rango == Rango.Maldito)
            _panelContenido.Add(Tema.Texto($"Efecto: {Efecto(d)} Se rompe con su redención.", 14, Tema.FuenteNegrita, Tema.Miel));
        if (def.Nota is { } nota)
            _panelContenido.Add(Tema.Texto(nota, 14, color: Tema.HuesoTenue));
        if (e.Fecha is { } f)
            _panelContenido.Add(Tema.Texto($"Conseguido el {Tema.FechaCorta(f)}.", 13, color: Tema.HuesoTenue));

        var cerrar = Tema.Boton("Volver al estante", Tema.Hoja);
        cerrar.Margin = new Thickness(0, 10, 0, 0);
        cerrar.Clicked += (_, _) => CerrarDetalle();
        _panelContenido.Add(cerrar);

        if (e.Nuevo) Servicios.Jardin.MarcarVisto(def.Id);
        _detalle.IsVisible = true;
    }

    private void CerrarDetalle()
    {
        _detalle.IsVisible = false;
        // Recarga para quitar el punto rojo del que se acaba de abrir.
        _estante = Servicios.Jardin.Estante();
        Pintar();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_detalle.IsVisible)
        {
            CerrarDetalle();
            return true;
        }
        if (_hojaFiltros.IsVisible)
        {
            _ = CerrarFiltros();
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private static string Nombre(Rango r) => r switch
    {
        Rango.Bronce => "Bronce ★",
        Rango.Plata => "Plata ★★",
        Rango.Oro => "Oro ★★★",
        Rango.Maldito => "Maldito",
        Rango.Legendario => "Legendario",
        _ => "Por ganar",
    };

    private static string Efecto(Debuff d) => d switch
    {
        Debuff.MasBichos => "las plagas traen más bichos.",
        Debuff.TierraSeca => "la tierra se seca más rápido; regar toma más tiempo.",
        Debuff.MasRamas => "crecen más ramas sueltas al podar.",
        _ => "las raíces podridas son más tercas.",
    };
}

internal static class TextoExtensiones
{
    public static string ToUpperFirst(this string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Tema.Cultura) + s[1..];
}
