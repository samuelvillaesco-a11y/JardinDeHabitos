using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

/// <summary>Colores de las gráficas del blog, medidos contra la parcela (#33422A): marcas ≥ 3:1, texto ≥ 4.5:1.</summary>
public static class ColoresGrafica
{
    public static readonly Color Rejilla = Color.FromArgb("#4A5A3E");
    public static readonly Color Eje = Tema.HuesoTenue;

    /// <summary>Intensidad 0–4 de un síntoma: un solo tono (miel), de apagado a brillante.</summary>
    public static readonly Color[] Intensidad =
        [Color.FromArgb("#3F4E33"), Color.FromArgb("#756C36"), Color.FromArgb("#A18638"), Color.FromArgb("#CB9D3B"), Color.FromArgb("#F2B33D")];

    public static Color DeIntensidad(double v) => Intensidad[(int)Math.Clamp(Math.Round(v), 0, 4)];
}

/// <summary>
/// Una serie por día (ánimo o ganas). Línea de 2 px que se corta en los días sin registro, puntos de 8 px,
/// rejilla tenue por nivel y una guía vertical en el día tocado.
/// </summary>
public sealed class GraficaLinea(Color color, double minimo, double maximo, string[] etiquetasY) : IDrawable
{
    public IReadOnlyList<PuntoDia> Puntos { get; set; } = [];
    public int? Seleccionado { get; set; }

    public const float MargenIzq = 34, MargenDer = 10, MargenArriba = 10, MargenAbajo = 24;

    public static int? IndiceEn(float x, float ancho, int n)
    {
        if (n == 0) return null;
        float util = ancho - MargenIzq - MargenDer;
        if (n == 1) return 0;
        return (int)Math.Clamp(Math.Round((x - MargenIzq) / util * (n - 1)), 0, n - 1);
    }

    public void Draw(ICanvas c, RectF r)
    {
        c.Antialias = true;
        float izq = r.Left + MargenIzq, der = r.Right - MargenDer, arriba = r.Top + MargenArriba, abajo = r.Bottom - MargenAbajo;
        float Y(double v) => (float)(abajo - (v - minimo) / (maximo - minimo) * (abajo - arriba));
        int n = Puntos.Count;
        float X(int i) => n <= 1 ? (izq + der) / 2 : izq + i * (der - izq) / (n - 1);

        // Rejilla y etiquetas del eje Y (un renglón por nivel de la escala).
        c.FontColor = ColoresGrafica.Eje;
        c.FontSize = 11;
        c.Font = new Microsoft.Maui.Graphics.Font(Tema.Fuente);
        for (int k = 0; k < etiquetasY.Length; k++)
        {
            double v = minimo + k * (maximo - minimo) / (etiquetasY.Length - 1);
            float y = Y(v);
            c.StrokeColor = ColoresGrafica.Rejilla;
            c.StrokeSize = 1;
            c.DrawLine(izq, y, der, y);
            c.DrawString(etiquetasY[k], r.Left, y - 9, MargenIzq - 6, 18, HorizontalAlignment.Right, VerticalAlignment.Center);
        }
        if (n == 0) return;

        // Fechas: primera y última (y la tocada se lee abajo de la gráfica).
        c.DrawString(Tema.FechaEje(Puntos[0].Dia), izq - 4, abajo + 6, 80, 16, HorizontalAlignment.Left, VerticalAlignment.Top);
        c.DrawString(Tema.FechaEje(Puntos[^1].Dia), der - 80, abajo + 6, 84, 16, HorizontalAlignment.Right, VerticalAlignment.Top);

        if (Seleccionado is { } s && s < n)
        {
            c.StrokeColor = Tema.HuesoTenue;
            c.StrokeSize = 1;
            c.StrokeDashPattern = [3, 3];
            c.DrawLine(X(s), arriba, X(s), abajo);
            c.StrokeDashPattern = null;
        }

        // Línea: segmentos solo entre días consecutivos con registro.
        c.StrokeColor = color;
        c.StrokeSize = 2;
        c.StrokeLineCap = LineCap.Round;
        c.StrokeLineJoin = LineJoin.Round;
        for (int i = 1; i < n; i++)
            if (Puntos[i - 1].Valor is { } a && Puntos[i].Valor is { } b)
                c.DrawLine(X(i - 1), Y(a), X(i), Y(b));

        // Puntos: todos si caben (≤ 31), si no solo los aislados, el último y el tocado.
        bool todos = n <= 31;
        for (int i = 0; i < n; i++)
        {
            if (Puntos[i].Valor is not { } v) continue;
            bool aislado = (i == 0 || Puntos[i - 1].Valor is null) && (i == n - 1 || Puntos[i + 1].Valor is null);
            bool tocado = Seleccionado == i;
            if (!todos && !aislado && !tocado && i != n - 1) continue;
            float radio = tocado ? 6 : 4;
            // Anillo del color de la parcela para que el punto se separe de la línea.
            c.Relleno(Tema.MusgoClaro);
            c.FillCircle(X(i), Y(v), radio + 2);
            c.Relleno(color);
            c.FillCircle(X(i), Y(v), radio);
        }
    }
}

