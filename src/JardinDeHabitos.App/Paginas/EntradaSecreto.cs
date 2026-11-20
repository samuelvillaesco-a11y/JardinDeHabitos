using JardinDeHabitos.Core;
using Microsoft.Maui.Controls.Shapes;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Donde se escribe el secreto según el candado: teclado numérico con puntos (PIN), cuadrícula
/// de 3×3 para dibujar (patrón) o campo de texto (clave o código de recuperación).
/// Avisa con <see cref="Listo"/> cuando el usuario termina.
/// </summary>
public sealed class EntradaSecreto : ContentView
{
    private readonly TipoCandado _tipo;
    private readonly Label _puntos = Tema.Texto("", 30, Tema.FuenteTitulo, Tema.Miel);
    private readonly Entry? _texto;
    private readonly PatronDibujo? _patron;
    private readonly GraphicsView? _lienzo;
    private string _pin = "";

    public event Action<string>? Listo;

    public EntradaSecreto(TipoCandado tipo, bool textoVisible = false)
    {
        _tipo = tipo;
        Content = tipo switch
        {
            TipoCandado.Pin => TecladoPin(),
            TipoCandado.Patron => CuadriculaPatron(out _patron, out _lienzo),
            _ => CampoTexto(textoVisible, out _texto),
        };
    }

    public void Limpiar()
    {
        _pin = "";
        PintarPuntos();
        if (_texto is not null) _texto.Text = "";
        _patron?.Limpiar();
        _lienzo?.Invalidate();
    }

    public void Enfocar() => _texto?.Focus();

    // ---------- PIN ----------

    private View TecladoPin()
    {
        _puntos.HorizontalTextAlignment = TextAlignment.Center;
        PintarPuntos();
        var rejilla = new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)],
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
            ColumnSpacing = 14,
            RowSpacing = 12,
            WidthRequest = 300,
            HorizontalOptions = LayoutOptions.Center,
        };
        string[] teclas = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "✓"];
        for (int i = 0; i < teclas.Length; i++)
        {
            string t = teclas[i];
            var b = Tema.Boton(t, t == "✓" ? Tema.Miel : Tema.MusgoClaro, t == "✓" ? Tema.Musgo : Tema.Hueso);
            b.FontSize = 24;
            b.HeightRequest = 64;
            SemanticProperties.SetDescription(b, t switch { "⌫" => "Borrar", "✓" => "Listo", _ => t });
            b.Clicked += (_, _) => Tecla(t);
            rejilla.Add(b, i % 3, i / 3);
        }
        return new VerticalStackLayout { Spacing = 18, Children = { _puntos, rejilla } };
    }

    private void Tecla(string t)
    {
        switch (t)
        {
            case "⌫":
                if (_pin.Length > 0) _pin = _pin[..^1];
                break;
            case "✓":
                if (_pin.Length > 0) Listo?.Invoke(_pin);
                return;
            default:
                if (_pin.Length < ClaveLocal.PinMaximo) _pin += t;
                break;
        }
        PintarPuntos();
    }

    private void PintarPuntos()
    {
        _puntos.Text = _pin.Length == 0 ? "· · · ·" : string.Join(" ", Enumerable.Repeat("●", _pin.Length));
        SemanticProperties.SetDescription(_puntos, $"{_pin.Length} números escritos");
    }

    // ---------- Patrón ----------

    private View CuadriculaPatron(out PatronDibujo patron, out GraphicsView lienzo)
    {
        var p = new PatronDibujo();
        var g = new GraphicsView { Drawable = p, HeightRequest = 300, WidthRequest = 300, HorizontalOptions = LayoutOptions.Center };
        g.StartInteraction += (_, e) => { p.Tocar(e.Touches[0]); g.Invalidate(); };
        g.DragInteraction += (_, e) => { p.Mover(e.Touches[0]); g.Invalidate(); };
        void Terminar()
        {
            var trazo = p.Soltar();
            g.Invalidate();
            if (trazo.Length > 0) Listo?.Invoke(trazo);
        }
        g.EndInteraction += (_, _) => Terminar();
        // Si el sistema cancela el gesto (por ejemplo, el dedo sale de la cuadrícula), cuenta como soltar.
        g.CancelInteraction += (_, _) => Terminar();
        SemanticProperties.SetDescription(g, "Cuadrícula de nueve puntos para dibujar el patrón");
        patron = p;
        lienzo = g;
        return new Border
        {
            Content = g,
            Background = Tema.MusgoClaro,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            HorizontalOptions = LayoutOptions.Center,
            Padding = 8,
        };
    }

    // ---------- Texto ----------

    private View CampoTexto(bool visible, out Entry entrada)
    {
        entrada = new Entry
        {
            IsPassword = !visible,
            IsSpellCheckEnabled = false,
            IsTextPredictionEnabled = false,
            Keyboard = visible ? Keyboard.Plain : Keyboard.Default,
            FontFamily = Tema.Fuente,
            FontSize = 20,
            TextColor = Tema.Hueso,
            PlaceholderColor = Tema.HuesoTenue,
            BackgroundColor = Tema.MusgoClaro,
            Placeholder = visible ? "XXXX-XXXX-XXXX-XXXX" : "Tu clave",
            MaxLength = ClaveLocal.LargoMaximo,
        };
        var e = entrada;
        var listo = Tema.Boton("Listo", Tema.Miel);
        listo.Clicked += (_, _) => Listo?.Invoke(e.Text ?? "");
        e.Completed += (_, _) => Listo?.Invoke(e.Text ?? "");
        return new VerticalStackLayout { Spacing = 12, Children = { e, listo } };
    }
}

