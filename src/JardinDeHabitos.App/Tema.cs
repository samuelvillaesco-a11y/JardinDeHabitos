using System.Globalization;
using JardinDeHabitos.Core;
using Microsoft.Maui.Controls.Shapes;

namespace JardinDeHabitos.App;

/// <summary>
/// Dos paletas según el fondo elegido (<see cref="Fondo.Oscuro"/>): cuaderno de campo (papel crema,
/// tinta verde, ocre) o noche de jardín (musgo oscuro, hueso, miel). Ambas con contraste WCAG ≥ 4.5:1.
/// Los nombres vienen del tema oscuro original: Musgo = fondo, Hueso = texto. El árbol no cambia
/// (<see cref="Naturaleza"/>). Las páginas leen los colores al construirse: tras <see cref="Usar"/>
/// hay que volver a crearlas.
/// </summary>
public static class Tema
{
    public static bool Oscuro { get; private set; }
    public static Color Musgo { get; private set; } = Color.FromArgb("#F4EEDF");
    public static Color MusgoClaro { get; private set; } = Color.FromArgb("#EAE2CC");
    public static Color Hoja { get; private set; } = Color.FromArgb("#3F6A2A");
    public static Color HojaOscura { get; private set; } = Color.FromArgb("#2F5020");
    public static Color Miel { get; private set; } = Color.FromArgb("#8A5A0B");
    public static Color Hueso { get; private set; } = Color.FromArgb("#2E3A2C");
    public static Color HuesoTenue { get; private set; } = Color.FromArgb("#5C5A47");
    public static Color Linea { get; private set; } = Color.FromArgb("#C9BFA6");
    public static readonly Color Tierra = Naturaleza.Tierra;
    public static readonly Color TierraOscura = Naturaleza.TierraOscura;
    public static readonly Color Corteza = Naturaleza.Corteza;

    public static void Usar(bool oscuro)
    {
        Oscuro = oscuro;
        (Musgo, MusgoClaro, Hoja, HojaOscura, Miel, Hueso, HuesoTenue, Linea) = oscuro
            ? (Color.FromArgb("#27331F"), Color.FromArgb("#33422A"), Color.FromArgb("#8DB255"), Color.FromArgb("#4F7A32"),
               Color.FromArgb("#F2B33D"), Color.FromArgb("#F3EBD3"), Color.FromArgb("#B8B49A"), Color.FromArgb("#4A5A3E"))
            : (Color.FromArgb("#F4EEDF"), Color.FromArgb("#EAE2CC"), Color.FromArgb("#3F6A2A"), Color.FromArgb("#2F5020"),
               Color.FromArgb("#8A5A0B"), Color.FromArgb("#2E3A2C"), Color.FromArgb("#5C5A47"), Color.FromArgb("#C9BFA6"));
    }

    public const string Fuente = "Nunito";
    public const string FuenteNegrita = "NunitoBold";
    public const string FuenteTitulo = "LoraSemiBold";
    public const string FuenteIconos = "Lucide";

    public static readonly CultureInfo Cultura = new("es-MX");

    public static Label Texto(string texto, double tamano = 15, string fuente = Fuente, Color? color = null) => new()
    {
        Text = texto,
        FontSize = tamano,
        FontFamily = fuente,
        TextColor = color ?? Hueso,
        LineHeight = 1.25,
    };

    public static Label Titulo(string texto, double tamano = 30) => Texto(texto, tamano, FuenteTitulo);

    /// <summary>Ícono de línea (Lucide). Decorativo: el texto de al lado ya dice qué es.</summary>
    public static Label Icono(string glifo, double tamano = 20, Color? color = null)
    {
        var l = Texto(glifo, tamano, FuenteIconos, color ?? Hueso);
        l.VerticalTextAlignment = TextAlignment.Center;
        SemanticProperties.SetDescription(l, "");
        AutomationProperties.SetIsInAccessibleTree(l, false);
        return l;
    }

    public static FontImageSource Imagen(string glifo, Color color, double tamano = 18) =>
        new() { FontFamily = FuenteIconos, Glyph = glifo, Color = color, Size = tamano };

