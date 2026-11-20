using System.Globalization;

namespace JardinDeHabitos.Core;

public enum TipoFondo
{
    Solido,
    Degradado,
    Escena,
}

/// <summary>Escenas dibujadas a mano para el fondo (vectoriales, propias del proyecto).</summary>
public enum EscenaFondo
{
    Bosque,
    Jardin,
    CasaDeCampo,
    Suburbio,
}

/// <summary>
/// El fondo elegido: un color, un degradado de 2 colores (de arriba hacia abajo) o una escena.
/// Colores en hexadecimal "#RRGGBB". Se guarda como texto en las preferencias.
/// </summary>
public sealed record Fondo(TipoFondo Tipo, string ColorA, string ColorB, EscenaFondo Escena = EscenaFondo.Bosque)
{
    /// <summary>El de siempre: verde musgo.</summary>
    public static Fondo PorDefecto { get; } = new(TipoFondo.Solido, Paleta.Musgo, Paleta.Musgo);

    public static Fondo Solido(string color) => new(TipoFondo.Solido, ColorHex.Normalizar(color), ColorHex.Normalizar(color));

    public static Fondo Degradado(string a, string b) => new(TipoFondo.Degradado, ColorHex.Normalizar(a), ColorHex.Normalizar(b));

    public static Fondo DeEscena(EscenaFondo e) => new(TipoFondo.Escena, Paleta.EscenaArriba(e), Paleta.EscenaAbajo(e), e);

    public string Serializar() => $"{Tipo}|{ColorA}|{ColorB}|{Escena}";

    /// <summary>Lee lo guardado; si está dañado o viene de otra versión, vuelve al fondo por defecto.</summary>
    public static Fondo Leer(string? texto)
    {
        var p = texto?.Split('|');
        if (p is not { Length: 4 }
            || !Enum.TryParse(p[0], out TipoFondo tipo) || !Enum.IsDefined(tipo)
            || !ColorHex.EsValido(p[1]) || !ColorHex.EsValido(p[2])
            || !Enum.TryParse(p[3], out EscenaFondo escena) || !Enum.IsDefined(escena))
            return PorDefecto;
        return tipo == TipoFondo.Escena ? DeEscena(escena) : new Fondo(tipo, ColorHex.Normalizar(p[1]), ColorHex.Normalizar(p[2]), escena);
    }

    /// <summary>
    /// Opacidad del velo verde musgo que va encima del fondo para que el texto claro de la app se lea
    /// (contraste WCAG ≥ 4.5:1 contra el color más claro del fondo). 0 si el fondo ya es oscuro.
    /// </summary>
    public double Velo => Contraste.VeloNecesario([ColorA, ColorB], Paleta.TextoClaro, Paleta.Musgo, Contraste.MinimoTexto);
}

/// <summary>Colores calmantes recomendados: tonos de bosque, otoño y miel.</summary>
public static class Paleta
{
    public const string Musgo = "#27331F";
    public const string TextoClaro = "#F3EBD3";

    public static IReadOnlyList<(string Nombre, string Color)> Solidos { get; } =
    [
        ("Verde musgo", Musgo),
        ("Bosque profundo", "#1F3A2B"),
        ("Oliva", "#5B6B2F"),
        ("Salvia", "#A7BF8E"),
        ("Menta suave", "#D6E8C8"),
        ("Ocre de otoño", "#8A5A2B"),
        ("Calabaza", "#B8662E"),
        ("Arcilla", "#A0674B"),
        ("Miel", "#E8C27A"),
        ("Mantequilla", "#F3E3B0"),
    ];

    public static IReadOnlyList<(string Nombre, string A, string B)> Degradados { get; } =
    [
        ("Atardecer en el bosque", "#F4C76E", "#6E7E3A"),
        ("Musgo y miel", "#E8C27A", "#27331F"),
        ("Hojas de otoño", "#D98E48", "#5A3A22"),
        ("Bosque al amanecer", "#CFE3B5", "#2F4A2A"),
        ("Prado tranquilo", "#F3E3B0", "#8DB255"),
        ("Té verde", "#E6EFD3", "#7FA067"),
        ("Tarde de calabaza", "#F2B33D", "#8A4B24"),
        ("Neblina de pinos", "#B9CBB0", "#1F3A2B"),
    ];

