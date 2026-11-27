using Android.App;
using Android.Content;

namespace JardinDeHabitos.App;

/// <summary>
/// Guardar y abrir archivos con el selector del sistema (Storage Access Framework): el usuario elige
/// carpeta o archivo y la app solo recibe permiso para ese. No pide permisos de almacenamiento.
/// </summary>
public static class Archivos
{
    private const int CodigoGuardar = 4712;
    private const int CodigoAbrir = 4713;
    /// <summary>Una copia del jardín pesa pocos MB; esto evita llenar el teléfono con un archivo equivocado.</summary>
    private const long MaximoBytes = 200L * 1024 * 1024;

    private static TaskCompletionSource<Android.Net.Uri?>? _espera;
    private static int _codigo;

    public static Task<Android.Net.Uri?> ElegirDondeGuardarAsync(string nombre)
    {
        var i = new Intent(Intent.ActionCreateDocument);
        i.AddCategory(Intent.CategoryOpenable);
        i.SetType("application/octet-stream");
        i.PutExtra(Intent.ExtraTitle, nombre);
        return Lanzar(i, CodigoGuardar);
    }

    public static Task<Android.Net.Uri?> ElegirArchivoAsync()
    {
        var i = new Intent(Intent.ActionOpenDocument);
        i.AddCategory(Intent.CategoryOpenable);
        i.SetType("*/*");
        return Lanzar(i, CodigoAbrir);
    }

    private static Task<Android.Net.Uri?> Lanzar(Intent i, int codigo)
    {
        var actividad = Platform.CurrentActivity;
        if (actividad is null) return Task.FromResult<Android.Net.Uri?>(null);
        _espera?.TrySetResult(null);
        _espera = new();
        _codigo = codigo;
        App.SalidaAlSelector();
        try { actividad.StartActivityForResult(i, codigo); }
        catch (ActivityNotFoundException) { _espera.TrySetResult(null); }
        return _espera.Task;
    }

    /// <summary>Lo llama MainActivity al volver del selector.</summary>
    public static void AlVolver(int codigo, Result resultado, Intent? datos)
    {
        if (_espera is null || codigo != _codigo) return;
        var tcs = _espera;
        _espera = null;
        tcs.TrySetResult(resultado == Result.Ok ? datos?.Data : null);
    }

    public static async Task CopiarAAsync(string local, Android.Net.Uri destino)
    {
        var resolver = Android.App.Application.Context.ContentResolver!;
        await using var salida = resolver.OpenOutputStream(destino, "wt") ?? throw new IOException("No se pudo escribir ahí.");
        await using var entrada = File.OpenRead(local);
        await entrada.CopyToAsync(salida);
    }

    public static async Task CopiarDeAsync(Android.Net.Uri origen, string local)
    {
        var resolver = Android.App.Application.Context.ContentResolver!;
        await using var entrada = resolver.OpenInputStream(origen) ?? throw new IOException("No se pudo leer el archivo.");
        await using var salida = File.Create(local);
        var buffer = new byte[81920];
        long total = 0;
        int n;
        while ((n = await entrada.ReadAsync(buffer)) > 0)
        {
            total += n;
            if (total > MaximoBytes) throw new IOException("Ese archivo es demasiado grande para ser una copia del jardín.");
            await salida.WriteAsync(buffer.AsMemory(0, n));
        }
    }
}