    /// <summary>Botón con ícono de línea a la izquierda del texto.</summary>
    public static Button Boton(string glifo, string texto, Color fondo, Color? colorTexto = null)
    {
        var b = Boton(texto, fondo, colorTexto);
        b.ImageSource = Imagen(glifo, b.TextColor);
        b.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8);
        return b;
    }

    public static Button Boton(string texto, Color fondo, Color? colorTexto = null) => new()
    {
        Text = texto,
        BackgroundColor = fondo,
        TextColor = colorTexto ?? Musgo,
        FontFamily = FuenteNegrita,
        FontSize = 16,
        CornerRadius = 6,
        Padding = new Thickness(18, 12),
        MinimumHeightRequest = 48,
    };

    public static Border Parcela(View contenido, Color? fondo = null, Color? borde = null) => new()
    {
        Content = contenido,
        Background = fondo ?? MusgoClaro,
        Stroke = borde ?? Linea,
        StrokeThickness = borde is null ? 1 : 2,
        StrokeShape = new RoundRectangle { CornerRadius = 6 },
        Padding = new Thickness(12),
    };

    public static string NombreEtapa(GrowthStage etapa) => etapa switch
    {
        GrowthStage.Semilla => "Semilla",
        GrowthStage.Brote => "Brote",
        GrowthStage.Arbusto => "Arbusto",
        GrowthStage.ArbolJoven => "Árbol joven",
        GrowthStage.Flor => "Flor",
        GrowthStage.FrutaInmadura => "Fruta inmadura",
        GrowthStage.FrutaComestible => "Fruta comestible",
        GrowthStage.FrutaLegendaria => "Fruta legendaria",
        _ => etapa.ToString(),
    };

    public static string NombreDia(DayOfWeek dia) => Cultura.DateTimeFormat.GetDayName(dia);

    public static string Fecha(DateOnly fecha) => fecha.ToString("dddd d 'de' MMMM", Cultura);

    public static string FechaCorta(DateOnly fecha) => fecha.ToString("d 'de' MMMM", Cultura);

    /// <summary>Fecha compacta para ejes de gráficas: "3 mar".</summary>
    public static string FechaEje(DateOnly fecha) => fecha.ToString("d MMM", Cultura).TrimEnd('.');
}

/// <summary>Colores del árbol y la escena: no cambian con el tema de la interfaz.</summary>
public static class Naturaleza
{
    public static readonly Color Tierra = Color.FromArgb("#6B4A2E");
    public static readonly Color TierraOscura = Color.FromArgb("#4A3322");
    public static readonly Color Corteza = Color.FromArgb("#4A3524");
    public static readonly Color Hoja = Color.FromArgb("#8DB255");
    public static readonly Color HojaOscura = Color.FromArgb("#4F7A32");
    public static readonly Color Miel = Color.FromArgb("#F2B33D");
    public static readonly Color Hueso = Color.FromArgb("#F3EBD3");
}

/// <summary>Glifos de Resources/Fonts/Lucide.ttf (recortada: agregar uno nuevo exige volver a recortarla).</summary>
public static class Iconos
{
    public const string Arboles = "\ue2f5";
    public const string Libro = "\ue05f";
    public const string Ajustes = "\ue154";
    public const string Musica = "\ue122";
    public const string Persona = "\ue19f";
    public const string Paleta = "\ue1dd";
    public const string Candado = "\ue10b";
    public const string Escudo = "\ue158";
    public const string Brote = "\ue1eb";
    public const string Salvavidas = "\ue101";
    public const string Telefono = "\ue133";
    public const string Mas = "\ue13d";
    public const string Filtros = "\ue29a";
    public const string Trofeo = "\ue373";
    public const string Pluma = "\ue130";
    public const string Celular = "\ue163";
    public const string Numeral = "\ue0ef";
    public const string Cuadricula = "\ue0e9";
    public const string Hoja = "\ue2de";
    public const string Flecha = "\ue06f";
}
