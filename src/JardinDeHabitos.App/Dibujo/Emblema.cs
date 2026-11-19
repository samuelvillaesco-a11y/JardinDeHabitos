using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

/// <summary>Colores de un emblema: claro, base y oscuro (o un arcoíris).</summary>
public sealed record PaletaEmblema(Color Claro, Color Base, Color Oscuro, Color Cinta, bool Arcoiris = false)
{
    public static readonly PaletaEmblema SinGanar = new(Color.FromArgb("#4A5640"), Color.FromArgb("#3A4532"), Color.FromArgb("#2C3526"), Color.FromArgb("#3A4532"));
    public static readonly PaletaEmblema Bronce = new(Color.FromArgb("#F0B27A"), Color.FromArgb("#CD7F32"), Color.FromArgb("#7A4A1C"), Color.FromArgb("#8E3B2E"));
    public static readonly PaletaEmblema Plata = new(Color.FromArgb("#F4F6F8"), Color.FromArgb("#B8C0C8"), Color.FromArgb("#6E7780"), Color.FromArgb("#3E5C76"));
    public static readonly PaletaEmblema Oro = new(Color.FromArgb("#FFEDB0"), Color.FromArgb("#E9C46A"), Color.FromArgb("#9C7A2A"), Color.FromArgb("#4F7A32"));
    public static readonly PaletaEmblema Maldito = new(Color.FromArgb("#4A4048"), Color.FromArgb("#1A161A"), Color.FromArgb("#000000"), Color.FromArgb("#4B1F4B"));
    public static readonly PaletaEmblema Fenix = new(Color.FromArgb("#FFB36B"), Color.FromArgb("#E2462E"), Color.FromArgb("#6E1410"), Color.FromArgb("#B8860B"));
    public static readonly PaletaEmblema Mitico = new(Color.FromArgb("#E6A0F0"), Color.FromArgb("#9B30B8"), Color.FromArgb("#4A1060"), Color.FromArgb("#B0283C"));
    public static readonly PaletaEmblema Iris = new(Colors.White, Color.FromArgb("#9B30B8"), Color.FromArgb("#3A1650"), Color.FromArgb("#2E5E9E"), Arcoiris: true);

    /// <summary>Legendarios: el Fénix en fuego, las redenciones en morado y rojo, las hazañas más grandes en arcoíris.</summary>
    public static PaletaEmblema De(EntradaEstante e) => e.Rango switch
    {
        Rango.Bronce => Bronce,
        Rango.Plata => Plata,
        Rango.Oro => Oro,
        Rango.Maldito => Maldito,
        Rango.Legendario when e.Def.Id == "leg.fenix" => Fenix,
        Rango.Legendario when e.Def.Redime is not null => Mitico,
        Rango.Legendario => Iris,
        _ => SinGanar,
    };
}

/// <summary>
/// Un emblema del estante: medalla con cinta, un dibujo propio de cada logro y las estrellas abajo.
/// El nombre va debajo, en la página; el detalle completo solo se ve al tocarlo.
/// </summary>
public sealed class Emblema(EntradaEstante entrada, bool conEstrellas = true) : IDrawable
{
    private static readonly Color[] ColoresArcoiris =
    [
        Color.FromArgb("#E2462E"), Color.FromArgb("#F08A24"), Color.FromArgb("#F2D13D"),
        Color.FromArgb("#5BA84A"), Color.FromArgb("#3E7CC0"), Color.FromArgb("#8E44AD"),
    ];

