using Microsoft.Maui.Controls.Shapes;

namespace JardinDeHabitos.App.Paginas;

/// <summary>Fila de opciones tipo "chip". Una sola elegida o varias con un máximo.</summary>
public sealed class Chips<T> : FlexLayout where T : notnull
{
    private readonly int _maximo;
    private readonly List<(T Valor, Border Chip, Label Texto)> _chips = [];
    private readonly List<T> _elegidos = [];

    public event Action? Cambio;

    /// <summary>Se intentó elegir una opción más del máximo: no se quita ninguna sin avisar.</summary>
    public event Action? Lleno;

    public Chips(IEnumerable<(T Valor, string Texto)> opciones, int maximo = 1, IEnumerable<T>? iniciales = null)
    {
        _maximo = maximo;
        Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap;
        foreach (var (valor, texto) in opciones)
        {
            var etiqueta = Tema.Texto(texto, 15, Tema.FuenteNegrita);
            var chip = new Border
            {
                Content = etiqueta,
                Padding = new Thickness(14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 2,
            };
            var toque = new TapGestureRecognizer();
            toque.Tapped += (_, _) => Alternar(valor);
            chip.GestureRecognizers.Add(toque);
            _chips.Add((valor, chip, etiqueta));
            Children.Add(chip);
        }
        foreach (var v in iniciales ?? []) _elegidos.Add(v);
        Pintar();
    }

    public IReadOnlyList<T> Elegidos => _elegidos;

    public T? Elegido => _elegidos.Count > 0 ? _elegidos[0] : default;

    private void Alternar(T valor)
    {
        if (_maximo == 1)
        {
            _elegidos.Clear();
            _elegidos.Add(valor);
        }
        else if (_elegidos.Contains(valor))
        {
            _elegidos.Remove(valor);
        }
        else
        {
            if (_elegidos.Count >= _maximo)
            {
                Lleno?.Invoke();
                return;
            }
            _elegidos.Add(valor);
        }
        Pintar();
        Cambio?.Invoke();
    }

    private void Pintar()
    {
        foreach (var (valor, chip, texto) in _chips)
        {
            bool activo = _elegidos.Contains(valor);
            chip.Background = activo ? Tema.Hoja : Colors.Transparent;
            chip.Stroke = activo ? Tema.Hoja : Tema.HuesoTenue.WithAlpha(0.4f);
            texto.TextColor = activo ? Tema.Musgo : Tema.Hueso;
            SemanticProperties.SetDescription(chip, $"{texto.Text}{(activo ? ", elegido" : "")}");
        }
    }
}

public static class Dias
{
    public static readonly string AvisoLleno =
        $"Ya elegiste {Core.CheatDaySchedule.MaxDiasPorSemana} días libres. Toca uno de ellos para quitarlo y luego elige otro.";

    /// <summary>Texto que aparece cuando se intenta elegir un 3.er día libre y se oculta al cambiar.</summary>
    public static Label AvisoPara<T>(Chips<T> chips) where T : notnull
    {
        var aviso = Tema.Texto(AvisoLleno, 14, Tema.FuenteNegrita, Tema.Miel);
        aviso.IsVisible = false;
        chips.Lleno += () => aviso.IsVisible = true;
        chips.Cambio += () => aviso.IsVisible = false;
        return aviso;
    }

    /// <summary>Semana de domingo a sábado, como la define la spec.</summary>
    public static IEnumerable<(DayOfWeek, string)> Semana =>
        Enum.GetValues<DayOfWeek>().Select(d => (d, Tema.NombreDia(d)));
}

/// <summary>
/// Escala de toques (ánimo 1–5, ganas o síntomas 0–4). Tocar el valor elegido lo quita (sin respuesta).
/// Debajo se lee en palabras lo elegido, así el número nunca va solo.
/// </summary>
public sealed class Escala : VerticalStackLayout
{
    private readonly int _primero;
    private readonly string[] _palabras;
    private readonly List<(Border Punto, Label Texto)> _puntos = [];
    private readonly Label _lectura;
    private readonly string _nombre;

    public event Action? Cambio;

    public int? Valor { get; private set; }

    public Escala(string nombre, string[] simbolos, string[] palabras, int primero, int? inicial, double tamano = 44)
    {
        _nombre = nombre;
        _primero = primero;
        _palabras = palabras;
        Valor = inicial;
        Spacing = 4;
        var fila = new HorizontalStackLayout { Spacing = 8 };
        for (int i = 0; i < simbolos.Length; i++)
        {
            int valor = primero + i;
            var texto = Tema.Texto(simbolos[i], simbolos[i].Length > 2 ? 22 : 16, Tema.FuenteNegrita);
            texto.HorizontalTextAlignment = TextAlignment.Center;
            texto.VerticalTextAlignment = TextAlignment.Center;
            var punto = new Border
            {
                Content = texto,
                WidthRequest = tamano,
                HeightRequest = tamano,
                StrokeShape = new RoundRectangle { CornerRadius = tamano / 2 },
                StrokeThickness = 2,
            };
            var toque = new TapGestureRecognizer();
            toque.Tapped += (_, _) =>
            {
                Valor = Valor == valor ? null : valor;
                Pintar();
                Cambio?.Invoke();
            };
            punto.GestureRecognizers.Add(toque);
            _puntos.Add((punto, texto));
            fila.Children.Add(punto);
        }
        _lectura = Tema.Texto("", 14, color: Tema.HuesoTenue);
        Children.Add(fila);
        Children.Add(_lectura);
        Pintar();
    }

    private void Pintar()
    {
        for (int i = 0; i < _puntos.Count; i++)
        {
            bool activo = Valor == _primero + i;
            var (punto, texto) = _puntos[i];
            punto.Background = activo ? Tema.Miel : Colors.Transparent;
            punto.Stroke = activo ? Tema.Miel : Tema.HuesoTenue.WithAlpha(0.4f);
            texto.TextColor = activo ? Tema.Musgo : Tema.Hueso;
            SemanticProperties.SetDescription(punto, $"{_nombre}: {_palabras[i]}{(activo ? ", elegido" : "")}");
        }
        _lectura.Text = Valor is { } v ? _palabras[v - _primero] : "Sin responder";
    }
}
