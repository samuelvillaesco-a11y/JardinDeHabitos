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

/// <summary>Animaciones cortas del árbol (todas se saltan con "reducir movimiento").</summary>
public enum Efecto
{
    Ninguno,
    Crecer,    // cambió de etapa: crece con rebote y suelta hojas, pétalos o destellos
    Alivio,    // check-in cumplido: respira hondo dos veces
    Grieta,    // check-in roto: se sacude, se abre una grieta y llegan los bichos
    Sacudida,  // después de podar: se mece y caen hojas que luego desaparecen
}

/// <summary>
/// El árbol grande con cielo y los mini-juegos de cuidado. Todo con gestos: tocar y arrastrar.
/// </summary>
public sealed class Escena : IDrawable
{
    private sealed class Particula
    {
        public PointF Pos;
        public PointF Vel;
        public float Giro;
        public float Vida;          // segundos que le quedan
        public float Total;
        public Color Color = Colors.White;
        public char Tipo;           // 'h' hoja, 'p' pétalo, 'd' destello, 'f' fruto hacia la canasta
        public PointF Destino;
        public bool EnSuelo;
    }

    private readonly List<Particula> _particulas = [];
    private Efecto _efecto;
    private float _tEfecto;
    private GrowthStage _etapaAntes;
    private float _reloj;
    private readonly HashSet<int> _cosechados = [];
    private int _enCanasta;

    /// <summary>Con "reducir movimiento" del sistema no se anima nada: se ve el estado final.</summary>
    public bool MovimientoReducido { get; set; }

    /// <summary>Hora que se pinta en el cielo (sol o luna).</summary>
    public DateTime Ahora { get; set; } = DateTime.Now;

    /// <summary>Se pueden cortar los frutos maduros (una vez al día).</summary>
    public bool PuedeCosechar { get; set; }

    /// <summary>Se cortaron todos los frutos maduros; lleva cuántos.</summary>
    public event Action<int>? Cosechado;

    public bool Animando => _efecto != Efecto.Ninguno || _particulas.Count > 0 || (Arbol.Plaga && !MovimientoReducido);

    /// <summary>Segundos que dura un efecto (para esperar antes de mostrar un diálogo).</summary>
    public static float Duracion(Efecto e) => e switch
    {
        Efecto.Crecer => 1.8f,
        Efecto.Alivio => 2.6f,
        Efecto.Grieta => 2.8f,
        Efecto.Sacudida => 1.4f,
        _ => 0,
    };

    public void Animar(Efecto efecto, GrowthStage? etapaAntes = null)
    {
        if (MovimientoReducido || _rect.Width <= 0) return;
        _efecto = efecto;
        _tEfecto = 0;
        _etapaAntes = etapaAntes ?? Arbol.Etapa;
        if (efecto == Efecto.Sacudida) SoltarHojas(5);
    }
    private const float SegundosDeRiegoBase = 1.6f;
    private static readonly Color RaizTallo = Color.FromArgb("#5B5236");
    private static readonly Color RaizBola = Color.FromArgb("#3E3826");

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

    /// <summary>Efectos de logros malditos: más bichos, tierra que se seca, más ramas, raíces tercas.</summary>
    public IReadOnlySet<Debuff> Debuffs { get; set; } = new HashSet<Debuff>();

    /// <summary>Juego corto de emergencia (mini-evento): pocas piezas, se resuelve en segundos.</summary>
    public bool Rescate { get; private set; }

    /// <summary>Piezas quitadas en el juego actual (bichos, raíces o ramas).</summary>
    public int Piezas { get; private set; }

    private float SegundosDeRiego =>
        Rescate ? 0.6f : SegundosDeRiegoBase * (Debuffs.Contains(Debuff.TierraSeca) ? 1.6f : 1f);

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

