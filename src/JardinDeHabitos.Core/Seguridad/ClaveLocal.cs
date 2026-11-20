using System.Security.Cryptography;
using System.Text;

namespace JardinDeHabitos.Core;

/// <summary>
/// Una clave guardada solo como hash: PBKDF2-HMAC-SHA256 con sal aleatoria de 16 bytes.
/// Nunca se guarda el texto. Se compara en tiempo constante.
/// </summary>
public sealed record HashDeClave(byte[] Sal, byte[] Hash, int Iteraciones)
{
    public string Serializar() => $"pbkdf2-sha256${Iteraciones}${Convert.ToBase64String(Sal)}${Convert.ToBase64String(Hash)}";

    public static HashDeClave? Leer(string? texto)
    {
        var p = texto?.Split('$');
        if (p is not { Length: 4 } || p[0] != "pbkdf2-sha256" || !int.TryParse(p[1], out int it) || it < 1) return null;
        try { return new HashDeClave(Convert.FromBase64String(p[2]), Convert.FromBase64String(p[3]), it); }
        catch (FormatException) { return null; }
    }
}

public static class ClaveLocal
{
    /// <summary>
    /// Iteraciones de PBKDF2. OWASP 2023 recomienda 600 000 para SHA-256; en un teléfono de gama
    /// media eso tarda ~1 s, aceptable porque solo corre al desbloquear.
    /// </summary>
    public const int Iteraciones = 600_000;

    public const int LargoMinimo = 6;
    public const int LargoMaximo = 64;
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    public const int PinMinimo = 4;
    public const int PinMaximo = 8;
    public const int PatronMinimo = 4;

    /// <summary>Por qué un secreto no sirve para ese tipo de candado, o null si sirve.</summary>
    public static string? Problema(TipoCandado tipo, string? secreto) => tipo switch
    {
        TipoCandado.Pin => ProblemaPin(secreto),
        TipoCandado.Patron => ProblemaPatron(secreto),
        TipoCandado.Clave => Problema(secreto),
        _ => "El bloqueo del teléfono no usa un secreto propio.",
    };

    private static string? ProblemaPin(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || !pin.All(char.IsAsciiDigit))
            return "El PIN solo lleva números.";
        if (pin.Length is < PinMinimo or > PinMaximo)
            return $"El PIN lleva de {PinMinimo} a {PinMaximo} números.";
        if (pin.Distinct().Count() == 1) return "El PIN no puede ser un solo número repetido, como 0000.";
        if (EsSecuencia(pin)) return "El PIN no puede ser una secuencia como 1234.";
        return null;
    }

    /// <summary>Un patrón se guarda como los puntos tocados en orden (0 a 8, de izquierda a derecha y de arriba abajo).</summary>
    private static string? ProblemaPatron(string? patron)
    {
        if (string.IsNullOrEmpty(patron) || !patron.All(c => c is >= '0' and <= '8') || patron.Distinct().Count() != patron.Length)
            return "Ese patrón no es válido. Vuelve a dibujarlo.";
        if (patron.Length < PatronMinimo) return $"Une al menos {PatronMinimo} puntos.";
        return null;
    }

    /// <summary>Por qué una clave de texto no sirve, o null si sirve.</summary>
    public static string? Problema(string? clave)
    {
        if (string.IsNullOrEmpty(clave) || clave.Length < LargoMinimo)
            return $"La clave necesita al menos {LargoMinimo} caracteres.";
        if (clave.Length > LargoMaximo) return $"La clave puede tener máximo {LargoMaximo} caracteres.";
        if (clave.Distinct().Count() == 1) return "La clave no puede ser un solo carácter repetido.";
        if (EsSecuencia(clave)) return "La clave no puede ser una secuencia como 123456.";
        return null;
    }

    public static HashDeClave Crear(string clave, int iteraciones = Iteraciones)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        return new HashDeClave(sal, Derivar(clave, sal, iteraciones), iteraciones);
    }

    public static bool Verificar(string? clave, HashDeClave guardado)
    {
        if (clave is null) return false;
        var calculado = Derivar(clave, guardado.Sal, guardado.Iteraciones);
        return CryptographicOperations.FixedTimeEquals(calculado, guardado.Hash);
    }

    private static byte[] Derivar(string clave, byte[] sal, int iteraciones) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(clave.Normalize(NormalizationForm.FormC)), sal, iteraciones,
            HashAlgorithmName.SHA256, BytesHash);

    private static bool EsSecuencia(string c)
    {
        if (c.Length < 3) return false;
        int paso = c[1] - c[0];
        if (paso is not (1 or -1)) return false;
        for (int i = 2; i < c.Length; i++) if (c[i] - c[i - 1] != paso) return false;
        return true;
    }
}

/// <summary>
/// Código de recuperación: 16 caracteres de un alfabeto sin letras confusas (sin I, L, O, U ni 0/1),
/// ~80 bits de azar criptográfico. Se muestra una sola vez; se guarda solo su hash.
/// </summary>
public static class CodigoDeRecuperacion
{
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int Largo = 16;

    public static string Generar()
    {
        var chars = new char[Largo];
        for (int i = 0; i < Largo; i++) chars[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        var s = new string(chars);
        return $"{s[..4]}-{s[4..8]}-{s[8..12]}-{s[12..]}";
    }

    /// <summary>Quita guiones y espacios, y pasa a mayúsculas (así lo escriba como lo escriba).</summary>
    public static string Normalizar(string? codigo) =>
        new([.. (codigo ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit)]);
}

/// <summary>
/// Límite de intentos fallidos: 5 libres; luego espera creciente (30 s, 1 min, 2 min… hasta 1 h).
/// </summary>
public static class Intentos
{
    public const int Libres = 5;

    public static TimeSpan Espera(int fallidos)
    {
        if (fallidos < Libres) return TimeSpan.Zero;
        double segundos = 30 * Math.Pow(2, fallidos - Libres);
        return TimeSpan.FromSeconds(Math.Min(segundos, 3600));
    }

    /// <summary>Cuánto falta para poder volver a intentar (cero si ya se puede).</summary>
    public static TimeSpan Restante(int fallidos, DateTimeOffset ultimoFallo, DateTimeOffset ahora)
    {
        var resta = ultimoFallo + Espera(fallidos) - ahora;
        // Un reloj movido hacia atrás no debe dar una espera mayor a la máxima.
        return resta <= TimeSpan.Zero ? TimeSpan.Zero : resta > TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : resta;
    }
}
