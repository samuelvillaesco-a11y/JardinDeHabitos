using System.Globalization;
using JardinDeHabitos.Core;
using Microsoft.Maui.Controls.Shapes;

namespace JardinDeHabitos.App;

/// <summary>
/// Jardín al atardecer: musgo oscuro de fondo, parcelas de tierra, hojas y miel.
/// Lo único llamativo es el árbol; todo lo demás va quieto.
/// </summary>
public static class Tema
{
    public static readonly Color Musgo = Color.FromArgb("#27331F");
    public static readonly Color MusgoClaro = Color.FromArgb("#33422A");
    public static readonly Color Tierra = Color.FromArgb("#6B4A2E");
    public static readonly Color TierraOscura = Color.FromArgb("#4A3322");
    public static readonly Color Corteza = Color.FromArgb("#4A3524");
    public static readonly Color Hoja = Color.FromArgb("#8DB255");
    public static readonly Color HojaOscura = Color.FromArgb("#4F7A32");
    public static readonly Color Miel = Color.FromArgb("#F2B33D");
    public static readonly Color Hueso = Color.FromArgb("#F3EBD3");
    public static readonly Color HuesoTenue = Color.FromArgb("#B8B49A");

    public const string Fuente = "Nunito";
    public const string FuenteNegrita = "NunitoBold";
    public const string FuenteTitulo = "NunitoExtraBold";

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

    public static Button Boton(string texto, Color fondo, Color? colorTexto = null) => new()
    {
        Text = texto,
        BackgroundColor = fondo,
        TextColor = colorTexto ?? Musgo,
        FontFamily = FuenteNegrita,
        FontSize = 16,
        CornerRadius = 22,
        Padding = new Thickness(18, 12),
        MinimumHeightRequest = 48,
    };

    public static Border Parcela(View contenido, Color? fondo = null, Color? borde = null) => new()
    {
        Content = contenido,
        Background = fondo ?? MusgoClaro,
        Stroke = borde ?? Colors.Transparent,
        StrokeThickness = borde is null ? 0 : 2,
        StrokeShape = new RoundRectangle { CornerRadius = 20 },
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
}