    /// <summary>Colores de cielo y suelo de cada escena (para el velo y la vista previa).</summary>
    public static string EscenaArriba(EscenaFondo e) => e switch
    {
        EscenaFondo.Jardin => "#F6E2A8",
        EscenaFondo.CasaDeCampo => "#F4C98A",
        EscenaFondo.Suburbio => "#F2A86B",
        _ => "#E9D9A6",
    };

    public static string EscenaAbajo(EscenaFondo e) => e switch
    {
        EscenaFondo.Jardin => "#5E8A3C",
        EscenaFondo.CasaDeCampo => "#6E7E3A",
        EscenaFondo.Suburbio => "#4A3A4E",
        _ => "#24391F",
    };

    public static string NombreEscena(EscenaFondo e) => e switch
    {
        EscenaFondo.Jardin => "Jardín en flor",
        EscenaFondo.CasaDeCampo => "Casa de campo",
        EscenaFondo.Suburbio => "Suburbio al atardecer",
        _ => "Bosque",
    };
}

public static class ColorHex
{
    public static bool EsValido(string? hex) =>
        hex is { Length: 7 } && hex[0] == '#' && hex[1..].All(Uri.IsHexDigit);

    public static string Normalizar(string hex)
    {
        if (!EsValido(hex)) throw new ArgumentException("Color inválido: se espera #RRGGBB.", nameof(hex));
        return hex.ToUpperInvariant();
    }

    public static (double R, double G, double B) Rgb(string hex)
    {
        Normalizar(hex);
        int v = int.Parse(hex[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (((v >> 16) & 0xFF) / 255.0, ((v >> 8) & 0xFF) / 255.0, (v & 0xFF) / 255.0);
    }

    public static string Hex(double r, double g, double b)
    {
        static int C(double x) => (int)Math.Round(Math.Clamp(x, 0, 1) * 255);
        return $"#{C(r):X2}{C(g):X2}{C(b):X2}";
    }

    /// <summary>Tono (0-360), saturación y brillo (0-1) a hexadecimal.</summary>
    public static string DesdeHsv(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
        var (r, g, b) = (h / 60) switch
        {
            < 1 => (c, x, 0.0),
            < 2 => (x, c, 0.0),
            < 3 => (0.0, c, x),
            < 4 => (0.0, x, c),
            < 5 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Hex(r + m, g + m, b + m);
    }

    public static (double H, double S, double V) Hsv(string hex)
    {
        var (r, g, b) = Rgb(hex);
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double h = d == 0 ? 0 : max == r ? 60 * (((g - b) / d) % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        return ((h + 360) % 360, max == 0 ? 0 : d / max, max);
    }

    /// <summary>Mezcla: (1-t)·a + t·b.</summary>
    public static string Mezclar(string a, string b, double t)
    {
        var (r1, g1, b1) = Rgb(a);
        var (r2, g2, b2) = Rgb(b);
        return Hex(r1 + (r2 - r1) * t, g1 + (g2 - g1) * t, b1 + (b2 - b1) * t);
    }
}

/// <summary>Contraste según WCAG 2.x.</summary>
public static class Contraste
{
    /// <summary>Mínimo para texto normal (WCAG AA).</summary>
    public const double MinimoTexto = 4.5;

    public static double Luminancia(string hex)
    {
        static double L(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        var (r, g, b) = ColorHex.Rgb(hex);
        return 0.2126 * L(r) + 0.7152 * L(g) + 0.0722 * L(b);
    }

    public static double Razon(string a, string b)
    {
        double la = Luminancia(a), lb = Luminancia(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>
    /// La menor opacidad del velo (color <paramref name="velo"/> encima del fondo) con la que el texto
    /// alcanza el contraste mínimo sobre todos los colores del fondo. Se busca en pasos de 0.05.
    /// </summary>
    public static double VeloNecesario(IEnumerable<string> fondo, string texto, string velo, double minimo)
    {
        var colores = fondo.ToList();
        for (double a = 0; a <= 0.95; a += 0.05)
        {
            double alfa = Math.Round(a, 2);
            if (colores.All(c => Razon(texto, ColorHex.Mezclar(c, velo, alfa)) >= minimo)) return alfa;
        }
        return 0.95;
    }
}
