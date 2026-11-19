namespace JardinDeHabitos.App.Dibujo;

public static class Lienzo
{
    /// <summary>
    /// Relleno sólido. En Android, asignar FillColor no quita un degradado previo
    /// de SetFillPaint, así que se reemplaza la pintura completa.
    /// </summary>
    public static void Relleno(this ICanvas c, Color color)
    {
        c.SetFillPaint(new SolidPaint(color), RectF.Zero);
        c.FillColor = color;
    }
}
