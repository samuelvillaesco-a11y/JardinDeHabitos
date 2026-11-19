using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

/// <summary>Especies disponibles y el color de su fruta.</summary>
public static class Especies
{
    public static readonly IReadOnlyList<(string Nombre, Color Fruta)> Todas =
    [
        ("Manzano", Color.FromArgb("#C8553D")),
        ("Naranjo", Color.FromArgb("#F08A24")),
        ("Limonero", Color.FromArgb("#F2D13D")),
        ("Cerezo", Color.FromArgb("#A3263A")),
        ("Durazno", Color.FromArgb("#F2A65A")),
        ("Higuera", Color.FromArgb("#7A4466")),
        ("Aguacate", Color.FromArgb("#5C7A29")),
        ("Mango", Color.FromArgb("#F5B335")),
        ("Granado", Color.FromArgb("#B0283C")),
        ("Olivo", Color.FromArgb("#4B3B47")),
    ];

    public static Color ColorFruta(string especie) =>
        Todas.FirstOrDefault(e => e.Nombre == especie).Fruta ?? Todas[0].Fruta;
}

/// <summary>Lo que hay que pintar de un árbol.</summary>
public sealed class EstadoArbol
{
    public GrowthStage Etapa { get; set; }
    public string Especie { get; set; } = "Manzano";
    public int Cicatrices { get; set; }
    public bool Plaga { get; set; }
    public bool RaicesPodridas { get; set; }
    public bool EnPaz { get; set; }
    public bool Regado { get; set; }

    public static EstadoArbol De(VistaArbol v) => new()
    {
        Etapa = v.Etapa,
        Especie = v.Especie,
        Cicatrices = v.Strikes + v.StrikesAbandono,
        // Un caracol o una rama seca se ven como una plaga pequeña: llaman la atención.
        Plaga = (v.Plaga && !v.TratadoHoy) || v.MiniEvento != MiniEvento.Ninguno,
        RaicesPodridas = v.Raices && !v.TratadoHoy,
        EnPaz = v.EsCheatDayHoy,
        Regado = v.RegadoHoy,
    };
}

/// <summary>Medidas del árbol dentro de un rectángulo, para dibujar y para los mini-juegos.</summary>
public readonly record struct GeometriaArbol(float CentroX, float Suelo, float Escala, RectF Copa, float TroncoAncho, float TroncoArriba)
{
    /// <summary>Zona "viva" de la planta: la copa o, si aún no hay copa, el área alrededor del brote.</summary>
    public RectF Zona => Copa.Width > 0
        ? Copa
        : new RectF(CentroX - Escala * 0.26f, Suelo - Escala * 0.36f, Escala * 0.52f, Escala * 0.32f);
}

/// <summary>Árbol vectorial plano con degradados cálidos. Sin texturas ni pixel art.</summary>
public static class Arbol
{
    private static readonly Color CieloArriba = Color.FromArgb("#F4C76E");
    private static readonly Color CieloAbajo = Color.FromArgb("#6E7E3A");
    private static readonly Color CortezaClara = Color.FromArgb("#7A5536");
    private static readonly Color Semilla = Color.FromArgb("#D9A066");
    private static readonly Color Flor = Color.FromArgb("#F6E4DC");
    private static readonly Color FrutaVerde = Color.FromArgb("#B8C85A");
    private static readonly Color Oro = Color.FromArgb("#F7CF4A");
    private static readonly Color Bicho = Color.FromArgb("#2E2233");
    private static readonly Color CopaEnfermaArriba = Color.FromArgb("#A9A452");
    private static readonly Color CopaEnfermaAbajo = Color.FromArgb("#5E6A2C");
    private static readonly Color RaizPodrida = Color.FromArgb("#9A8A5C");

