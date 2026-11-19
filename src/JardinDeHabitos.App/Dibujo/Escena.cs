using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

public enum MiniJuego
{
    Ninguno,
    Regar,   // arrastrar la regadera sobre el árbol
    Podar,   // deslizar el dedo para cortar ramas sueltas
    Plaga,   // tocar cada bicho
    Raices,  // arrastrar raíces podridas fuera de la tierra
}

/// <summary>
/// El árbol grande con cielo y los mini-juegos de cuidado. Todo con gestos: tocar y arrastrar.
/// </summary>
public sealed class Escena : IDrawable
{
    private const float SegundosDeRiego = 1.6f;

    private sealed class Raiz
    {
        public PointF Origen;
        public PointF Pos;
    }

    private sealed class Gota
    {
        public PointF Pos;
        public float Vy;
    }

    private RectF _rect;
    private bool _pendienteIniciar;
    private readonly List<PointF> _bichos = [];
    private readonly List<Raiz> _raices = [];
    private readonly List<(PointF A, PointF B)> _ramas = [];
    private readonly List<(PointF A, PointF B)> _cortes = [];
    private readonly List<Gota> _gotas = [];
    private PointF _regadera;
    private bool _agarrandoRegadera;
    private float _riego;
    private Raiz? _raizAgarrada;
    private PointF? _ultimoToque;

    public EstadoArbol Arbol { get; set; } = new();
    public MiniJuego Juego { get; private set; }

    /// <summary>Se llama cuando el mini-juego en curso se completa.</summary>
    public event Action<MiniJuego>? Completado;

    /// <summary>Avance de 0 a 1 del mini-juego actual (para el texto de ayuda).</summary>
    public string Progreso => Juego switch
    {
        MiniJuego.Plaga => $"Quedan {_bichos.Count} bichos.",
        MiniJuego.Raices => $"Quedan {_raices.Count} raíces podridas.",
        MiniJuego.Podar => $"Quedan {_ramas.Count} por cortar.",
        MiniJuego.Regar => $"{Math.Min(100, (int)(_riego / SegundosDeRiego * 100))}% regado.",
        _ => "",
    };

    public void Iniciar(MiniJuego juego)
    {
        Juego = juego;
        _pendienteIniciar = true;
        _bichos.Clear();
        _raices.Clear();
        _ramas.Clear();
        _cortes.Clear();
        _gotas.Clear();
        _riego = 0;
        _agarrandoRegadera = false;
        _raizAgarrada = null;
        _ultimoToque = null;
    }

    public void Cancelar() => Juego = MiniJuego.Ninguno;

    public void Draw(ICanvas canvas, RectF rect)
    {
        _rect = rect;
        if (_pendienteIniciar) Preparar();

        // Durante la plaga interactiva los bichos los pinta la escena, no el árbol.
        var estado = Arbol;
        if (Juego is MiniJuego.Plaga or MiniJuego.Raices)
        {
            estado = Copia(Arbol);
            estado.Plaga = false;
            estado.RaicesPodridas = false;
        }
        Dibujo.Arbol.Dibujar(canvas, rect, estado, conCielo: true);

        var g = Geometria;
        switch (Juego)
        {
            case MiniJuego.Plaga:
                foreach (var b in _bichos) Dibujo.Arbol.DibujarBicho(canvas, b, g.Escala * 0.03f);
                break;
            case MiniJuego.Raices:
                canvas.StrokeLineCap = LineCap.Round;
                foreach (var r in _raices)
                {
                    canvas.StrokeColor = Color.FromArgb("#5B5236");
                    canvas.StrokeSize = g.Escala * 0.02f;
                    canvas.DrawLine(r.Pos.X, r.Pos.Y - g.Escala * 0.05f, r.Pos.X, r.Pos.Y + g.Escala * 0.03f);
                    canvas.Relleno(Color.FromArgb("#3E3826"));
                    canvas.FillCircle(r.Pos.X, r.Pos.Y + g.Escala * 0.03f, g.Escala * 0.025f);
                }
                break;
            case MiniJuego.Podar:
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeColor = Tema.Corteza;
                canvas.StrokeSize = g.Escala * 0.014f;
                foreach (var (a, b) in _ramas)
                {
                    canvas.DrawLine(a, b);
                    canvas.Relleno(Tema.HojaOscura);
                    canvas.FillCircle(b, g.Escala * 0.018f);
                }
                canvas.StrokeColor = Tema.Hueso.WithAlpha(0.6f);
                canvas.StrokeSize = 3;
                foreach (var (a, b) in _cortes.TakeLast(12)) canvas.DrawLine(a, b);
                break;
            case MiniJuego.Regar:
                canvas.Relleno(Color.FromArgb("#9CC9D9"));
                foreach (var gota in _gotas) canvas.FillEllipse(gota.Pos.X - 3, gota.Pos.Y - 5, 6, 10);
                DibujarRegadera(canvas, _regadera, g.Escala);
                break;
        }
    }

