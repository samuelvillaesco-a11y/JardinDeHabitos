using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

/// <summary>El color de la fruta de cada especie. Las especies se definen una sola vez, en Core (<see cref="EspeciesDeArbol"/>).</summary>
public static class Especies
{
    private static readonly Dictionary<string, Color> Frutas = new()
    {
        ["Manzano"] = Color.FromArgb("#C8553D"),
        ["Naranjo"] = Color.FromArgb("#F08A24"),
        ["Limonero"] = Color.FromArgb("#F2D13D"),
        ["Cerezo"] = Color.FromArgb("#A3263A"),
        ["Durazno"] = Color.FromArgb("#F2A65A"),
        ["Higuera"] = Color.FromArgb("#7A4466"),
        ["Aguacate"] = Color.FromArgb("#5C7A29"),
        ["Mango"] = Color.FromArgb("#F5B335"),
        ["Granado"] = Color.FromArgb("#B0283C"),
        ["Olivo"] = Color.FromArgb("#4B3B47"),
    };

    static Especies() =>
        System.Diagnostics.Debug.Assert(EspeciesDeArbol.Todas.All(Frutas.ContainsKey), "Falta el color de fruta de una especie nueva.");

    public static Color ColorFruta(string especie) => Frutas.GetValueOrDefault(especie, Frutas["Manzano"]);
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

    // Estado de animación (lo pone la escena; en el jardín y los avisos quedan en su valor final).
    /// <summary>Qué tan mojada está la tierra, de 0 a 1 (al regar se moja poco a poco).</summary>
    public float? Humedad { get; set; }
    /// <summary>Cuánto se ha abierto la grieta más nueva, de 0 a 1.</summary>
    public float GrietaNueva { get; set; } = 1;
    /// <summary>Cuánto han subido los bichos desde el suelo, de 0 a 1.</summary>
    public float LlegadaPlaga { get; set; } = 1;
    /// <summary>Segundos corridos, para que los bichos se muevan un poco.</summary>
    public float Reloj { get; set; }
    /// <summary>Frutos ya cortados hoy (índices), que no se pintan en la copa.</summary>
    public IReadOnlySet<int> Cosechados { get; set; } = new HashSet<int>();

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

    /// <summary>Solo la tierra (la escena la pinta aparte para que no se mueva con el árbol).</summary>
    public static void Tierra(ICanvas c, RectF r, EstadoArbol e) =>
        Monticulo(c, Geometria(r, e.Etapa), e.Humedad ?? (e.Regado ? 1 : 0));