    // Manchas de la copa: (x, y, radio) relativos al ancho de la copa.
    private static readonly (float X, float Y, float R)[] Manchas =
    [
        (0.50f, 0.58f, 0.34f), (0.24f, 0.62f, 0.24f), (0.76f, 0.62f, 0.24f),
        (0.34f, 0.36f, 0.25f), (0.66f, 0.36f, 0.25f), (0.50f, 0.24f, 0.24f),
    ];

    // Puntos dentro de la copa para flores y frutas.
    private static readonly PointF[] Adornos =
    [
        new(0.30f, 0.55f), new(0.62f, 0.30f), new(0.72f, 0.66f), new(0.45f, 0.40f),
        new(0.22f, 0.70f), new(0.55f, 0.74f), new(0.40f, 0.22f), new(0.80f, 0.48f),
    ];

    // Dónde aparece la plaga dentro de la zona de la planta.
    public static readonly PointF[] PosicionesPlaga =
    [
        new(0.28f, 0.40f), new(0.66f, 0.30f), new(0.52f, 0.62f), new(0.78f, 0.60f), new(0.36f, 0.76f),
    ];

    public static GeometriaArbol Geometria(RectF r, GrowthStage etapa)
    {
        float s = Math.Min(r.Width, r.Height);
        float cx = r.Center.X;
        float suelo = r.Top + r.Height * 0.80f;

        (float copa, float tronco) = etapa switch
        {
            GrowthStage.Semilla => (0f, 0f),
            GrowthStage.Brote => (0f, 0.16f),
            GrowthStage.Arbusto => (0.32f, 0.06f),
            GrowthStage.ArbolJoven => (0.38f, 0.20f),
            GrowthStage.Flor => (0.46f, 0.22f),
            GrowthStage.FrutaInmadura => (0.52f, 0.24f),
            GrowthStage.FrutaComestible => (0.58f, 0.26f),
            _ => (0.64f, 0.28f),
        };

        float troncoArriba = suelo - tronco * s;
        float ancho = copa * s;
        float alto = ancho * 0.84f;
        var rectCopa = ancho > 0
            ? new RectF(cx - ancho / 2, troncoArriba - alto * 0.78f, ancho, alto)
            : RectF.Zero;
        float grosor = Math.Max(0.014f, copa * 0.10f) * s;
        return new GeometriaArbol(cx, suelo, s, rectCopa, grosor, troncoArriba);
    }

    public static void Dibujar(ICanvas c, RectF r, EstadoArbol e, bool conCielo)
    {
        var g = Geometria(r, e.Etapa);
        float s = g.Escala;

        if (conCielo) Cielo(c, r);
        if (e.EnPaz) Paz(c, g);
        if (e.Etapa == GrowthStage.FrutaLegendaria) Resplandor(c, g);

        Monticulo(c, g, e.Regado);
        if (e.RaicesPodridas) Raices(c, g);

        switch (e.Etapa)
        {
            case GrowthStage.Semilla:
                // Semilla grande y clara, medio enterrada, con la punta del brote asomando.
                c.Relleno(Semilla);
                c.FillEllipse(g.CentroX - s * 0.06f, g.Suelo - s * 0.06f, s * 0.12f, s * 0.085f);
                c.Relleno(Colors.White.WithAlpha(0.25f));
                c.FillEllipse(g.CentroX - s * 0.035f, g.Suelo - s * 0.05f, s * 0.04f, s * 0.025f);
                c.StrokeColor = Tema.Hoja;
                c.StrokeSize = s * 0.014f;
                c.StrokeLineCap = LineCap.Round;
                c.DrawLine(g.CentroX + s * 0.01f, g.Suelo - s * 0.055f, g.CentroX + s * 0.03f, g.Suelo - s * 0.10f);
                break;
            case GrowthStage.Brote:
                Tallo(c, g);
                break;
            default:
                Tronco(c, g);
                Copa(c, g, e.Plaga);
                break;
        }

        Adornar(c, g, e);
        Cicatrices(c, g, e);
        if (e.Plaga) Plaga(c, g);
    }