/// <summary>Nueve puntos (0 a 8, por filas). Se unen arrastrando el dedo; no se repiten.</summary>
public sealed class PatronDibujo : IDrawable
{
    private readonly List<int> _trazo = [];
    private PointF? _dedo;
    private RectF _rect;

    public void Limpiar()
    {
        _trazo.Clear();
        _dedo = null;
    }

    private PointF Centro(int i)
    {
        float paso = Math.Min(_rect.Width, _rect.Height) / 3f;
        float x0 = _rect.Center.X - paso, y0 = _rect.Center.Y - paso;
        return new PointF(x0 + paso * (i % 3), y0 + paso * (i / 3));
    }

    private float Radio => Math.Min(_rect.Width, _rect.Height) / 9f;

    public void Tocar(PointF p)
    {
        Limpiar();
        Mover(p);
    }

    public void Mover(PointF p)
    {
        _dedo = p;
        for (int i = 0; i < 9; i++)
        {
            if (_trazo.Contains(i)) continue;
            var c = Centro(i);
            if (MathF.Sqrt((c.X - p.X) * (c.X - p.X) + (c.Y - p.Y) * (c.Y - p.Y)) < Radio * 1.2f) _trazo.Add(i);
        }
    }

    /// <summary>Termina el trazo y lo devuelve como texto ("0124…"). La cuadrícula queda limpia.</summary>
    public string Soltar()
    {
        var s = string.Concat(_trazo);
        Limpiar();
        return s;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        _rect = rect;
        canvas.StrokeLineCap = LineCap.Round;
        if (_trazo.Count > 0)
        {
            canvas.StrokeColor = Tema.Miel;
            canvas.StrokeSize = 8;
            for (int i = 1; i < _trazo.Count; i++) canvas.DrawLine(Centro(_trazo[i - 1]), Centro(_trazo[i]));
            if (_dedo is { } d) canvas.DrawLine(Centro(_trazo[^1]), d);
        }
        for (int i = 0; i < 9; i++)
        {
            var c = Centro(i);
            bool usado = _trazo.Contains(i);
            canvas.FillColor = usado ? Tema.Miel : Tema.HuesoTenue;
            canvas.FillCircle(c, usado ? Radio * 0.45f : Radio * 0.3f);
        }
    }
}
