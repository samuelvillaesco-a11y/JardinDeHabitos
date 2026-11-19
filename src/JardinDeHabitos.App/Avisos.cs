namespace JardinDeHabitos.App;

/// <summary>Anuncios dentro de la app. Nunca dicen qué logro ni de qué hábito: eso solo se ve en el estante.</summary>
public static class Avisos
{
    public const string TituloLogro = "🏆 ¡Desbloqueaste un logro!";
    public const string CuerpoLogro = "Abre tu jardín para verlo.";

    public static async Task AnunciarLogros(Page pagina)
    {
        int n = Servicios.Jardin.TomarLogrosNuevos();
        if (n == 0) return;
        string cuerpo = n == 1
            ? "Tienes algo nuevo en tu estante de trofeos."
            : $"Tienes {n} cosas nuevas en tu estante de trofeos.";
        await pagina.DisplayAlertAsync(TituloLogro, cuerpo, "¡Bien!");
    }
}
