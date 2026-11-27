namespace JardinDeHabitos.App.Dibujo;

public static class Lienzo
{
    /// <summary>
    /// Relleno sólido. En Android, asignar FillColor (o una SolidPaint) no quita un degradado
    /// previo, ni siquiera el que vuelve con RestoreState: se pinta con un degradado de un solo
    /// color, que sí reemplaza al anterior.
    /// </summary>
    public static void Relleno(this ICanvas c, Color color)
    {
        c.SetFillPaint(new LinearGradientPaint([new PaintGradientStop(0, color), new PaintGradientStop(1, color)]), new RectF(0, 0, 1, 1));
        c.FillColor = color;
    }
}
