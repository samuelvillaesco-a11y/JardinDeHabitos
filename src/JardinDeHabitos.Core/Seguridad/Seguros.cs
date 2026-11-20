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

/// <summary>Con qué se abre lo que está bajo llave. Lo elige el usuario y lo puede cambiar.</summary>
public enum TipoCandado
{
    /// <summary>El mismo bloqueo del teléfono (huella, PIN o patrón del sistema). Por defecto.</summary>
    Telefono,

    /// <summary>Un PIN propio de la app (4 a 8 dígitos).</summary>
    Pin,

    /// <summary>Un patrón propio de la app (3×3, mínimo 4 puntos).</summary>
    Patron,

    /// <summary>Una clave de texto (versiones anteriores). Se sigue aceptando, ya no se ofrece.</summary>
    Clave,
}