    private static void Cielo(ICanvas c, RectF r)
    {
        c.SetFillPaint(Degradado(CieloArriba, CieloAbajo), r);
        c.FillRoundedRectangle(r, 24);
        // Sol bajo, plano.
        c.Relleno(Color.FromArgb("#FBE3A0").WithAlpha(0.85f));
        float sol = Math.Min(r.Width, r.Height) * 0.08f;
        c.FillCircle(r.Right - sol * 2.4f, r.Top + sol * 2.2f, sol);
    }

    private static void Paz(ICanvas c, GeometriaArbol g)
    {
        // Halo cálido y difuso: el árbol descansa en paz.
        var z = g.Zona;
        var area = new RectF(z.X - z.Width * 0.2f, z.Y - z.Height * 0.2f, z.Width * 1.4f, z.Height * 1.4f);
        c.SetFillPaint(new RadialGradientPaint(
            [new PaintGradientStop(0, Tema.Miel.WithAlpha(0.28f)), new PaintGradientStop(1, Tema.Miel.WithAlpha(0f))],
            new Point(0.5, 0.5), 0.5), area);
        c.FillEllipse(area);
    }

    private static void Resplandor(ICanvas c, GeometriaArbol g)
    {
        var z = g.Copa;
        var paint = new RadialGradientPaint(
            [new PaintGradientStop(0, Oro.WithAlpha(0.55f)), new PaintGradientStop(1, Oro.WithAlpha(0f))],
            new Point(0.5, 0.5), 0.5);
        var area = new RectF(z.X - z.Width * 0.18f, z.Y - z.Height * 0.12f, z.Width * 1.36f, z.Height * 1.36f);
        c.SetFillPaint(paint, area);
        c.FillEllipse(area);
    }

    private static void Monticulo(ICanvas c, GeometriaArbol g, bool regado)
    {
        float s = g.Escala;
        var m = new RectF(g.CentroX - s * 0.40f, g.Suelo - s * 0.06f, s * 0.80f, s * 0.18f);
        c.SetFillPaint(Degradado(Tema.Tierra, Tema.TierraOscura), m);
        c.FillEllipse(m);
        if (regado)
        {
            c.Relleno(Color.FromArgb("#3A2618").WithAlpha(0.7f));
            c.FillEllipse(g.CentroX - s * 0.18f, g.Suelo - s * 0.02f, s * 0.36f, s * 0.07f);
        }
    }

    private static void Raices(ICanvas c, GeometriaArbol g)
    {
        float s = g.Escala;
        c.StrokeColor = RaizPodrida;
        c.StrokeSize = s * 0.014f;
        c.StrokeLineCap = LineCap.Round;
        foreach (var (dx, dy) in new[] { (-0.16f, 0.07f), (0.02f, 0.09f), (0.17f, 0.06f) })
        {
            var p = new PathF(g.CentroX, g.Suelo);
            p.QuadTo(g.CentroX + s * dx * 0.4f, g.Suelo + s * dy, g.CentroX + s * dx, g.Suelo + s * dy * 0.8f);
            c.DrawPath(p);
            c.Relleno(Color.FromArgb("#6E6440"));
            c.FillCircle(g.CentroX + s * dx, g.Suelo + s * dy * 0.8f, s * 0.014f);
        }
    }

    private static void Tallo(ICanvas c, GeometriaArbol g)
    {
        float s = g.Escala;
        c.StrokeColor = Tema.HojaOscura;
        c.StrokeSize = s * 0.014f;
        c.StrokeLineCap = LineCap.Round;
        var p = new PathF(g.CentroX, g.Suelo);
        p.QuadTo(g.CentroX - s * 0.02f, g.Suelo - s * 0.08f, g.CentroX, g.TroncoArriba);
        c.DrawPath(p);
        HojaSuelta(c, g.CentroX, g.TroncoArriba + s * 0.02f, s * 0.08f, -0.5f);
        HojaSuelta(c, g.CentroX, g.TroncoArriba + s * 0.05f, s * 0.07f, 3.6f);
    }

