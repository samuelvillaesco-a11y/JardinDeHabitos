using JardinDeHabitos.Core;

namespace JardinDeHabitos.App;

/// <summary>
/// El fondo que eligió el usuario (color, degradado o escena), guardado en Preferences y aplicado a
/// las pantallas principales. Encima va un velo verde musgo lo bastante opaco para que el texto
/// claro se lea (contraste WCAG ≥ 4.5:1, calculado en <see cref="Fondo.Velo"/>).
/// </summary>
public static class Apariencia
{
    private const string ClaveFondo = "apariencia.fondo";


    public static Fondo Actual => Fondo.Leer(Preferences.Default.Get<string?>(ClaveFondo, null));

    public static void Elegir(Fondo fondo)
    {
        Preferences.Default.Set(ClaveFondo, fondo.Serializar());
    }

    public static string ImagenDe(EscenaFondo e) => e switch
    {
        EscenaFondo.Jardin => "escena_jardin.png",
        EscenaFondo.CasaDeCampo => "escena_casadecampo.png",
        EscenaFondo.Suburbio => "escena_suburbio.png",
        _ => "escena_bosque.png",
    };

    /// <summary>El pincel del fondo (sin escena): color sólido o degradado de arriba hacia abajo.</summary>
    public static Brush Pincel(Fondo f) => f.Tipo == TipoFondo.Solido
        ? new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb(f.ColorA))
        : new LinearGradientBrush(
            [new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb(f.ColorA), 0), new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb(f.ColorB), 1)],
            new Point(0, 0), new Point(0, 1));

    /// <summary>
    /// Pone el fondo detrás del contenido de la página. La primera vez envuelve el contenido en una
    /// <see cref="CapaFondo"/>; después solo actualiza las capas. Llamar en OnAppearing.
    /// </summary>
    public static void Aplicar(ContentPage pagina)
    {
        if (pagina.Content is not CapaFondo capa)
        {
            var contenido = pagina.Content;
            pagina.Content = null;
            capa = new CapaFondo(contenido);
            pagina.Content = capa;
        }
        capa.Pintar(Actual);
    }
}

/// <summary>Fondo + velo + contenido, uno encima del otro.</summary>
public sealed class CapaFondo : Grid
{
    private readonly BoxView _color = new() { InputTransparent = true };
    private readonly Image _escena = new() { Aspect = Aspect.AspectFill, InputTransparent = true };
    private readonly BoxView _velo = new() { Color = Tema.Musgo, InputTransparent = true };

    public View? Contenido { get; }

    public CapaFondo(View? contenido)
    {
        Contenido = contenido;
        // El fondo llega hasta los bordes (detrás de la barra de estado); el contenido no.
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.None);
        Add(_color);
        Add(_escena);
        Add(_velo);
        if (contenido is not null)
            Add(new ContentView { Content = contenido, SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container) });
    }

    public void Pintar(Fondo f)
    {
        _color.Background = Apariencia.Pincel(f);
        _escena.IsVisible = f.Tipo == TipoFondo.Escena;
        if (_escena.IsVisible) _escena.Source = Apariencia.ImagenDe(f.Escena);
        _velo.Opacity = f.Velo;
        _velo.IsVisible = f.Velo > 0;
    }
}