    public void Iniciar(MiniJuego juego, bool rescate = false)
    {
        Juego = juego;
        Rescate = rescate;
        Piezas = 0;
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
        var g = Geometria;
        estado = Animado(estado);
        Dibujo.Arbol.Cielo(canvas, rect, Ahora);
        if (_efecto == Efecto.Alivio) Halo(canvas, g);
        Dibujo.Arbol.Tierra(canvas, rect, estado);
        canvas.SaveState();
        Transformar(canvas, g);
        Dibujo.Arbol.Dibujar(canvas, rect, estado, conCielo: false, conTierra: false);
        canvas.RestoreState();
        DibujarParticulas(canvas, g);
        if (PuedeCosechar || _enCanasta > 0) Canasta(canvas, g);

        switch (Juego)
        {
            case MiniJuego.Plaga:
                foreach (var b in _bichos) Dibujo.Arbol.DibujarBicho(canvas, b, g.Escala * 0.03f);
                break;
            case MiniJuego.Raices:
                canvas.StrokeLineCap = LineCap.Round;
                foreach (var r in _raices)
                {
                    canvas.StrokeColor = RaizTallo;
                    canvas.StrokeSize = g.Escala * 0.02f;
                    canvas.DrawLine(r.Pos.X, r.Pos.Y - g.Escala * 0.05f, r.Pos.X, r.Pos.Y + g.Escala * 0.03f);
                    canvas.Relleno(RaizBola);
                    canvas.FillCircle(r.Pos.X, r.Pos.Y + g.Escala * 0.03f, g.Escala * 0.025f);
                }
                break;
            case MiniJuego.Podar:
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeColor = Naturaleza.Corteza;
                canvas.StrokeSize = g.Escala * 0.014f;
                foreach (var (a, b) in _ramas)
                {
                    canvas.DrawLine(a, b);
                    canvas.Relleno(Naturaleza.HojaOscura);
                    canvas.FillCircle(b, g.Escala * 0.018f);
                }
                canvas.StrokeColor = Naturaleza.Hueso.WithAlpha(0.6f);
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
        _reloj += dt;
        bool repintar = Arbol.Plaga && !MovimientoReducido;
        if (_efecto != Efecto.Ninguno)
        {
            float antes = _tEfecto;
            _tEfecto += dt;
            // A media subida de etapa brotan las hojas, los pétalos o los destellos.
            if (_efecto == Efecto.Crecer && antes < 0.6f && _tEfecto >= 0.6f) Celebrar();
            if (_tEfecto >= Duracion(_efecto)) _efecto = Efecto.Ninguno;
            repintar = true;
        }
        if (_particulas.Count > 0)
        {
            MoverParticulas(dt);
            repintar = true;
        }
        if (Juego != MiniJuego.Regar) return repintar;

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
        if (Juego == MiniJuego.Ninguno && PuedeCosechar) Cortar(p, g);
        switch (Juego)
        {
            case MiniJuego.Plaga:
                float radio = g.Escala * 0.07f;
                var bicho = _bichos.FirstOrDefault(b => Distancia(b, p) < radio, new PointF(float.NaN, float.NaN));
                if (!float.IsNaN(bicho.X))
                {
                    _bichos.Remove(bicho);
                    Piezas++;
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
                Piezas += cortadas;
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
            // Raíces tercas: hay que jalarlas más lejos.
            float tirón = Debuffs.Contains(Debuff.RaicesTercas) ? 1.5f : 1f;
            bool fuera = r.Pos.Y < g.Suelo - g.Escala * 0.10f * tirón || Distancia(r.Pos, r.Origen) > g.Escala * 0.30f * tirón;
            if (fuera)
            {
                _raices.Remove(r);
                Piezas++;
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

    // ---------- Efectos ----------

    private static float Suave(float t) => t * t * (3 - 2 * Math.Clamp(t, 0, 1));

    private EstadoArbol Animado(EstadoArbol e)
    {
        var a = Copia(e);
        a.Reloj = _reloj;
        a.Cosechados = _cosechados;
        if (Juego == MiniJuego.Regar) a.Humedad = Math.Clamp(_riego / SegundosDeRiego, 0, 1);
        if (_efecto == Efecto.Grieta)
        {
            a.GrietaNueva = Math.Clamp((_tEfecto - 0.35f) / 0.8f, 0, 1);
            a.LlegadaPlaga = Math.Clamp((_tEfecto - 1.3f) / 1.3f, 0, 1);
        }
        return a;
    }

    private void Transformar(ICanvas c, GeometriaArbol g)
    {
        float t = _tEfecto;
        switch (_efecto)
        {
            case Efecto.Crecer:
            {
                // Desde el tamaño de la etapa anterior hasta un poco más grande, y rebota a su lugar.
                var antes = Dibujo.Arbol.Geometria(_rect, _etapaAntes);
                float inicio = g.Copa.Width > 0 && antes.Copa.Width > 0 ? antes.Copa.Width / g.Copa.Width : 0.55f;
                float k = t < 0.7f
                    ? inicio + (1.08f - inicio) * Suave(t / 0.7f)
                    : 1.08f - 0.08f * Suave(Math.Min(1, (t - 0.7f) / 0.5f));
                c.Translate(g.CentroX, g.Suelo);
                c.Scale(k, k);
                c.Translate(-g.CentroX, -g.Suelo);
                break;
            }
            case Efecto.Alivio:
            {
                // Dos respiraciones hondas: se ensancha un poco y vuelve.
                float r = MathF.Pow(MathF.Sin(MathF.PI * t / 1.3f), 2);
                c.Translate(g.CentroX, g.Suelo);
                c.Scale(1 + 0.02f * r, 1 + 0.045f * r);
                c.Translate(-g.CentroX, -g.Suelo);
                break;
            }
            case Efecto.Grieta when t < 0.4f:
                c.Translate(MathF.Sin(t * 70) * g.Escala * 0.012f * (1 - t / 0.4f), 0);
                break;
            case Efecto.Sacudida:
                c.Rotate(5f * MathF.Sin(t * 16) * MathF.Exp(-2.5f * t), g.CentroX, g.Suelo);
                break;
        }
    }

    private void Halo(ICanvas c, GeometriaArbol g)
    {
        float r = MathF.Pow(MathF.Sin(MathF.PI * _tEfecto / 1.3f), 2);
        var z = g.Zona;
        var area = new RectF(z.X - z.Width * 0.3f, z.Y - z.Height * 0.3f, z.Width * 1.6f, z.Height * 1.6f);
        c.SetFillPaint(new RadialGradientPaint(
            [new PaintGradientStop(0, Color.FromArgb("#FFF3C4").WithAlpha(0.55f * r)), new PaintGradientStop(1, Colors.Transparent)],
            new Point(0.5, 0.5), 0.5), area);
        c.FillEllipse(area);
    }

    private void Celebrar()
    {
        var g = Geometria;
        var z = g.Zona;
        var (tipo, colores) = Arbol.Etapa switch
        {
            GrowthStage.Flor => ('p', new[] { "#F6E4DC", "#F2B8C6", "#FBEFF2" }),
            >= GrowthStage.FrutaInmadura => ('d', new[] { "#F7CF4A", "#FFF3C4", "#F2A65A" }),
            _ => ('h', new[] { "#8DB255", "#6E9A4E", "#A9C77A" }),
        };
        for (int i = 0; i < 16; i++)
        {
            float a = Random.Shared.NextSingle() * MathF.Tau;
            float v = g.Escala * (0.25f + Random.Shared.NextSingle() * 0.35f);
            _particulas.Add(new Particula
            {
                Pos = new PointF(z.Center.X + MathF.Cos(a) * z.Width * 0.3f, z.Center.Y + MathF.Sin(a) * z.Height * 0.3f),
                Vel = new PointF(MathF.Cos(a) * v, MathF.Sin(a) * v - g.Escala * 0.35f),
                Giro = Random.Shared.NextSingle() * 360,
                Vida = 1.6f, Total = 1.6f, Tipo = tipo,
                Color = Color.FromArgb(colores[i % colores.Length]),
            });
        }
    }

    private void SoltarHojas(int cuantas)
    {
        var g = Geometria;
        var z = g.Zona;
        for (int i = 0; i < cuantas; i++)
        {
            _particulas.Add(new Particula
            {
                Pos = new PointF(z.X + z.Width * (0.15f + 0.7f * Random.Shared.NextSingle()), z.Y + z.Height * (0.3f + 0.5f * Random.Shared.NextSingle())),
                Vel = new PointF(0, g.Escala * 0.12f),
                Giro = Random.Shared.NextSingle() * 360,
                Vida = 3.2f, Total = 3.2f, Tipo = 'h',
                Color = Naturaleza.Hoja,
            });
        }
    }

    private void MoverParticulas(float dt)
    {
        var g = Geometria;
        foreach (var q in _particulas)
        {
            q.Vida -= dt;
            if (q.Tipo == 'f')
            {
                // Vuela en arco hasta la canasta.
                float t = 1 - q.Vida / q.Total;
                var desde = q.Vel; // origen guardado en Vel
                float x = desde.X + (q.Destino.X - desde.X) * t;
                float y = desde.Y + (q.Destino.Y - desde.Y) * t - MathF.Sin(MathF.PI * t) * g.Escala * 0.18f;
                q.Pos = new PointF(x, y);
                if (q.Vida <= 0) _enCanasta++;
                continue;
            }
            if (q.EnSuelo) continue;
            if (q.Tipo == 'h')
            {
                // Hoja: cae despacio meciéndose y se queda en el suelo hasta desvanecerse.
                q.Pos = new PointF(q.Pos.X + MathF.Sin(q.Vida * 5) * g.Escala * 0.15f * dt, q.Pos.Y + q.Vel.Y * dt);
                q.Giro += 90 * dt;
                if (q.Pos.Y >= g.Suelo + g.Escala * 0.02f) q.EnSuelo = true;
            }
            else
            {
                q.Vel = new PointF(q.Vel.X * 0.98f, q.Vel.Y + g.Escala * 0.9f * dt);
                q.Pos = new PointF(q.Pos.X + q.Vel.X * dt, q.Pos.Y + q.Vel.Y * dt);
                q.Giro += 180 * dt;
            }
        }
        _particulas.RemoveAll(q => q.Vida <= 0);
    }

    private void DibujarParticulas(ICanvas c, GeometriaArbol g)
    {
        foreach (var q in _particulas)
        {
            float alfa = q.Tipo == 'f' ? 1 : Math.Clamp(q.Vida / Math.Min(1f, q.Total), 0, 1);
            c.SaveState();
            c.Alpha = alfa;
            switch (q.Tipo)
            {
                case 'h':
                    Dibujo.Arbol.HojaSuelta(c, q.Pos.X, q.Pos.Y, g.Escala * 0.05f, q.Giro * MathF.PI / 180);
                    break;
                case 'p':
                    c.Relleno(q.Color);
                    c.Rotate(q.Giro, q.Pos.X, q.Pos.Y);
                    c.FillEllipse(q.Pos.X - g.Escala * 0.012f, q.Pos.Y - g.Escala * 0.007f, g.Escala * 0.024f, g.Escala * 0.014f);
                    break;
                case 'd':
                    c.Relleno(q.Color);
                    c.FillCircle(q.Pos, g.Escala * 0.008f);
                    break;
                case 'f':
                    c.Relleno(q.Color);
                    c.FillCircle(q.Pos, g.Escala * 0.022f);
                    break;
            }
            c.RestoreState();
        }
    }

    // ---------- Cosecha ----------

    private RectF CanastaRect(GeometriaArbol g) =>
        new(_rect.Right - g.Escala * 0.30f, g.Suelo - g.Escala * 0.02f, g.Escala * 0.22f, g.Escala * 0.12f);

    private void Canasta(ICanvas c, GeometriaArbol g)
    {
        var b = CanastaRect(g);
        // Frutos ya en la canasta, asomando.
        var frutos = Dibujo.Arbol.FrutosMaduros(g, Arbol);
        var color = frutos.Count > 0 ? frutos[0].Color : Naturaleza.Miel;
        c.Relleno(color);
        for (int i = 0; i < Math.Min(_enCanasta, 8); i++)
            c.FillCircle(b.X + b.Width * (0.2f + 0.6f * (i % 4) / 3f), b.Y + b.Height * (i < 4 ? 0.15f : -0.05f), g.Escala * 0.022f);
        var cesto = new PathF(b.X, b.Y + b.Height * 0.15f);
        cesto.LineTo(b.Right, b.Y + b.Height * 0.15f);
        cesto.LineTo(b.Right - b.Width * 0.12f, b.Bottom);
        cesto.LineTo(b.X + b.Width * 0.12f, b.Bottom);
        cesto.Close();
        c.SetFillPaint(Dibujo.Arbol.Degradado(Color.FromArgb("#C99A5B"), Color.FromArgb("#8A6232")), b);
        c.FillPath(cesto);
        c.StrokeColor = Color.FromArgb("#6E4B24").WithAlpha(0.6f);
        c.StrokeSize = 1.5f;
        for (int i = 1; i < 4; i++) c.DrawLine(b.X + b.Width * 0.06f * i, b.Y + b.Height * (0.15f + 0.2f * i), b.Right - b.Width * 0.06f * i, b.Y + b.Height * (0.15f + 0.2f * i));
    }

    private void Cortar(PointF p, GeometriaArbol g)
    {
        var frutos = Dibujo.Arbol.FrutosMaduros(g, Arbol);
        for (int i = 0; i < frutos.Count; i++)
        {
            if (_cosechados.Contains(i) || Distancia(frutos[i].Pos, p) > g.Escala * 0.07f) continue;
            _cosechados.Add(i);
            var canasta = CanastaRect(g);
            float vuelo = MovimientoReducido ? 0.01f : 0.7f;
            _particulas.Add(new Particula
            {
                Pos = frutos[i].Pos, Vel = frutos[i].Pos, Tipo = 'f', Color = frutos[i].Color,
                Destino = new PointF(canasta.Center.X, canasta.Y), Vida = vuelo, Total = vuelo,
            });
            if (_cosechados.Count == frutos.Count)
            {
                PuedeCosechar = false;
                Cosechado?.Invoke(frutos.Count);
            }
            return;
        }
    }

    /// <summary>Al cambiar de árbol o de día: frutos de nuevo en su rama y canasta vacía.</summary>
    public void ReiniciarCosecha()
    {
        _cosechados.Clear();
        _enCanasta = 0;
    }

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
                IEnumerable<PointF> posiciones = Rescate
                    ? Dibujo.Arbol.PosicionesPlaga.Take(2)
                    : Debuffs.Contains(Debuff.MasBichos) ? [.. Dibujo.Arbol.PosicionesPlaga, .. BichosExtra] : Dibujo.Arbol.PosicionesPlaga;
                foreach (var p in posiciones)
                    _bichos.Add(new PointF(z.X + z.Width * p.X, z.Y + z.Height * p.Y));
                break;
            case MiniJuego.Raices:
                float[] xs = Debuffs.Contains(Debuff.RaicesTercas) ? [-0.24f, -0.12f, 0f, 0.12f, 0.24f] : [-0.18f, 0f, 0.18f];
                foreach (var dx in xs)
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
                    if (Debuffs.Contains(Debuff.MasRamas) && !Rescate)
                    {
                        _ramas.Add((new PointF(z.Left + z.Width * 0.15f, z.Top + z.Height * 0.3f), new PointF(z.Left - s * 0.08f, z.Top - s * 0.02f)));
                        _ramas.Add((new PointF(z.Right - z.Width * 0.15f, z.Bottom - z.Height * 0.2f), new PointF(z.Right + s * 0.10f, z.Bottom + s * 0.02f)));
                    }
                }
                else
                {
                    // Planta pequeña: se quitan hierbas alrededor.
                    float[] hierbas = Debuffs.Contains(Debuff.MasRamas) && !Rescate ? [-0.34f, -0.26f, 0.12f, 0.22f, 0.32f] : [-0.26f, 0.22f, 0.32f];
                    foreach (var dx in hierbas)
                        _ramas.Add((new PointF(g.CentroX + s * dx, g.Suelo), new PointF(g.CentroX + s * (dx + 0.02f), g.Suelo - s * 0.10f)));
                }
                // Una rama seca de emergencia: un solo corte.
                if (Rescate && _ramas.Count > 1) _ramas.RemoveRange(1, _ramas.Count - 1);
                break;
            case MiniJuego.Regar:
                _regadera = new PointF(_rect.Left + s * 0.16f, _rect.Bottom - s * 0.14f);
                break;
        }
    }

    private static readonly PointF[] BichosExtra = [new(0.18f, 0.55f), new(0.60f, 0.48f), new(0.46f, 0.22f)];

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
