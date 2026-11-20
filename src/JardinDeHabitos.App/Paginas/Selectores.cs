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
            chip.Stroke = activo ? Tema.Hoja : Tema.MusgoClaro;
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