/// <summary>
/// Mapa de calor de síntomas: una fila por síntoma, una columna por día (o semana). Las celdas sin
/// registro quedan vacías con un punto tenue, para no confundir "no anoté" con "nada".
/// </summary>
public sealed class MapaSintomas : IDrawable
{
    public IReadOnlyList<FilaMapa> Filas { get; set; } = [];
    public IReadOnlyList<DateOnly> Columnas { get; set; } = [];
    public (int Fila, int Columna)? Seleccionada { get; set; }

    public const float AnchoEtiquetas = 128, AltoFila = 26, Separacion = 2, AltoPie = 22;

    public static float AltoPara(int filas) => filas * AltoFila + AltoPie;

    public (int Fila, int Columna)? CeldaEn(Point p, float ancho)
    {
        if (Filas.Count == 0 || Columnas.Count == 0 || p.X < AnchoEtiquetas) return null;
        int fila = (int)(p.Y / AltoFila);
        int col = (int)((p.X - AnchoEtiquetas) / ((ancho - AnchoEtiquetas) / Columnas.Count));
        return fila < Filas.Count && col < Columnas.Count ? (fila, col) : null;
    }

    public void Draw(ICanvas c, RectF r)
    {
        c.Antialias = true;
        c.Font = new Microsoft.Maui.Graphics.Font(Tema.Fuente);
        c.FontSize = 12;
        float anchoCelda = (r.Width - AnchoEtiquetas) / Math.Max(1, Columnas.Count);
        for (int f = 0; f < Filas.Count; f++)
        {
            float y = r.Top + f * AltoFila;
            c.FontColor = Tema.Hueso;
            c.DrawString(Filas[f].Nombre, r.Left, y, AnchoEtiquetas - 8, AltoFila, HorizontalAlignment.Left, VerticalAlignment.Center,
                TextFlow.ClipBounds);
            for (int k = 0; k < Columnas.Count; k++)
            {
                var celda = new RectF(r.Left + AnchoEtiquetas + k * anchoCelda + Separacion / 2, y + Separacion / 2,
                    anchoCelda - Separacion, AltoFila - Separacion);
                if (Filas[f].Valores[k] is { } v)
                {
                    c.Relleno(ColoresGrafica.DeIntensidad(v));
                    c.FillRoundedRectangle(celda, Math.Min(4, celda.Width / 3));
                }
                else
                {
                    c.Relleno(ColoresGrafica.Rejilla);
                    c.FillCircle(celda.Center.X, celda.Center.Y, 1.5f);
                }
                if (Seleccionada == (f, k))
                {
                    c.StrokeColor = Tema.Hueso;
                    c.StrokeSize = 2;
                    c.DrawRoundedRectangle(celda, Math.Min(4, celda.Width / 3));
                }
            }
        }
        if (Columnas.Count == 0) return;
        c.FontColor = ColoresGrafica.Eje;
        c.FontSize = 11;
        float pie = r.Top + Filas.Count * AltoFila + 4;
        c.DrawString(Tema.FechaEje(Columnas[0]), r.Left + AnchoEtiquetas, pie, 80, 16, HorizontalAlignment.Left, VerticalAlignment.Top);
        c.DrawString(Tema.FechaEje(Columnas[^1]), r.Right - 80, pie, 80, 16, HorizontalAlignment.Right, VerticalAlignment.Top);
    }
}
