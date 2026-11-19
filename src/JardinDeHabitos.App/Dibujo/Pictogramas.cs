using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Dibujo;

/// <summary>
/// El dibujo del centro de cada emblema: algo que se entienda a simple vista
/// (regadera para regar, tijeras para podar, copa tachada para dejar el alcohol...).
/// Todo se dibuja dentro de un círculo de radio <c>r</c> centrado en <c>o</c>.
/// </summary>
public static class Pictogramas
{
    public enum Pic
    {
        Semilla, Brote, FrutaBrillante, Libreta, Sol, Regadera, Tijeras, Flor, Arboles, Corazon, Venda, Manos, Replantar,
        Calendario, FinDeSemana, Hamaca, Pala,
        Bicho, Raiz, Caracol, Luna, Amanecer, Gotas, RegaderaRacha,
        FrutaRota, TierraAgrietada, Telarana, Llama,
        Fenix, Corona, Bosque, Regreso, FlorDeLlama, TresFrutas, CopoDeNieve,
        Copa, Dado, Caramelo, Taza, Cigarro, Bolsa, Pan, Ojo, Telefono, Salero, Pastilla, Hoja,
    }

    /// <summary>Qué dibujo lleva un logro, y si va tachado (metas de tiempo de un hábito).</summary>
    public static (Pic Pic, bool Tachado) De(LogroDef d)
    {
        if (d.Habito is { } h)
        {
            string sufijo = d.Id[(d.Id.LastIndexOf('.') + 1)..];
            if (sufijo.StartsWith("tiempo")) return (DelHabito(h), true);
            return (sufijo switch
            {
                "constancia" => Pic.Calendario,
                "bitacora" => Pic.Libreta,
                "replantar" => Pic.Replantar,
                "cura" => Pic.Venda,
                "apoyo" => Pic.Manos,
                "borron" => Pic.Pala,
                "findesemana" => Pic.FinDeSemana,
                "libres" => Pic.Hamaca,
                _ => Pic.Hoja,
            }, false);
        }

        return (d.Id switch
        {
            "gen.semilla" => Pic.Semilla,
            "gen.brote" => Pic.Brote,
            "gen.legendaria" => Pic.FrutaBrillante,
            "gen.bitacora" => Pic.Libreta,
            "gen.constancia" => Pic.Sol,
            "gen.riego" => Pic.Regadera,
            "gen.poda" => Pic.Tijeras,
            "gen.primavera" => Pic.Flor,
            "gen.huerto" => Pic.Arboles,
            "gen.honesto" => Pic.Corazon,
            "gen.cura" => Pic.Venda,
            "gen.apoyo" => Pic.Manos,
            "gen.replantar" => Pic.Replantar,
            "oc.ramas" => Pic.Tijeras,
            "oc.bichos" => Pic.Bicho,
            "oc.raices" => Pic.Raiz,
            "oc.rescate" => Pic.Caracol,
            "oc.nocturno" => Pic.Luna,
            "oc.madrugada" => Pic.Amanecer,
            "oc.completo" => Pic.Gotas,
            "oc.mimo" => Pic.Corazon,
            "oc.rachariego" => Pic.RegaderaRacha,
            "mal.leyenda_caida" => Pic.FrutaRota,
            "mal.tierra_baldia" => Pic.TierraAgrietada,
            "mal.jardin_olvidado" => Pic.Telarana,
            "mal.ceniza" => Pic.Llama,
            "leg.fenix" => Pic.Fenix,
            "leg.corona" => Pic.Corona,
            "leg.bosque" => Pic.Bosque,
            "leg.regreso" => Pic.Regreso,
            "leg.fenix_ceniza" => Pic.FlorDeLlama,
            "leg.huerto_eterno" => Pic.TresFrutas,
            "leg.cinco_inviernos" => Pic.CopoDeNieve,
            _ => Pic.Hoja,
        }, false);
    }