    public static void Dibujar(ICanvas c, RectF r, EstadoArbol e, bool conCielo, bool conTierra = true)
    {
        var g = Geometria(r, e.Etapa);
        float s = g.Escala;

        if (conCielo) Cielo(c, r, DateTime.Now);
        if (e.EnPaz) Paz(c, g);
        if (e.Etapa == GrowthStage.FrutaLegendaria) Resplandor(c, g);

        if (conTierra) Monticulo(c, g, e.Humedad ?? (e.Regado ? 1 : 0));
        if (e.RaicesPodridas) Raices(c, g);

        switch (e.Etapa)
        {
            case GrowthStage.Semilla:
                // Semilla grande y clara, medio enterrada, con la punta del brote asomando.
                c.Relleno(Semilla);
                c.FillEllipse(g.CentroX - s * 0.06f, g.Suelo - s * 0.06f, s * 0.12f, s * 0.085f);
                c.Relleno(Colors.White.WithAlpha(0.25f));
                c.FillEllipse(g.CentroX - s * 0.035f, g.Suelo - s * 0.05f, s * 0.04f, s * 0.025f);
                c.StrokeColor = Naturaleza.Hoja;
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
        if (e.Plaga) Plaga(c, g, e);
    }

    // Cielo de día, de ocaso y de noche (arriba, abajo).
    private static readonly (Color A, Color B) CieloDia = (CieloArriba, CieloAbajo);
    private static readonly (Color A, Color B) CieloOcaso = (Color.FromArgb("#E88F6A"), Color.FromArgb("#6B5A4E"));
    private static readonly (Color A, Color B) CieloNoche = (Color.FromArgb("#1F2A44"), Color.FromArgb("#3A4A3A"));
    private const float Amanecer = 6.5f, Anochecer = 19.5f;

    /// <summary>Cielo según la hora: el sol cruza de día, la luna (en su fase real) de noche.</summary>
    public static void Cielo(ICanvas c, RectF r) => Cielo(c, r, DateTime.Now);

    public static void Cielo(ICanvas c, RectF r, DateTime ahora)
    {
        float h = ahora.Hour + ahora.Minute / 60f;
        float dia = (h - Amanecer) / (Anochecer - Amanecer);              // 0..1 mientras hay sol
        float altura = dia is >= 0 and <= 1 ? MathF.Sin(MathF.PI * dia) : -0.4f;
        // Mezcla: noche → ocaso (sol cerca del horizonte) → día.
        float aOcaso = Math.Clamp((altura + 0.15f) / 0.3f, 0, 1);
        float aDia = Math.Clamp((altura - 0.15f) / 0.35f, 0, 1);
        Color Mezcla(Color n, Color o, Color d) => Lerp(Lerp(n, o, aOcaso), d, aDia);
        var arriba = Mezcla(CieloNoche.A, CieloOcaso.A, CieloDia.A);
        var abajo = Mezcla(CieloNoche.B, CieloOcaso.B, CieloDia.B);
        c.SetFillPaint(Degradado(arriba, abajo), r);
        c.FillRoundedRectangle(r, 24);

        float horizonte = r.Top + r.Height * 0.78f;
        float s = Math.Min(r.Width, r.Height);
        float noche = 1 - aOcaso;
        if (noche > 0)
        {
            c.Relleno(Colors.White.WithAlpha(0.8f * noche));
            foreach (var (x, y, t) in Estrellas) c.FillCircle(r.Left + r.Width * x, r.Top + r.Height * y, t * s * 0.006f);
        }

        if (dia is >= -0.03f and <= 1.03f)
        {
            // Sol: de izquierda (este) a derecha (oeste) en arco; más cálido cerca del horizonte.
            float x = r.Left + r.Width * (0.08f + 0.84f * Math.Clamp(dia, 0, 1));
            float y = horizonte - MathF.Max(altura, -0.05f) * r.Height * 0.62f;
            float radio = s * 0.075f;
            var color = Lerp(Color.FromArgb("#F6B26B"), Color.FromArgb("#FBE3A0"), aDia);
            c.Relleno(color.WithAlpha(0.25f));
            c.FillCircle(x, y, radio * 1.6f);
            c.Relleno(color.WithAlpha(0.9f));
            c.FillCircle(x, y, radio);
        }
        else
        {
            // Luna: cruza la noche de 19:30 a 6:30.
            float n = ((h - Anochecer + 24) % 24) / (24 - (Anochecer - Amanecer));
            float x = r.Left + r.Width * (0.08f + 0.84f * n);
            float y = horizonte - MathF.Sin(MathF.PI * n) * r.Height * 0.55f;
            Luna(c, new PointF(x, y), s * 0.06f, FaseLunar(ahora), arriba);
        }
    }

    private static readonly (float X, float Y, float T)[] Estrellas =
    [
        (0.12f, 0.12f, 1.0f), (0.27f, 0.25f, 0.7f), (0.41f, 0.08f, 0.8f), (0.58f, 0.20f, 1.1f), (0.71f, 0.10f, 0.7f),
        (0.86f, 0.28f, 0.9f), (0.20f, 0.40f, 0.6f), (0.50f, 0.34f, 0.6f), (0.92f, 0.07f, 0.8f), (0.35f, 0.47f, 0.5f),
    ];

    /// <summary>Edad de la luna de 0 a 1 (0 = nueva, 0.5 = llena), desde la luna nueva del 6-ene-2000.</summary>
    public static float FaseLunar(DateTime ahora)
    {
        const double Sinodico = 29.530588853;
        double dias = (ahora.ToUniversalTime() - new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc)).TotalDays;
        double edad = dias % Sinodico;
        if (edad < 0) edad += Sinodico;
        return (float)(edad / Sinodico);
    }

    private static void Luna(ICanvas c, PointF centro, float radio, float fase, Color cielo)
    {
        c.Relleno(Color.FromArgb("#F4EEDF").WithAlpha(0.18f));
        c.FillCircle(centro, radio * 1.7f);
        c.Relleno(Color.FromArgb("#F4EEDF"));
        c.FillCircle(centro, radio);
        // Sombra: un disco del color del cielo que se corre según la fase (creciente: luz a la derecha).
        float desplazamiento = fase <= 0.5f ? -4 * radio * fase : 4 * radio * (1 - fase);
        var borde = new PathF();
        borde.AppendCircle(centro.X, centro.Y, radio);
        c.SaveState();
        c.ClipPath(borde);
        c.Relleno(cielo.WithAlpha(0.88f));
        c.FillCircle(centro.X + desplazamiento, centro.Y, radio * 1.02f);
        c.RestoreState();
    }

    private static Color Lerp(Color a, Color b, float t) =>
        new(a.Red + (b.Red - a.Red) * t, a.Green + (b.Green - a.Green) * t, a.Blue + (b.Blue - a.Blue) * t, a.Alpha + (b.Alpha - a.Alpha) * t);

