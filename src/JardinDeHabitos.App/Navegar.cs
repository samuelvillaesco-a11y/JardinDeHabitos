namespace JardinDeHabitos.App;

/// <summary>Navegación a prueba de doble toque, para toda la app.</summary>
public static class Navegar
{
    private static bool _enCurso;

    /// <summary>Abre una página una sola vez aunque el toque llegue doble, y solo si <paramref name="desde"/> está arriba.</summary>
    public static async Task UnaVez(this Page desde, Func<Page> crear)
    {
        if (_enCurso || desde.Navigation.NavigationStack.LastOrDefault() != desde) return;
        _enCurso = true;
        try { await desde.Navigation.PushAsync(crear()); }
        finally { _enCurso = false; }
    }

    /// <summary>Verifica al dueño y, si pasa, abre la página (la verificación concurrente ya se comparte en Seguridad).</summary>
    public static async Task Verificado(this Page desde, string titulo, Func<Page> crear, Core.Seguro? seccion = null)
    {
        if (_enCurso) return;
        bool ok = seccion is { } s
            ? await Seguridad.EntrarAsync(desde, s, titulo)
            : await Seguridad.VerificarAsync(desde, titulo);
        if (ok) await desde.UnaVez(crear);
    }
}