    public static Pic DelHabito(MalHabito h) => h switch
    {
        MalHabito.Alcohol => Pic.Copa,
        MalHabito.Apuestas => Pic.Dado,
        MalHabito.Azucares => Pic.Caramelo,
        MalHabito.Cafeina => Pic.Taza,
        MalHabito.Cigarro => Pic.Cigarro,
        MalHabito.Compras => Pic.Bolsa,
        MalHabito.Harinas => Pic.Pan,
        MalHabito.Pornografia => Pic.Ojo,
        MalHabito.Pantallas => Pic.Telefono,
        MalHabito.Sal => Pic.Salero,
        MalHabito.Sustancias => Pic.Pastilla,
        _ => Pic.Hoja,
    };

    /// <param name="fondo">Color del disco, para recortar la raya del tachado.</param>
    public static void Dibujar(ICanvas c, Pic pic, bool tachado, PointF o, float r, Color color, Color fondo)
    {
        c.Relleno(color);
        c.StrokeColor = color;
        c.StrokeLineCap = LineCap.Round;
        c.StrokeLineJoin = LineJoin.Round;
        c.StrokeSize = Math.Max(1.5f, r * 0.11f);
        float x = o.X, y = o.Y;

        switch (pic)
        {
            case Pic.Semilla:
                c.FillEllipse(x - r * 0.28f, y - r * 0.05f, r * 0.56f, r * 0.42f);
                c.DrawLine(x - r * 0.6f, y + r * 0.45f, x + r * 0.6f, y + r * 0.45f);
                c.DrawLine(x, y - r * 0.05f, x + r * 0.12f, y - r * 0.35f);
                break;
            case Pic.Brote:
                c.DrawLine(x, y + r * 0.55f, x, y - r * 0.1f);
                Hoja(c, x - r * 0.25f, y - r * 0.2f, r * 0.32f, -35);
                Hoja(c, x + r * 0.25f, y - r * 0.35f, r * 0.32f, 35);
                c.DrawLine(x - r * 0.55f, y + r * 0.55f, x + r * 0.55f, y + r * 0.55f);
                break;
            case Pic.FrutaBrillante:
                // Manzana con hoja y destellos.
                c.FillCircle(x - r * 0.12f, y + r * 0.12f, r * 0.26f);
                c.FillCircle(x + r * 0.12f, y + r * 0.12f, r * 0.26f);
                c.FillCircle(x, y + r * 0.28f, r * 0.24f);
                c.DrawLine(x, y - r * 0.1f, x + r * 0.05f, y - r * 0.32f);
                Hoja(c, x + r * 0.2f, y - r * 0.3f, r * 0.14f, 60);
                c.FillPath(Estrella(new PointF(x - r * 0.42f, y - r * 0.38f), r * 0.16f, r * 0.06f));
                c.FillPath(Estrella(new PointF(x + r * 0.45f, y - r * 0.05f), r * 0.11f, r * 0.045f));
                break;
            case Pic.Libreta:
                c.DrawRoundedRectangle(x - r * 0.4f, y - r * 0.5f, r * 0.8f, r * 1.0f, r * 0.08f);
                for (int i = 0; i < 3; i++) c.DrawLine(x - r * 0.22f, y - r * 0.2f + i * r * 0.25f, x + r * 0.22f, y - r * 0.2f + i * r * 0.25f);
                break;
            case Pic.Sol:
                c.FillCircle(x, y, r * 0.28f);
                Rayos(c, x, y, r * 0.42f, r * 0.62f, 8);
                break;
            case Pic.Regadera:
                Regadera(c, x, y, r);
                break;
            case Pic.RegaderaRacha:
                Regadera(c, x - r * 0.12f, y, r * 0.85f);
                c.DrawLine(x + r * 0.42f, y - r * 0.35f, x + r * 0.58f, y - r * 0.2f);
                c.DrawLine(x + r * 0.58f, y - r * 0.2f, x + r * 0.42f, y - r * 0.05f);
                c.DrawLine(x + r * 0.42f, y + r * 0.05f, x + r * 0.58f, y + r * 0.2f);
                c.DrawLine(x + r * 0.58f, y + r * 0.2f, x + r * 0.42f, y + r * 0.35f);
                break;
            case Pic.Tijeras:
                c.DrawCircle(x - r * 0.28f, y + r * 0.35f, r * 0.17f);
                c.DrawCircle(x + r * 0.28f, y + r * 0.35f, r * 0.17f);
                c.DrawLine(x - r * 0.18f, y + r * 0.22f, x + r * 0.25f, y - r * 0.5f);
                c.DrawLine(x + r * 0.18f, y + r * 0.22f, x - r * 0.25f, y - r * 0.5f);
                break;
            case Pic.Flor:
                for (int i = 0; i < 5; i++)
                {
                    double a = i * Math.PI * 2 / 5 - Math.PI / 2;
                    c.FillCircle(x + r * 0.28f * (float)Math.Cos(a), y - r * 0.1f + r * 0.28f * (float)Math.Sin(a), r * 0.2f);
                }
                c.Relleno(fondo);
                c.FillCircle(x, y - r * 0.1f, r * 0.12f);
                c.DrawLine(x, y + r * 0.2f, x, y + r * 0.6f);
                break;
            case Pic.Arboles:
                Arbolito(c, x - r * 0.32f, y + r * 0.05f, r * 0.55f);
                Arbolito(c, x + r * 0.32f, y + r * 0.05f, r * 0.55f);
                Arbolito(c, x, y - r * 0.1f, r * 0.7f);
                break;
            case Pic.Bosque:
                Arbolito(c, x - r * 0.38f, y + r * 0.1f, r * 0.55f);
                Arbolito(c, x + r * 0.38f, y + r * 0.1f, r * 0.55f);
                Arbolito(c, x, y, r * 0.75f);
                c.FillPath(Estrella(new PointF(x, y - r * 0.62f), r * 0.16f, r * 0.07f));
                break;
            case Pic.Corazon:
                Corazon(c, x, y, r * 0.55f);
                break;
            case Pic.Venda:
                c.DrawRoundedRectangle(x - r * 0.55f, y - r * 0.2f, r * 1.1f, r * 0.4f, r * 0.2f);
                c.FillRectangle(x - r * 0.08f, y - r * 0.35f, r * 0.16f, r * 0.7f);
                c.FillRectangle(x - r * 0.25f, y - r * 0.08f, r * 0.5f, r * 0.16f);
                break;
            case Pic.Manos:
                // Dos personas tomadas de la mano.
                c.FillCircle(x - r * 0.3f, y - r * 0.35f, r * 0.14f);
                c.FillCircle(x + r * 0.3f, y - r * 0.35f, r * 0.14f);
                c.DrawLine(x - r * 0.3f, y - r * 0.15f, x - r * 0.3f, y + r * 0.25f);
                c.DrawLine(x + r * 0.3f, y - r * 0.15f, x + r * 0.3f, y + r * 0.25f);
                c.DrawLine(x - r * 0.3f, y, x + r * 0.3f, y);
                c.DrawLine(x - r * 0.3f, y + r * 0.25f, x - r * 0.42f, y + r * 0.55f);
                c.DrawLine(x - r * 0.3f, y + r * 0.25f, x - r * 0.18f, y + r * 0.55f);
                c.DrawLine(x + r * 0.3f, y + r * 0.25f, x + r * 0.18f, y + r * 0.55f);
                c.DrawLine(x + r * 0.3f, y + r * 0.25f, x + r * 0.42f, y + r * 0.55f);
                break;
            case Pic.Replantar:
                c.DrawArc(x - r * 0.5f, y - r * 0.5f, r, r, 30, 300, false, false);
                c.FillPath(Triangulo(new PointF(x + r * 0.43f, y - r * 0.33f), r * 0.17f, 200));
                c.DrawLine(x, y + r * 0.25f, x, y - r * 0.05f);
                Hoja(c, x + r * 0.13f, y - r * 0.12f, r * 0.2f, 35);
                break;
            case Pic.Calendario:
                Calendario(c, x, y, r);
                c.DrawLine(x - r * 0.18f, y + r * 0.12f, x - r * 0.04f, y + r * 0.26f);
                c.DrawLine(x - r * 0.04f, y + r * 0.26f, x + r * 0.2f, y - r * 0.02f);
                break;
            case Pic.FinDeSemana:
                Calendario(c, x, y, r);
                c.FillRectangle(x + r * 0.02f, y - r * 0.02f, r * 0.14f, r * 0.14f);
                c.FillRectangle(x + r * 0.2f, y - r * 0.02f, r * 0.14f, r * 0.14f);
                c.FillRectangle(x + r * 0.02f, y + r * 0.18f, r * 0.14f, r * 0.14f);
                c.FillRectangle(x + r * 0.2f, y + r * 0.18f, r * 0.14f, r * 0.14f);
                break;
            case Pic.Hamaca:
                // Sombrilla de descanso.
                var sombrilla = new PathF();
                sombrilla.MoveTo(x - r * 0.55f, y - r * 0.05f);
                sombrilla.QuadTo(x, y - r * 0.75f, x + r * 0.55f, y - r * 0.05f);
                sombrilla.Close();
                c.FillPath(sombrilla);
                c.DrawLine(x, y - r * 0.1f, x, y + r * 0.5f);
                c.DrawLine(x - r * 0.5f, y + r * 0.55f, x + r * 0.5f, y + r * 0.55f);
                break;
            case Pic.Pala:
                c.DrawLine(x - r * 0.4f, y - r * 0.5f, x + r * 0.1f, y + r * 0.05f);
                c.DrawLine(x - r * 0.52f, y - r * 0.38f, x - r * 0.28f, y - r * 0.62f);
                var pala = new PathF();
                pala.MoveTo(x + r * 0.02f, y + r * 0.0f);
                pala.LineTo(x + r * 0.2f, y - r * 0.15f);
                pala.LineTo(x + r * 0.55f, y + r * 0.25f);
                pala.LineTo(x + r * 0.35f, y + r * 0.5f);
                pala.Close();
                c.FillPath(pala);
                break;
            case Pic.Bicho:
                Dibujo.Arbol.DibujarBicho(c, o, r * 0.42f);
                break;
            case Pic.Raiz:
                c.DrawLine(x - r * 0.55f, y - r * 0.3f, x + r * 0.55f, y - r * 0.3f);
                c.DrawLine(x, y - r * 0.3f, x, y + r * 0.2f);
                c.DrawLine(x, y + r * 0.05f, x - r * 0.3f, y + r * 0.45f);
                c.DrawLine(x, y + r * 0.05f, x + r * 0.3f, y + r * 0.45f);
                c.DrawLine(x, y + r * 0.2f, x + r * 0.05f, y + r * 0.6f);
                break;
            case Pic.Caracol:
                c.DrawArc(x - r * 0.35f, y - r * 0.45f, r * 0.7f, r * 0.7f, 0, 360, false, true);
                c.DrawArc(x - r * 0.18f, y - r * 0.28f, r * 0.36f, r * 0.36f, 0, 360, false, true);
                c.DrawLine(x - r * 0.55f, y + r * 0.3f, x + r * 0.55f, y + r * 0.3f);
                c.DrawLine(x + r * 0.4f, y + r * 0.3f, x + r * 0.55f, y - r * 0.05f);
                break;
            case Pic.Luna:
                c.FillCircle(x, y, r * 0.42f);
                c.Relleno(fondo);
                c.FillCircle(x + r * 0.2f, y - r * 0.12f, r * 0.36f);
                c.Relleno(color);
                c.FillPath(Estrella(new PointF(x + r * 0.35f, y + r * 0.3f), r * 0.12f, r * 0.05f));
                break;
            case Pic.Amanecer:
                c.FillArc(x - r * 0.32f, y - r * 0.15f, r * 0.64f, r * 0.64f, 0, 180, true);
                c.DrawLine(x - r * 0.6f, y + r * 0.2f, x + r * 0.6f, y + r * 0.2f);
                Rayos(c, x, y + r * 0.17f, r * 0.45f, r * 0.62f, 5, arriba: true);
                break;
            case Pic.Gotas:
                Gota(c, x - r * 0.32f, y + r * 0.1f, r * 0.3f);
                Gota(c, x + r * 0.32f, y + r * 0.1f, r * 0.3f);
                Gota(c, x, y - r * 0.2f, r * 0.34f);
                break;
            case Pic.FrutaRota:
                c.FillCircle(x - r * 0.06f, y + r * 0.1f, r * 0.36f);
                c.StrokeColor = fondo;
                c.DrawLine(x - r * 0.06f, y - r * 0.26f, x + r * 0.06f, y + r * 0.05f);
                c.DrawLine(x + r * 0.06f, y + r * 0.05f, x - r * 0.1f, y + r * 0.45f);
                c.StrokeColor = color;
                c.DrawLine(x + r * 0.35f, y - r * 0.55f, x + r * 0.35f, y - r * 0.3f);
                c.DrawLine(x + r * 0.5f, y - r * 0.5f, x + r * 0.5f, y - r * 0.3f);
                break;
            case Pic.TierraAgrietada:
                c.FillRoundedRectangle(x - r * 0.6f, y - r * 0.05f, r * 1.2f, r * 0.45f, r * 0.08f);
                c.StrokeColor = fondo;
                c.DrawLine(x - r * 0.3f, y - r * 0.05f, x - r * 0.15f, y + r * 0.2f);
                c.DrawLine(x - r * 0.15f, y + r * 0.2f, x - r * 0.3f, y + r * 0.4f);
                c.DrawLine(x + r * 0.2f, y - r * 0.05f, x + r * 0.32f, y + r * 0.4f);
                c.StrokeColor = color;
                c.DrawLine(x - r * 0.05f, y - r * 0.15f, x - r * 0.05f, y - r * 0.45f);
                c.DrawLine(x - r * 0.05f, y - r * 0.35f, x + r * 0.15f, y - r * 0.5f);
                break;
            case Pic.Telarana:
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3;
                    c.DrawLine(x, y, x + r * 0.6f * (float)Math.Cos(a), y + r * 0.6f * (float)Math.Sin(a));
                }
                c.StrokeSize = Math.Max(1f, r * 0.06f);
                c.DrawCircle(x, y, r * 0.22f);
                c.DrawCircle(x, y, r * 0.42f);
                break;
            case Pic.Llama:
                Llama(c, x, y, r);
                break;
            case Pic.Fenix:
                Llama(c, x, y + r * 0.15f, r * 0.7f);
                var alaI = new PathF();
                alaI.MoveTo(x - r * 0.1f, y);
                alaI.QuadTo(x - r * 0.45f, y - r * 0.55f, x - r * 0.65f, y - r * 0.2f);
                alaI.QuadTo(x - r * 0.35f, y - r * 0.15f, x - r * 0.1f, y + r * 0.1f);
                c.FillPath(alaI);
                var alaD = new PathF();
                alaD.MoveTo(x + r * 0.1f, y);
                alaD.QuadTo(x + r * 0.45f, y - r * 0.55f, x + r * 0.65f, y - r * 0.2f);
                alaD.QuadTo(x + r * 0.35f, y - r * 0.15f, x + r * 0.1f, y + r * 0.1f);
                c.FillPath(alaD);
                break;
            case Pic.Corona:
                var corona = new PathF();
                corona.MoveTo(x - r * 0.5f, y + r * 0.3f);
                corona.LineTo(x - r * 0.55f, y - r * 0.3f);
                corona.LineTo(x - r * 0.25f, y - r * 0.02f);
                corona.LineTo(x, y - r * 0.45f);
                corona.LineTo(x + r * 0.25f, y - r * 0.02f);
                corona.LineTo(x + r * 0.55f, y - r * 0.3f);
                corona.LineTo(x + r * 0.5f, y + r * 0.3f);
                corona.Close();
                c.FillPath(corona);
                c.FillRectangle(x - r * 0.5f, y + r * 0.38f, r, r * 0.12f);
                break;
            case Pic.Regreso:
                c.DrawArc(x - r * 0.45f, y - r * 0.4f, r * 0.9f, r * 0.8f, 0, 180, false, false);
                c.FillPath(Triangulo(new PointF(x - r * 0.45f, y + r * 0.05f), r * 0.17f, 90));
                Hoja(c, x + r * 0.05f, y + r * 0.1f, r * 0.25f, 0);
                break;
            case Pic.FlorDeLlama:
                Llama(c, x, y + r * 0.2f, r * 0.55f);
                for (int i = 0; i < 5; i++)
                {
                    double a = i * Math.PI * 2 / 5 - Math.PI / 2;
                    c.FillCircle(x + r * 0.18f * (float)Math.Cos(a), y - r * 0.38f + r * 0.18f * (float)Math.Sin(a), r * 0.12f);
                }
                break;
            case Pic.TresFrutas:
                c.FillCircle(x - r * 0.3f, y + r * 0.2f, r * 0.24f);
                c.FillCircle(x + r * 0.3f, y + r * 0.2f, r * 0.24f);
                c.FillCircle(x, y - r * 0.22f, r * 0.24f);
                Rayos(c, x, y, r * 0.58f, r * 0.7f, 8);
                break;
            case Pic.CopoDeNieve:
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3;
                    float cx = (float)Math.Cos(a), sy = (float)Math.Sin(a);
                    c.DrawLine(x, y, x + r * 0.6f * cx, y + r * 0.6f * sy);
                    float bx = x + r * 0.38f * cx, by = y + r * 0.38f * sy;
                    double a1 = a + 0.6, a2 = a - 0.6;
                    c.DrawLine(bx, by, bx + r * 0.16f * (float)Math.Cos(a1), by + r * 0.16f * (float)Math.Sin(a1));
                    c.DrawLine(bx, by, bx + r * 0.16f * (float)Math.Cos(a2), by + r * 0.16f * (float)Math.Sin(a2));
                }
                break;