    public void Draw(ICanvas c, RectF r)
    {
        var p = PaletaEmblema.De(entrada);
        bool ganado = entrada.Nivel > 0;
        float alto = conEstrellas ? r.Height * 0.80f : r.Height;
        float radio = Math.Min(r.Width, alto) * 0.40f;
        var centro = new PointF(r.Center.X, r.Top + radio + Math.Min(r.Width, alto) * 0.06f);

        // Cinta (detrás de la medalla).
        c.Relleno(ganado ? p.Cinta : p.Cinta.WithAlpha(0.5f));
        foreach (int lado in new[] { -1, 1 })
        {
            var cinta = new PathF();
            cinta.MoveTo(centro.X + lado * radio * 0.15f, centro.Y + radio * 0.5f);
            cinta.LineTo(centro.X + lado * radio * 0.70f, centro.Y + radio * 0.5f);
            cinta.LineTo(centro.X + lado * radio * 0.55f, centro.Y + radio * 1.45f);
            cinta.LineTo(centro.X + lado * radio * 0.40f, centro.Y + radio * 1.25f);
            cinta.LineTo(centro.X + lado * radio * 0.25f, centro.Y + radio * 1.45f);
            cinta.Close();
            c.FillPath(cinta);
        }

        // Aro exterior con brillo de metal.
        var aro = new RectF(centro.X - radio, centro.Y - radio, radio * 2, radio * 2);
        if (p.Arcoiris)
        {
            var stops = ColoresArcoiris.Select((col, i) => new PaintGradientStop(i / (float)(ColoresArcoiris.Length - 1), col)).ToArray();
            c.SetFillPaint(new LinearGradientPaint(stops) { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) }, aro);
        }
        else
        {
            c.SetFillPaint(new RadialGradientPaint([new PaintGradientStop(0, p.Claro), new PaintGradientStop(0.6f, p.Base), new PaintGradientStop(1, p.Oscuro)])
            { Center = new Point(0.35, 0.3), Radius = 0.9 }, aro);
        }
        c.FillEllipse(aro);

        // Disco interior.
        float ri = radio * 0.74f;
        var disco = new RectF(centro.X - ri, centro.Y - ri, ri * 2, ri * 2);
        c.SetFillPaint(new RadialGradientPaint([new PaintGradientStop(0, p.Base), new PaintGradientStop(1, p.Oscuro)])
        { Center = new Point(0.5, 0.4), Radius = 0.8 }, disco);
        c.FillEllipse(disco);

        if (!ganado)
        {
            c.StrokeColor = Tema.HuesoTenue.WithAlpha(0.35f);
            c.StrokeSize = 2;
            c.StrokeDashPattern = [4, 4];
            c.DrawEllipse(aro);
            c.StrokeDashPattern = null;
        }

        // Dibujo que dice de qué es el logro (también en los que faltan, más tenue, para saber qué buscar).
        var (pic, tachado) = Pictogramas.De(entrada.Def);
        Pictogramas.Dibujar(c, pic, tachado, centro, ri, ganado ? p.Claro : Tema.HuesoTenue.WithAlpha(0.55f), ganado ? p.Base : p.Oscuro);

        if (conEstrellas && entrada.Def.Niveles.Length > 0 && entrada.Rango is not (Rango.Maldito or Rango.Legendario))
            Estrellas(c, r, ganado ? p.Base : Tema.HuesoTenue);

        if (entrada.Nuevo)
        {
            float d = radio * 0.30f;
            var punto = new PointF(centro.X + radio * 0.78f, centro.Y - radio * 0.78f);
            c.Relleno(Colors.White);
            c.FillCircle(punto, d + 2);
            c.Relleno(Color.FromArgb("#E0352B"));
            c.FillCircle(punto, d);
        }
    }

    private void Estrellas(ICanvas c, RectF r, Color llena)
    {
        int total = entrada.Def.Niveles.Length;
        float tam = Math.Min(r.Width / 7f, r.Height * 0.09f);
        float y = r.Bottom - tam * 1.1f;
        float ancho = total * tam * 2.2f;
        for (int i = 0; i < total; i++)
        {
            var o = new PointF(r.Center.X - ancho / 2 + tam * 1.1f + i * tam * 2.2f, y);
            var estrella = Estrella(o, tam, tam * 0.45f);
            if (i < entrada.Nivel)
            {
                c.Relleno(llena);
                c.FillPath(estrella);
            }
            else
            {
                c.StrokeColor = Tema.HuesoTenue.WithAlpha(0.5f);
                c.StrokeSize = 1.5f;
                c.DrawPath(estrella);
            }
        }
    }

    private static PathF Estrella(PointF o, float rExt, float rInt)
    {
        var p = new PathF();
        for (int i = 0; i < 10; i++)
        {
            float rad = i % 2 == 0 ? rExt : rInt;
            double a = -Math.PI / 2 + i * Math.PI / 5;
            var punto = new PointF(o.X + rad * (float)Math.Cos(a), o.Y + rad * (float)Math.Sin(a));
            if (i == 0) p.MoveTo(punto); else p.LineTo(punto);
        }
        p.Close();
        return p;
    }
}
