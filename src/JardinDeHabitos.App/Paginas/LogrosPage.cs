using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// El estante de trofeos (se abre con el bloqueo del teléfono). Solo emblemas: lo que significa cada uno
/// se ve al tocarlo. Los nuevos llevan un punto rojo y van primero. Mini-juegos, malditos y legendarios
/// no existen aquí (ni en los filtros) hasta que se gana al menos uno.
/// </summary>
public sealed class LogrosPage : ContentPage
{
    private enum Categoria { Todas, Principales, Especificos, MiniJuegos }
    private enum Orden { NuevosPrimero, Recientes, Antiguos }

    private const string TodoRango = "todos";
    private static readonly Color Madera = Color.FromArgb("#6B4A2E");
    private static readonly Color MaderaOscura = Color.FromArgb("#4A3322");

    private IReadOnlyList<EntradaEstante> _estante = [];
    private readonly VerticalStackLayout _lista = new() { Padding = new Thickness(18, 8, 18, 28), Spacing = 6 };
    private readonly VerticalStackLayout _filtros = new() { Spacing = 6, IsVisible = false };
    private readonly Button _botonFiltros = Tema.Boton("Filtrar y ordenar ▾", Tema.MusgoClaro, Tema.Hueso);
    private readonly Grid _detalle = new() { IsVisible = false };
    private readonly Border _panel;
    private readonly VerticalStackLayout _panelContenido = new() { Spacing = 6 };

    private Chips<Categoria> _categoria = null!;
    private Chips<string> _rango = null!;
    private Chips<Orden> _orden = null!;

