namespace JardinDeHabitos.Core;

/// <summary>Logros a nivel de usuario: persisten aunque el árbol muera.</summary>
public enum Logro
{
    FrutaLegendaria,
    LeyendaCaida,
}

public sealed record LogroInfo(Logro Logro, string Nombre, string Descripcion, bool EsSecreto)
{
    private static readonly Dictionary<Logro, LogroInfo> Catalogo = new()
    {
        [Logro.FrutaLegendaria] = new(Logro.FrutaLegendaria, "Fruta Legendaria",
            "Un árbol llegó a un año completo de racha.", EsSecreto: false),
        [Logro.LeyendaCaida] = new(Logro.LeyendaCaida, "Leyenda Caída",
            "Un árbol legendario volvió a morir. Placa dorada: la caída también es parte de la historia.", EsSecreto: true),
    };

    public static LogroInfo De(Logro logro) => Catalogo[logro];

    public static IEnumerable<LogroInfo> Todos => Catalogo.Values;
}

public sealed record LogroDesbloqueado(Logro Logro, DateOnly Fecha, string Habito);