    private static void HojaSuelta(ICanvas c, float x, float y, float largo, float angulo)
    {
        c.SaveState();
        c.Translate(x, y);
        c.Rotate(angulo * 180f / MathF.PI);
        var p = new PathF(0, 0);
        p.QuadTo(largo * 0.5f, -largo * 0.45f, largo, 0);
        p.QuadTo(largo * 0.5f, largo * 0.45f, 0, 0);
        p.Close();
        c.SetFillPaint(Degradado(Tema.Hoja, Tema.HojaOscura), new RectF(0, -largo * 0.3f, largo, largo * 0.6f));
        c.FillPath(p);
        c.RestoreState();
    }

    private static void Tronco(ICanvas c, GeometriaArbol g)
    {
        float w = g.TroncoAncho;
        var p = new PathF(g.CentroX - w * 0.9f, g.Suelo);
        p.QuadTo(g.CentroX - w * 0.4f, (g.Suelo + g.TroncoArriba) / 2, g.CentroX - w * 0.45f, g.TroncoArriba - w);
        p.LineTo(g.CentroX + w * 0.45f, g.TroncoArriba - w);
        p.QuadTo(g.CentroX + w * 0.4f, (g.Suelo + g.TroncoArriba) / 2, g.CentroX + w * 0.9f, g.Suelo);
        p.Close();
        c.SetFillPaint(Degradado(CortezaClara, Tema.Corteza, horizontal: true),
            new RectF(g.CentroX - w, g.TroncoArriba, w * 2, g.Suelo - g.TroncoArriba));
        c.FillPath(p);
    }

    private static void Copa(ICanvas c, GeometriaArbol g, bool enferma)
    {
        var z = g.Copa;
        c.SetFillPaint(enferma ? Degradado(CopaEnfermaArriba, CopaEnfermaAbajo) : Degradado(Tema.Hoja, Tema.HojaOscura), z);
        foreach (var (x, y, r) in Manchas)
            c.FillCircle(z.X + z.Width * x, z.Y + z.Height * y, z.Width * r);
        // Brillo plano arriba a la izquierda.
        c.Relleno(Colors.White.WithAlpha(0.10f));
        c.FillCircle(z.X + z.Width * 0.38f, z.Y + z.Height * 0.30f, z.Width * 0.16f);
    }

