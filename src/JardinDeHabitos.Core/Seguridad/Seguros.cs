namespace JardinDeHabitos.Core;

/// <summary>Las partes de la app que se pueden poner bajo llave.</summary>
public enum Seguro
{
    /// <summary>El estante de logros.</summary>
    Logros,

    /// <summary>El blog botánico (diario y registro de emociones).</summary>
    Blog,

    /// <summary>Toda la app: se pide verificación al abrirla y al volver de segundo plano.</summary>
    General,

    /// <summary>Doble verificación: clave de la app + bloqueo del teléfono, juntos.</summary>
    DobleVerificacion,
}

public static class Seguros
{
    /// <summary>Logros y blog nacen con llave; la app completa y la doble verificación no.</summary>
    public static bool PorDefecto(Seguro seguro) => seguro is Seguro.Logros or Seguro.Blog;
}