    /// <summary>Avanza animaciones. Devuelve true si hay que repintar.</summary>
    public bool Tick(float dt)
    {
        if (Juego != MiniJuego.Regar) return false;

        var g = Geometria;
        foreach (var gota in _gotas)
        {
            gota.Vy += 900 * dt;
            gota.Pos = new PointF(gota.Pos.X, gota.Pos.Y + gota.Vy * dt);
        }
        _gotas.RemoveAll(x => x.Pos.Y > g.Suelo);

        var pico = PicoRegadera(_regadera, g.Escala);
        var zona = g.Zona;
        bool sobreArbol = _agarrandoRegadera
            && pico.X >= zona.Left - g.Escala * 0.05f && pico.X <= zona.Right + g.Escala * 0.05f
            && pico.Y <= zona.Bottom;
        if (sobreArbol)
        {
            _riego += dt;
            if (Random.Shared.NextSingle() < 0.6f)
                _gotas.Add(new Gota { Pos = new PointF(pico.X + Random.Shared.Next(-6, 6), pico.Y), Vy = 60 });
            if (_riego >= SegundosDeRiego) Terminar();
        }
        return true;
    }

    public void Tocar(PointF p)
    {
        _ultimoToque = p;
        var g = Geometria;
        switch (Juego)
        {
            case MiniJuego.Plaga:
                float radio = g.Escala * 0.07f;
                var bicho = _bichos.FirstOrDefault(b => Distancia(b, p) < radio, new PointF(float.NaN, float.NaN));
                if (!float.IsNaN(bicho.X))
                {
                    _bichos.Remove(bicho);
                    if (_bichos.Count == 0) Terminar();
                }
                break;
            case MiniJuego.Raices:
                _raizAgarrada = _raices
                    .Where(r => Distancia(r.Pos, p) < g.Escala * 0.09f)
                    .OrderBy(r => Distancia(r.Pos, p))
                    .FirstOrDefault();
                break;
            case MiniJuego.Regar:
                _agarrandoRegadera = Distancia(p, _regadera) < g.Escala * 0.16f;
                break;
        }
    }

    public void Arrastrar(PointF p)
    {
        var g = Geometria;
        switch (Juego)
        {
            case MiniJuego.Raices when _raizAgarrada is not null:
                _raizAgarrada.Pos = p;
                break;
            case MiniJuego.Regar when _agarrandoRegadera:
                _regadera = p;
                break;
            case MiniJuego.Podar when _ultimoToque is { } antes:
                _cortes.Add((antes, p));
                int cortadas = _ramas.RemoveAll(r => SeCruzan(antes, p, r.A, r.B));
                if (cortadas > 0 && _ramas.Count == 0) Terminar();
                break;
        }
        _ultimoToque = p;
    }

    public void Soltar()
    {
        var g = Geometria;
        if (Juego == MiniJuego.Raices && _raizAgarrada is { } r)
        {
            bool fuera = r.Pos.Y < g.Suelo - g.Escala * 0.10f || Distancia(r.Pos, r.Origen) > g.Escala * 0.30f;
            if (fuera)
            {
                _raices.Remove(r);
                if (_raices.Count == 0) Terminar();
            }
            else
            {
                r.Pos = r.Origen;
            }
        }
        _raizAgarrada = null;
        _agarrandoRegadera = false;
        _ultimoToque = null;
    }

    private GeometriaArbol Geometria => Dibujo.Arbol.Geometria(_rect, Arbol.Etapa);

