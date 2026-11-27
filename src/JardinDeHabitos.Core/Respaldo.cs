using SQLite;

namespace JardinDeHabitos.Core;

/// <summary>El archivo no abre con esa contraseña (o no es una copia del jardín).</summary>
public sealed class ContrasenaIncorrectaException() : Exception("La contraseña no abre esta copia, o el archivo no es una copia del jardín.");

/// <summary>
/// Copia del jardín en un archivo cifrado con una contraseña que elige el usuario, para pasarlo a
/// otro teléfono. Es una base SQLCipher completa: lo que se exporta es exactamente lo guardado.
/// La contraseña nunca se guarda: sin ella, el archivo no se puede abrir.
/// </summary>
public static class Respaldo
{
    /// <summary>El archivo puede salir del teléfono y atacarse sin límite de intentos: pide largo.</summary>
    public const int MinimoContrasena = 10;

    public static string? ProblemaContrasena(string contrasena) =>
        string.IsNullOrWhiteSpace(contrasena) || contrasena.Trim().Length < MinimoContrasena
            ? $"Usa al menos {MinimoContrasena} caracteres (una frase corta es fácil de recordar y difícil de adivinar)."
            : null;

    public static void Exportar(SqliteGardenStore store, string destino, string contrasena)
    {
        if (ProblemaContrasena(contrasena) is { } problema) throw new ArgumentException(problema, nameof(contrasena));
        string temporal = destino + ".escribiendo";
        File.Delete(temporal);
        try
        {
            store.ExportarA(temporal, contrasena);
            File.Move(temporal, destino, overwrite: true);
        }
        finally { File.Delete(temporal); }
    }

    /// <summary>
    /// Abre la copia con la contraseña y la deja lista, cifrada con la llave de este teléfono, junto a
    /// <paramref name="rutaBase"/>. No toca la base actual. Devuelve la ruta preparada.
    /// </summary>
    public static string Preparar(string archivo, string contrasena, string rutaBase, string llave)
    {
        string preparada = rutaBase + ".importada";
        File.Delete(preparada);
        try
        {
            using var copia = new SQLiteConnection(new SQLiteConnectionString(archivo, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true, key: contrasena));
            int version;
            try { version = copia.ExecuteScalar<int>("PRAGMA user_version"); copia.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master"); }
            catch (SQLiteException e) when (e.Result is SQLite3.Result.NonDBFile or SQLite3.Result.Corrupt) { throw new ContrasenaIncorrectaException(); }
            if (version < 1) throw new ContrasenaIncorrectaException();
            if (version > SqliteGardenStore.VersionActual)
                throw new InvalidOperationException("Esta copia viene de una versión más nueva de la app. Actualiza la app primero.");

            copia.Execute("ATTACH DATABASE ? AS nueva KEY ?", preparada, SqliteGardenStore.ClaveDirecta(llave));
            copia.ExecuteScalar<string>("SELECT sqlcipher_export('nueva')");
            copia.Execute($"PRAGMA nueva.user_version = {version}");
            copia.Execute("DETACH DATABASE nueva");
        }
        catch
        {
            File.Delete(preparada);
            throw;
        }
        // Comprueba que abre y la lleva al esquema actual antes de cambiar nada.
        using (new SqliteGardenStore(preparada, llave: llave)) { }
        return preparada;
    }

    /// <summary>
    /// Pone la copia preparada en lugar de la base (que debe estar cerrada). La base anterior se
    /// aparta, nunca se borra; devuelve adónde (null si no había).
    /// </summary>
    public static string? Instalar(string preparada, string rutaBase)
    {
        string? anterior = null;
        if (File.Exists(rutaBase))
        {
            anterior = $"{rutaBase}.antes-de-importar-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(rutaBase, anterior);
            foreach (var extra in new[] { "-wal", "-shm", "-journal" })
                if (File.Exists(rutaBase + extra)) File.Move(rutaBase + extra, anterior + extra);
        }
        File.Move(preparada, rutaBase);
        return anterior;
    }
}