    private static void Paz(ICanvas c, GeometriaArbol g)
    {
        // Halo cálido y difuso: el árbol descansa en paz.
        var z = g.Zona;
        var area = new RectF(z.X - z.Width * 0.2f, z.Y - z.Height * 0.2f, z.Width * 1.4f, z.Height * 1.4f);
        c.SetFillPaint(new RadialGradientPaint(
            [new PaintGradientStop(0, Naturaleza.Miel.WithAlpha(0.28f)), new PaintGradientStop(1, Naturaleza.Miel.WithAlpha(0f))],
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

    private static void Monticulo(ICanvas c, GeometriaArbol g, float humedad)
    {
        float s = g.Escala;
        var m = new RectF(g.CentroX - s * 0.40f, g.Suelo - s * 0.06f, s * 0.80f, s * 0.18f);
        c.SetFillPaint(Degradado(Naturaleza.Tierra, Naturaleza.TierraOscura), m);
        c.FillEllipse(m);
        if (humedad <= 0) return;
        // La mancha húmeda crece desde el tronco y se oscurece mientras se riega.
        float h = Math.Clamp(humedad, 0, 1);
        float ancho = s * (0.14f + 0.50f * h), alto = s * (0.04f + 0.08f * h);
        c.Relleno(Color.FromArgb("#3A2618").WithAlpha(0.30f + 0.40f * h));
        c.FillEllipse(g.CentroX - ancho / 2, g.Suelo - alto * 0.35f, ancho, alto);
        c.Relleno(Colors.White.WithAlpha(0.10f * h));
        c.FillEllipse(g.CentroX - ancho * 0.25f, g.Suelo - alto * 0.25f, ancho * 0.3f, alto * 0.3f);
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
        c.StrokeColor = Naturaleza.HojaOscura;
        c.StrokeSize = s * 0.014f;
        c.StrokeLineCap = LineCap.Round;
        var p = new PathF(g.CentroX, g.Suelo);
        p.QuadTo(g.CentroX - s * 0.02f, g.Suelo - s * 0.08f, g.CentroX, g.TroncoArriba);
        c.DrawPath(p);
        HojaSuelta(c, g.CentroX, g.TroncoArriba + s * 0.02f, s * 0.08f, -0.5f);
        HojaSuelta(c, g.CentroX, g.TroncoArriba + s * 0.05f, s * 0.07f, 3.6f);
    }

    public static void HojaSuelta(ICanvas c, float x, float y, float largo, float angulo)
    {
        c.SaveState();
        c.Translate(x, y);
        c.Rotate(angulo * 180f / MathF.PI);
        var p = new PathF(0, 0);
        p.QuadTo(largo * 0.5f, -largo * 0.45f, largo, 0);
        p.QuadTo(largo * 0.5f, largo * 0.45f, 0, 0);
        p.Close();
        c.SetFillPaint(Degradado(Naturaleza.Hoja, Naturaleza.HojaOscura), new RectF(0, -largo * 0.3f, largo, largo * 0.6f));
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
        c.SetFillPaint(Degradado(CortezaClara, Naturaleza.Corteza, horizontal: true),
            new RectF(g.CentroX - w, g.TroncoArriba, w * 2, g.Suelo - g.TroncoArriba));
        c.FillPath(p);
    }

    private static void Copa(ICanvas c, GeometriaArbol g, bool enferma)
    {
        var z = g.Copa;
        c.SetFillPaint(enferma ? Degradado(CopaEnfermaArriba, CopaEnfermaAbajo) : Degradado(Naturaleza.Hoja, Naturaleza.HojaOscura), z);
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
                    c.Relleno(Naturaleza.Miel);
                    c.FillCircle(x, y, r * 0.45f);
                }
                break;
            case GrowthStage.FrutaInmadura:
                Frutas(c, z, FrutaVerde, r * 0.9f, 6);
                break;
            case GrowthStage.FrutaComestible:
                Frutas(c, z, Especies.ColorFruta(e.Especie), r * 1.2f, 7, e.Cosechados);
                break;
            case GrowthStage.FrutaLegendaria:
                Frutas(c, z, Oro, r * 1.35f, 8, e.Cosechados);
                c.Relleno(Colors.White.WithAlpha(0.9f));
                foreach (var p in new[] { new PointF(0.1f, 0.1f), new PointF(0.95f, 0.2f), new PointF(0.05f, 0.85f) })
                    Destello(c, z.X + z.Width * p.X, z.Y + z.Height * p.Y, r * 1.2f);
                break;
        }
    }

    /// <summary>Frutos maduros que se pueden cosechar: posición, radio y color (vacío si la etapa no tiene).</summary>
    public static IReadOnlyList<(PointF Pos, float R, Color Color)> FrutosMaduros(GeometriaArbol g, EstadoArbol e)
    {
        var z = g.Copa;
        float r = z.Width * 0.045f;
        (int n, float radio, Color color) = e.Etapa switch
        {
            GrowthStage.FrutaComestible => (7, r * 1.2f, Especies.ColorFruta(e.Especie)),
            GrowthStage.FrutaLegendaria => (8, r * 1.35f, Oro),
            _ => (0, 0f, Colors.Transparent),
        };
        return [.. Adornos.Take(n).Select(p => (new PointF(z.X + z.Width * p.X, z.Y + z.Height * p.Y), radio, color))];
    }

    private static void Frutas(ICanvas c, RectF z, Color color, float r, int cuantas, IReadOnlySet<int>? cosechados = null)
    {
        foreach (var (p, i) in Adornos.Take(cuantas).Select((p, i) => (p, i)))
        {
            if (cosechados?.Contains(i) == true) continue;
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

    /// <summary>
    /// Cicatrices: grietas oscuras en el tronco (o en la tierra si aún es pequeña). La más nueva se
    /// abre según <see cref="EstadoArbol.GrietaNueva"/>.
    /// </summary>
    private static void Cicatrices(ICanvas c, GeometriaArbol g, EstadoArbol e)
    {
        int n = Math.Min(e.Cicatrices, 8);
        if (n == 0) return;
        float s = g.Escala;
        bool hayTronco = e.Etapa >= GrowthStage.ArbolJoven;
        c.StrokeLineCap = LineCap.Round;
        c.StrokeLineJoin = LineJoin.Round;
        for (int i = 0; i < n; i++)
        {
            float avance = i == n - 1 ? Math.Clamp(e.GrietaNueva, 0, 1) : 1;
            if (avance <= 0) continue;
            PointF[] zigzag;
            float grosor;
            if (hayTronco)
            {
                float alto = g.Suelo - g.TroncoArriba, w = g.TroncoAncho;
                float y0 = g.Suelo - alto * (i + 0.6f) / (n + 0.5f);
                float x0 = g.CentroX + (i % 2 == 0 ? -0.15f : 0.12f) * w;
                zigzag = [new(x0, y0), new(x0 + w * 0.18f, y0 + alto * 0.06f), new(x0 - w * 0.10f, y0 + alto * 0.12f), new(x0 + w * 0.12f, y0 + alto * 0.19f)];
                grosor = Math.Max(1.5f, w * 0.12f);
            }
            else
            {
                float x0 = g.CentroX + s * (-0.26f + i * 0.08f), y0 = g.Suelo + s * 0.005f;
                zigzag = [new(x0, y0), new(x0 + s * 0.02f, y0 + s * 0.025f), new(x0 - s * 0.005f, y0 + s * 0.045f), new(x0 + s * 0.015f, y0 + s * 0.07f)];
                grosor = s * 0.007f;
            }
            var path = Parcial(zigzag, avance);
            c.StrokeColor = Color.FromArgb("#2A1A10");
            c.StrokeSize = grosor;
            c.DrawPath(path);
            // Borde claro: la madera viva bajo la corteza.
            c.StrokeColor = Color.FromArgb("#C9A27A").WithAlpha(0.45f);
            c.StrokeSize = grosor * 0.35f;
            c.SaveState();
            c.Translate(grosor * 0.6f, 0);
            c.DrawPath(path);
            c.RestoreState();
        }
    }

    /// <summary>Línea quebrada recortada a una fracción de su largo.</summary>
    private static PathF Parcial(PointF[] puntos, float avance)
    {
        var path = new PathF(puntos[0]);
        int tramos = puntos.Length - 1;
        float hasta = avance * tramos;
        for (int i = 0; i < tramos; i++)
        {
            float f = Math.Clamp(hasta - i, 0, 1);
            if (f <= 0) break;
            var (a, b) = (puntos[i], puntos[i + 1]);
            path.LineTo(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f);
        }
        return path;
    }

    private static void Plaga(ICanvas c, GeometriaArbol g, EstadoArbol e)
    {
        var z = g.Zona;
        float llegada = Math.Clamp(e.LlegadaPlaga, 0, 1);
        for (int i = 0; i < PosicionesPlaga.Length; i++)
        {
            var p = PosicionesPlaga[i];
            var destino = new PointF(z.X + z.Width * p.X, z.Y + z.Height * p.Y);
            // Suben desde la tierra uno tras otro y luego caminan un poquito en su lugar.
            float f = Math.Clamp(llegada * 1.6f - i * 0.12f, 0, 1);
            var origen = new PointF(g.CentroX + (destino.X - g.CentroX) * 0.3f, g.Suelo);
            float x = origen.X + (destino.X - origen.X) * f + MathF.Sin(e.Reloj * 5 + i * 1.7f) * g.Escala * 0.005f;
            float y = origen.Y + (destino.Y - origen.Y) * f + MathF.Cos(e.Reloj * 4 + i) * g.Escala * 0.003f;
            if (f > 0) DibujarBicho(c, new PointF(x, y), g.Escala * 0.022f);
        }
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