    private void Preparar()
    {
        if (_rect.Width <= 0) return;
        _pendienteIniciar = false;
        var g = Geometria;
        var z = g.Zona;
        float s = g.Escala;

        switch (Juego)
        {
            case MiniJuego.Plaga:
                foreach (var p in Dibujo.Arbol.PosicionesPlaga)
                    _bichos.Add(new PointF(z.X + z.Width * p.X, z.Y + z.Height * p.Y));
                break;
            case MiniJuego.Raices:
                foreach (var dx in new[] { -0.18f, 0f, 0.18f })
                {
                    var o = new PointF(g.CentroX + s * dx, g.Suelo + s * 0.06f);
                    _raices.Add(new Raiz { Origen = o, Pos = o });
                }
                break;
            case MiniJuego.Podar:
                if (g.Copa.Width > 0)
                {
                    // Ramas sueltas que salen de la copa.
                    _ramas.Add((new PointF(z.Left + z.Width * 0.08f, z.Center.Y), new PointF(z.Left - s * 0.12f, z.Center.Y - s * 0.06f)));
                    _ramas.Add((new PointF(z.Right - z.Width * 0.08f, z.Center.Y + z.Height * 0.1f), new PointF(z.Right + s * 0.12f, z.Center.Y - s * 0.02f)));
                    _ramas.Add((new PointF(z.Center.X + z.Width * 0.1f, z.Top + z.Height * 0.08f), new PointF(z.Center.X + s * 0.06f, z.Top - s * 0.10f)));
                }
                else
                {
                    // Planta pequeña: se quitan hierbas alrededor.
                    foreach (var dx in new[] { -0.26f, 0.22f, 0.32f })
                        _ramas.Add((new PointF(g.CentroX + s * dx, g.Suelo), new PointF(g.CentroX + s * (dx + 0.02f), g.Suelo - s * 0.10f)));
                }
                break;
            case MiniJuego.Regar:
                _regadera = new PointF(_rect.Left + s * 0.16f, _rect.Bottom - s * 0.14f);
                break;
        }
    }

    private void Terminar()
    {
        var hecho = Juego;
        Juego = MiniJuego.Ninguno;
        Completado?.Invoke(hecho);
    }

    private static PointF PicoRegadera(PointF centro, float s) => new(centro.X + s * 0.10f, centro.Y - s * 0.07f);

    private static void DibujarRegadera(ICanvas c, PointF p, float s)
    {
        float w = s * 0.14f, h = s * 0.10f;
        c.Relleno(Color.FromArgb("#5F8FA0"));
        c.FillRoundedRectangle(p.X - w / 2, p.Y - h / 2, w, h, s * 0.02f);
        c.StrokeColor = Color.FromArgb("#5F8FA0");
        c.StrokeSize = s * 0.018f;
        c.StrokeLineCap = LineCap.Round;
        c.DrawLine(p.X + w * 0.4f, p.Y, PicoRegadera(p, s).X, PicoRegadera(p, s).Y);
        c.StrokeSize = s * 0.012f;
        c.DrawArc(p.X - w * 0.35f, p.Y - h * 1.0f, w * 0.7f, h * 0.9f, 0, 180, false, false);
        c.Relleno(Colors.White.WithAlpha(0.25f));
        c.FillRoundedRectangle(p.X - w * 0.4f, p.Y - h * 0.4f, w * 0.25f, h * 0.6f, s * 0.01f);
    }

    private static float Distancia(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static bool SeCruzan(PointF p1, PointF p2, PointF q1, PointF q2)
    {
        static float Cruz(PointF a, PointF b, PointF c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        float d1 = Cruz(q1, q2, p1), d2 = Cruz(q1, q2, p2), d3 = Cruz(p1, p2, q1), d4 = Cruz(p1, p2, q2);
        return d1 * d2 < 0 && d3 * d4 < 0;
    }

    private static EstadoArbol Copia(EstadoArbol e) => new()
    {
        Etapa = e.Etapa,
        Especie = e.Especie,
        Cicatrices = e.Cicatrices,
        Plaga = e.Plaga,
        RaicesPodridas = e.RaicesPodridas,
        EnPaz = e.EnPaz,
        Regado = e.Regado,
    };
}
