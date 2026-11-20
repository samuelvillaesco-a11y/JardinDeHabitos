using System.Net.Mail;

namespace JardinDeHabitos.Core;

/// <summary>Cuánta actividad física hace el usuario (categorías de la OMS, aproximadas).</summary>
public enum ActividadFisica
{
    SinDecir,
    Ninguna,
    Ligera,     // caminar, menos de 150 min/semana de actividad moderada
    Moderada,   // 150–300 min/semana (recomendación OMS)
    Intensa,    // más de 300 min/semana o actividad vigorosa frecuente
}

/// <summary>
/// Datos opcionales del usuario. Todo es opcional y se guarda cifrado solo en el teléfono.
/// </summary>
public sealed record Perfil(
    string? Nombre = null,
    string? Correo = null,
    string? Telefono = null,
    double? PesoKg = null,
    double? AlturaCm = null,
    ActividadFisica Actividad = ActividadFisica.SinDecir)
{
    public const int LargoMaximoTexto = 80;

    /// <summary>Por qué el perfil no se puede guardar, o null si está bien.</summary>
    public string? Problema()
    {
        if (Nombre?.Length > LargoMaximoTexto) return "El nombre es demasiado largo.";
        if (!string.IsNullOrWhiteSpace(Correo) && !EsCorreo(Correo)) return "Ese correo no parece válido.";
        if (!string.IsNullOrWhiteSpace(Telefono) && Jardinero.Limpiar(Telefono).Length is < 7 or > 15)
            return "El teléfono debe tener entre 7 y 15 dígitos.";
        if (PesoKg is { } p && p is < 20 or > 350) return "El peso debe estar entre 20 y 350 kg.";
        if (AlturaCm is { } a && a is < 80 or > 250) return "La altura debe estar entre 80 y 250 cm.";
        return null;
    }

    private static bool EsCorreo(string c)
    {
        try { return new MailAddress(c.Trim()).Address == c.Trim() && c.Contains('.'); }
        catch (FormatException) { return false; }
    }
}

/// <summary>
/// Un "jardinero": alguien de confianza a quien llamar o escribir en un momento difícil.
/// </summary>
public sealed record Jardinero(Guid Id, string Nombre, string Telefono)
{
    public const int Maximo = 5;

    public static Jardinero Nuevo(string nombre, string telefono)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0) throw new ArgumentException("Tu jardinero necesita un nombre.");
        if (nombre.Length > Perfil.LargoMaximoTexto) throw new ArgumentException("El nombre es demasiado largo.");
        var limpio = Limpiar(telefono);
        if (limpio.Length is < 3 or > 15) throw new ArgumentException("Ese número no parece válido.");
        return new Jardinero(Guid.NewGuid(), nombre, (telefono ?? "").Trim().StartsWith('+') ? "+" + limpio : limpio);
    }

    /// <summary>Solo los dígitos de un número (quita espacios, guiones y paréntesis).</summary>
    public static string Limpiar(string? telefono) => new([.. (telefono ?? "").Where(char.IsAsciiDigit)]);
}