    private static void Adornar(ICanvas c, GeometriaArbol g, EstadoArbol e)
    {
        var z = g.Copa;
        if (z.Width <= 0) return;
        float r = z.Width * 0.045f;

        switch (e.Etapa)
        {
            case GrowthStage.Flor:
                foreach (var p in Adornos.Take(6))
                {
                    var (x, y) = (z.X + z.Width * p.X, z.Y + z.Height * p.Y);
                    c.Relleno(Flor);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * MathF.Tau / 5;
                        c.FillCircle(x + MathF.Cos(a) * r * 0.8f, y + MathF.Sin(a) * r * 0.8f, r * 0.6f);
                    }
                    c.Relleno(Tema.Miel);
                    c.FillCircle(x, y, r * 0.45f);
                }
                break;
            case GrowthStage.FrutaInmadura:
                Frutas(c, z, FrutaVerde, r * 0.9f, 6);
                break;
            case GrowthStage.FrutaComestible:
                Frutas(c, z, Especies.ColorFruta(e.Especie), r * 1.2f, 7);
                break;
            case GrowthStage.FrutaLegendaria:
                Frutas(c, z, Oro, r * 1.35f, 8);
                c.Relleno(Colors.White.WithAlpha(0.9f));
                foreach (var p in new[] { new PointF(0.1f, 0.1f), new PointF(0.95f, 0.2f), new PointF(0.05f, 0.85f) })
                    Destello(c, z.X + z.Width * p.X, z.Y + z.Height * p.Y, r * 1.2f);
                break;
        }
    }

    private static void Frutas(ICanvas c, RectF z, Color color, float r, int cuantas)
    {
        foreach (var p in Adornos.Take(cuantas))
        {
            var (x, y) = (z.X + z.Width * p.X, z.Y + z.Height * p.Y);
            c.Relleno(color);
            c.FillCircle(x, y, r);
            c.Relleno(Colors.White.WithAlpha(0.35f));
            c.FillCircle(x - r * 0.35f, y - r * 0.35f, r * 0.3f);
        }
    }

    private static void Destello(ICanvas c, float x, float y, float r)
    {
        var p = new PathF(x, y - r);
        p.QuadTo(x, y, x + r, y);
        p.QuadTo(x, y, x, y + r);
        p.QuadTo(x, y, x - r, y);
        p.QuadTo(x, y, x, y - r);
        p.Close();
        c.FillPath(p);
    }

    /// <summary>Cicatrices: marcas claras en el tronco (o junto a la planta si aún es pequeña).</summary>
    private static void Cicatrices(ICanvas c, GeometriaArbol g, EstadoArbol e)
    {
        int n = Math.Min(e.Cicatrices, 8);
        if (n == 0) return;
        float s = g.Escala;
        c.StrokeColor = Tema.Hueso.WithAlpha(0.75f);
        c.StrokeLineCap = LineCap.Round;

        bool hayTronco = e.Etapa >= GrowthStage.ArbolJoven;
        if (hayTronco)
        {
            c.StrokeSize = Math.Max(1.5f, g.TroncoAncho * 0.18f);
            float alto = g.Suelo - g.TroncoArriba;
            for (int i = 0; i < n; i++)
            {
                float y = g.Suelo - alto * (i + 1) / (n + 1);
                c.DrawLine(g.CentroX - g.TroncoAncho * 0.35f, y + 2, g.CentroX + g.TroncoAncho * 0.35f, y - 2);
            }
        }
        else
        {
            c.StrokeSize = s * 0.008f;
            for (int i = 0; i < n; i++)
            {
                float x = g.CentroX - s * 0.30f + i * s * 0.03f;
                c.DrawLine(x, g.Suelo + s * 0.07f, x + s * 0.012f, g.Suelo + s * 0.04f);
            }
        }
    }

    private static void Plaga(ICanvas c, GeometriaArbol g)
    {
        var z = g.Zona;
        foreach (var p in PosicionesPlaga)
            DibujarBicho(c, new PointF(z.X + z.Width * p.X, z.Y + z.Height * p.Y), g.Escala * 0.022f);
    }

    public static void DibujarBicho(ICanvas c, PointF p, float r)
    {
        c.Relleno(Bicho);
        c.FillEllipse(p.X - r, p.Y - r * 0.75f, r * 2, r * 1.5f);
        c.StrokeColor = Bicho;
        c.StrokeSize = Math.Max(1, r * 0.22f);
        for (int i = -1; i <= 1; i++)
        {
            c.DrawLine(p.X - r * 0.6f, p.Y + i * r * 0.4f, p.X - r * 1.5f, p.Y + i * r * 0.7f);
            c.DrawLine(p.X + r * 0.6f, p.Y + i * r * 0.4f, p.X + r * 1.5f, p.Y + i * r * 0.7f);
        }
        c.Relleno(Color.FromArgb("#9C6FA8"));
        c.FillCircle(p.X, p.Y - r * 0.2f, r * 0.3f);
    }

    public static LinearGradientPaint Degradado(Color a, Color b, bool horizontal = false) =>
        new([new PaintGradientStop(0, a), new PaintGradientStop(1, b)],
            new Point(0, 0), horizontal ? new Point(1, 0) : new Point(0, 1));
}

/// <summary>Árbol pequeño para las parcelas del jardín.</summary>
public sealed class ArbolPequeno(EstadoArbol estado) : IDrawable
{
    public EstadoArbol Estado { get; set; } = estado;

    public void Draw(ICanvas canvas, RectF dirtyRect) => Arbol.Dibujar(canvas, dirtyRect, Estado, conCielo: false);
}