    public LogrosPage()
    {
        Title = "Tu estante";
        BackgroundColor = Tema.Musgo;

        var fondo = new BoxView { Color = Colors.Black.WithAlpha(0.55f) };
        var cerrarFondo = new TapGestureRecognizer();
        cerrarFondo.Tapped += (_, _) => CerrarDetalle();
        fondo.GestureRecognizers.Add(cerrarFondo);
        _panel = new Border
        {
            Content = new ScrollView { Content = _panelContenido },
            Background = Tema.MusgoClaro,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
            Padding = new Thickness(22, 18, 22, 28),
            VerticalOptions = LayoutOptions.End,
            MaximumHeightRequest = 620,
        };
        _detalle.Add(fondo);
        _detalle.Add(_panel);

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
                            Tema.Titulo("🏆 Tu estante", 30),
                            Tema.Texto("Toca un emblema para ver qué significa.", 15, color: Tema.HuesoTenue),
                        },
                    },
                    new VerticalStackLayout
                    {
                        Padding = new Thickness(18, 12, 18, 4),
                        Spacing = 8,
                        Children = { _botonFiltros, _filtros },
                    },
                    _lista,
                },
            },
        };

        _botonFiltros.HorizontalOptions = LayoutOptions.Start;
        _botonFiltros.Clicked += (_, _) =>
        {
            _filtros.IsVisible = !_filtros.IsVisible;
            _botonFiltros.Text = _filtros.IsVisible ? "Filtrar y ordenar ▴" : "Filtrar y ordenar ▾";
        };

        var raiz = new Grid();
        raiz.Add(contenido);
        raiz.Add(_detalle);
        Content = raiz;

        Cargar();
    }

    private void Cargar()
    {
        _estante = Servicios.Jardin.Estante();
        ArmarFiltros();
        Pintar();
    }

    /// <summary>Las opciones de mini-juegos, malditos y legendarios aparecen solo si ya hay alguno ganado.</summary>
    private void ArmarFiltros()
    {
        var cat = _categoria?.Elegido ?? Categoria.Todas;
        var ran = _rango?.Elegido ?? TodoRango;
        var ord = _orden?.Elegido ?? Orden.NuevosPrimero;

        bool hayOcultos = _estante.Any(e => e.Def.Categoria == CategoriaLogro.Oculto);
        bool hayMalditos = _estante.Any(e => e.Rango == Rango.Maldito);
        bool hayLegendarios = _estante.Any(e => e.Rango == Rango.Legendario);

        var categorias = new List<(Categoria, string)>
        {
            (Categoria.Todas, "Todos"), (Categoria.Principales, "Principales"), (Categoria.Especificos, "Específicos"),
        };
        if (hayOcultos) categorias.Add((Categoria.MiniJuegos, "Mini-juegos"));

        var rangos = new List<(string, string)>
        {
            (TodoRango, "Todos"), (nameof(Rango.Bronce), "★ Bronce"), (nameof(Rango.Plata), "★★ Plata"),
            (nameof(Rango.Oro), "★★★ Oro"), (nameof(Rango.SinGanar), "Por ganar"),
        };
        if (hayMalditos) rangos.Add((nameof(Rango.Maldito), "Malditos"));
        if (hayLegendarios) rangos.Add((nameof(Rango.Legendario), "Legendarios"));

        if (!categorias.Any(c => c.Item1 == cat)) cat = Categoria.Todas;
        if (!rangos.Any(r => r.Item1 == ran)) ran = TodoRango;

        _categoria = new Chips<Categoria>(categorias, iniciales: [cat]);
        _rango = new Chips<string>(rangos, iniciales: [ran]);
        _orden = new Chips<Orden>([(Orden.NuevosPrimero, "Nuevos primero"), (Orden.Recientes, "Más recientes"), (Orden.Antiguos, "Más antiguos")], iniciales: [ord]);
        _categoria.Cambio += Pintar;
        _rango.Cambio += Pintar;
        _orden.Cambio += Pintar;

        _filtros.Clear();
        _filtros.Add(Tema.Texto("Categoría", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(_categoria);
        _filtros.Add(Tema.Texto("Rango", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(_rango);
        _filtros.Add(Tema.Texto("Orden", 14, Tema.FuenteNegrita, Tema.HuesoTenue));
        _filtros.Add(_orden);
    }

    private void Pintar()
    {
        var cat = _categoria.Elegido;
        Rango? ran = _rango.Elegido is { } r && r != TodoRango ? Enum.Parse<Rango>(r) : null;
        var ord = _orden.Elegido;

        var visibles = _estante.Where(e => cat switch
        {
            Categoria.Principales => e.Def.Categoria == CategoriaLogro.General,
            Categoria.Especificos => e.Def.Categoria == CategoriaLogro.Habito,
            Categoria.MiniJuegos => e.Def.Categoria == CategoriaLogro.Oculto,
            _ => true,
        }).Where(e => ran is null || e.Rango == ran).ToList();

        _lista.Clear();
        if (visibles.Count == 0)
        {
            _lista.Add(Tema.Texto("No hay emblemas con estos filtros.", 15, color: Tema.HuesoTenue));
            return;
        }

        if (ord != Orden.NuevosPrimero)
        {
            // Por fecha de adquisición: un solo estante; lo que aún no se gana va al final.
            var ganados = visibles.Where(e => e.Fecha is not null);
            ganados = ord == Orden.Recientes ? ganados.OrderByDescending(e => e.Fecha) : ganados.OrderBy(e => e.Fecha);
            Estante("Por fecha", [.. ganados, .. visibles.Where(e => e.Fecha is null)]);
            return;
        }

        var nuevos = visibles.Where(e => e.Nuevo).ToList();
        if (nuevos.Count > 0) Estante("Recién conseguidos", nuevos);
        var resto = visibles.Where(e => !e.Nuevo).ToList();

        Estante("Legendarios", resto.Where(e => e.Rango == Rango.Legendario));
        Estante("Malditos", resto.Where(e => e.Rango == Rango.Maldito));
        Estante("Mini-juegos", Ordenar(resto.Where(e => e.Def.Categoria == CategoriaLogro.Oculto)));
        Estante("Tu jardín", Ordenar(resto.Where(e => e.Def.Categoria == CategoriaLogro.General)));
        foreach (var grupo in resto.Where(e => e.Def.Categoria == CategoriaLogro.Habito).GroupBy(e => e.Def.Habito!.Value))
        {
            // El estante se nombra por el árbol, no por el hábito: el hábito solo se ve al tocar un emblema.
            var arbol = grupo.Select(e => e.Arbol).FirstOrDefault(a => a is not null);
            Estante(arbol is null ? "Un árbol que ya no está" : $"Estante del {arbol.ToLower(Tema.Cultura)}", Ordenar(grupo));
        }
    }

    /// <summary>Ganados primero (del mejor metal al menor); luego lo más cercano a su siguiente estrella.</summary>
    private static IEnumerable<EntradaEstante> Ordenar(IEnumerable<EntradaEstante> entradas) =>
        entradas.OrderByDescending(e => e.Nivel > 0).ThenByDescending(e => e.Rango).ThenByDescending(Avance);

    private static double Avance(EntradaEstante e) =>
        e.Nivel >= e.Def.Niveles.Length ? 1 : Math.Min(1, e.Progreso / (double)e.Def.Niveles[e.Nivel]);

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
        var dibujo = new GraphicsView { Drawable = new Emblema(e), WidthRequest = 78, HeightRequest = 96, InputTransparent = true };
        var celda = new ContentView { Content = dibujo, Margin = new Thickness(0, 4, 6, 4) };
        SemanticProperties.SetDescription(celda, e.Nivel > 0 ? $"Emblema {Nombre(e.Rango)}{(e.Nuevo ? ", nuevo" : "")}" : "Emblema por ganar");
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
        if (!_detalle.IsVisible) return base.OnBackButtonPressed();
        CerrarDetalle();
        return true;
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