            // Los hábitos (tachados en las metas de tiempo).
            case Pic.Copa:
                var copa = new PathF();
                copa.MoveTo(x - r * 0.35f, y - r * 0.5f);
                copa.LineTo(x + r * 0.35f, y - r * 0.5f);
                copa.QuadTo(x + r * 0.32f, y, x, y + r * 0.05f);
                copa.QuadTo(x - r * 0.32f, y, x - r * 0.35f, y - r * 0.5f);
                c.FillPath(copa);
                c.DrawLine(x, y + r * 0.05f, x, y + r * 0.45f);
                c.DrawLine(x - r * 0.25f, y + r * 0.5f, x + r * 0.25f, y + r * 0.5f);
                break;
            case Pic.Dado:
                c.DrawRoundedRectangle(x - r * 0.45f, y - r * 0.45f, r * 0.9f, r * 0.9f, r * 0.15f);
                foreach (var (dx, dy) in new[] { (-0.2f, -0.2f), (0.2f, 0.2f), (0f, 0f), (0.2f, -0.2f), (-0.2f, 0.2f) })
                    c.FillCircle(x + r * dx, y + r * dy, r * 0.08f);
                break;
            case Pic.Caramelo:
                c.FillCircle(x, y, r * 0.28f);
                c.FillPath(Triangulo(new PointF(x - r * 0.42f, y), r * 0.2f, 0));
                c.FillPath(Triangulo(new PointF(x + r * 0.42f, y), r * 0.2f, 180));
                break;
            case Pic.Taza:
                c.FillRoundedRectangle(x - r * 0.4f, y - r * 0.1f, r * 0.6f, r * 0.55f, r * 0.1f);
                c.DrawArc(x + r * 0.1f, y, r * 0.3f, r * 0.3f, 90, 270, true, false);
                c.StrokeSize = Math.Max(1f, r * 0.07f);
                c.DrawLine(x - r * 0.2f, y - r * 0.25f, x - r * 0.15f, y - r * 0.5f);
                c.DrawLine(x, y - r * 0.25f, x + r * 0.05f, y - r * 0.5f);
                break;
            case Pic.Cigarro:
                c.FillRectangle(x - r * 0.6f, y - r * 0.08f, r * 0.95f, r * 0.2f);
                c.DrawRectangle(x + r * 0.35f, y - r * 0.08f, r * 0.2f, r * 0.2f);
                c.StrokeSize = Math.Max(1f, r * 0.07f);
                c.DrawLine(x + r * 0.45f, y - r * 0.2f, x + r * 0.35f, y - r * 0.35f);
                c.DrawLine(x + r * 0.35f, y - r * 0.35f, x + r * 0.45f, y - r * 0.5f);
                break;
            case Pic.Bolsa:
                c.FillRoundedRectangle(x - r * 0.4f, y - r * 0.2f, r * 0.8f, r * 0.7f, r * 0.08f);
                c.DrawArc(x - r * 0.2f, y - r * 0.5f, r * 0.4f, r * 0.5f, 0, 180, false, false);
                break;
            case Pic.Pan:
                var pan = new PathF();
                pan.MoveTo(x - r * 0.55f, y + r * 0.3f);
                pan.LineTo(x - r * 0.55f, y - r * 0.05f);
                pan.QuadTo(x, y - r * 0.6f, x + r * 0.55f, y - r * 0.05f);
                pan.LineTo(x + r * 0.55f, y + r * 0.3f);
                pan.Close();
                c.FillPath(pan);
                c.StrokeColor = fondo;
                c.StrokeSize = Math.Max(1f, r * 0.07f);
                for (int i = -1; i <= 1; i++) c.DrawLine(x + i * r * 0.22f - r * 0.06f, y - r * 0.12f, x + i * r * 0.22f + r * 0.06f, y - r * 0.32f);
                break;
            case Pic.Ojo:
                var ojo = new PathF();
                ojo.MoveTo(x - r * 0.6f, y);
                ojo.QuadTo(x, y - r * 0.55f, x + r * 0.6f, y);
                ojo.QuadTo(x, y + r * 0.55f, x - r * 0.6f, y);
                c.DrawPath(ojo);
                c.FillCircle(x, y, r * 0.18f);
                break;
            case Pic.Telefono:
                c.DrawRoundedRectangle(x - r * 0.28f, y - r * 0.5f, r * 0.56f, r * 1.0f, r * 0.1f);
                c.FillCircle(x, y + r * 0.35f, r * 0.06f);
                break;
            case Pic.Salero:
                c.FillRoundedRectangle(x - r * 0.25f, y - r * 0.15f, r * 0.5f, r * 0.65f, r * 0.08f);
                var tapa = new PathF();
                tapa.MoveTo(x - r * 0.25f, y - r * 0.15f);
                tapa.QuadTo(x, y - r * 0.6f, x + r * 0.25f, y - r * 0.15f);
                tapa.Close();
                c.FillPath(tapa);
                c.Relleno(fondo);
                foreach (float dx in new[] { -0.08f, 0.08f }) c.FillCircle(x + r * dx, y - r * 0.3f, r * 0.04f);
                break;
            case Pic.Pastilla:
                c.SaveState();
                c.Rotate(-40, x, y);
                c.DrawRoundedRectangle(x - r * 0.55f, y - r * 0.2f, r * 1.1f, r * 0.4f, r * 0.2f);
                c.FillRoundedRectangle(x - r * 0.55f, y - r * 0.2f, r * 0.55f, r * 0.4f, r * 0.2f);
                c.RestoreState();
                break;
            default:
                Hoja(c, x, y, r * 0.55f, 0);
                break;
        }

        if (tachado)
        {
            // Raya en diagonal, recortada con el color del disco para que se lea encima del dibujo.
            c.StrokeColor = fondo;
            c.StrokeSize = Math.Max(3f, r * 0.24f);
            c.DrawLine(x - r * 0.55f, y + r * 0.55f, x + r * 0.55f, y - r * 0.55f);
            c.StrokeColor = color;
            c.StrokeSize = Math.Max(1.5f, r * 0.11f);
            c.DrawLine(x - r * 0.55f, y + r * 0.55f, x + r * 0.55f, y - r * 0.55f);
        }
    }

    private static void Hoja(ICanvas c, float x, float y, float tam, float grados)
    {
        c.SaveState();
        c.Rotate(grados, x, y);
        var hoja = new PathF();
        hoja.MoveTo(x, y - tam);
        hoja.QuadTo(x + tam * 0.9f, y, x, y + tam);
        hoja.QuadTo(x - tam * 0.9f, y, x, y - tam);
        c.FillPath(hoja);
        c.RestoreState();
    }

    private static void Arbolito(ICanvas c, float x, float y, float tam)
    {
        c.FillCircle(x, y - tam * 0.25f, tam * 0.35f);
        c.FillRectangle(x - tam * 0.06f, y, tam * 0.12f, tam * 0.45f);
    }

    private static void Regadera(ICanvas c, float x, float y, float r)
    {
        // Cuerpo, asa al lado, pico largo hacia arriba y gotas saliendo.
        var cuerpo = new PathF();
        cuerpo.MoveTo(x - r * 0.42f, y - r * 0.15f);
        cuerpo.LineTo(x + r * 0.12f, y - r * 0.15f);
        cuerpo.LineTo(x + r * 0.08f, y + r * 0.42f);
        cuerpo.LineTo(x - r * 0.38f, y + r * 0.42f);
        cuerpo.Close();
        c.FillPath(cuerpo);
        c.DrawArc(x - r * 0.62f, y - r * 0.08f, r * 0.36f, r * 0.36f, 90, 270, false, false);
        c.DrawLine(x + r * 0.08f, y + r * 0.15f, x + r * 0.45f, y - r * 0.3f);
        c.FillCircle(x + r * 0.48f, y - r * 0.33f, r * 0.09f);
        c.FillCircle(x + r * 0.62f, y - r * 0.12f, r * 0.05f);
        c.FillCircle(x + r * 0.55f, y + r * 0.05f, r * 0.05f);
    }

    private static void Calendario(ICanvas c, float x, float y, float r)
    {
        c.DrawRoundedRectangle(x - r * 0.48f, y - r * 0.4f, r * 0.96f, r * 0.9f, r * 0.08f);
        c.DrawLine(x - r * 0.48f, y - r * 0.15f, x + r * 0.48f, y - r * 0.15f);
        c.DrawLine(x - r * 0.25f, y - r * 0.55f, x - r * 0.25f, y - r * 0.3f);
        c.DrawLine(x + r * 0.25f, y - r * 0.55f, x + r * 0.25f, y - r * 0.3f);
    }

    private static void Corazon(ICanvas c, float x, float y, float tam)
    {
        var p = new PathF();
        p.MoveTo(x, y + tam * 0.8f);
        p.CurveTo(x - tam * 1.2f, y - tam * 0.1f, x - tam * 0.6f, y - tam * 1.0f, x, y - tam * 0.35f);
        p.CurveTo(x + tam * 0.6f, y - tam * 1.0f, x + tam * 1.2f, y - tam * 0.1f, x, y + tam * 0.8f);
        c.FillPath(p);
    }

    private static void Gota(ICanvas c, float x, float y, float tam)
    {
        var g = new PathF();
        g.MoveTo(x, y - tam);
        g.CurveTo(x + tam, y + tam * 0.1f, x + tam * 0.6f, y + tam * 0.9f, x, y + tam * 0.9f);
        g.CurveTo(x - tam * 0.6f, y + tam * 0.9f, x - tam, y + tam * 0.1f, x, y - tam);
        c.FillPath(g);
    }

    private static void Llama(ICanvas c, float x, float y, float r)
    {
        var p = new PathF();
        p.MoveTo(x, y - r * 0.6f);
        p.CurveTo(x + r * 0.5f, y - r * 0.1f, x + r * 0.45f, y + r * 0.5f, x, y + r * 0.5f);
        p.CurveTo(x - r * 0.45f, y + r * 0.5f, x - r * 0.5f, y, x - r * 0.15f, y - r * 0.25f);
        p.QuadTo(x - r * 0.1f, y, x, y - r * 0.6f);
        c.FillPath(p);
    }

    private static void Rayos(ICanvas c, float x, float y, float desde, float hasta, int n, bool arriba = false)
    {
        for (int i = 0; i < n; i++)
        {
            double a = arriba ? Math.PI + (i + 0.5) * Math.PI / n : i * Math.PI * 2 / n;
            float cx = (float)Math.Cos(a), sy = (float)Math.Sin(a);
            c.DrawLine(x + desde * cx, y + desde * sy, x + hasta * cx, y + hasta * sy);
        }
    }

    private static PathF Triangulo(PointF punta, float tam, float grados)
    {
        double a = grados * Math.PI / 180;
        var p = new PathF();
        p.MoveTo(punta);
        p.LineTo(punta.X + tam * (float)Math.Cos(a - 0.5), punta.Y + tam * (float)Math.Sin(a - 0.5));
        p.LineTo(punta.X + tam * (float)Math.Cos(a + 0.5), punta.Y + tam * (float)Math.Sin(a + 0.5));
        p.Close();
        return p;
    }

    internal static PathF Estrella(PointF o, float rExt, float rInt)
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
