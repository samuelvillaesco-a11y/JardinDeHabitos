namespace JardinDeHabitos.Core;

/// <summary>Especies de árbol. Cada árbol vivo del jardín usa una distinta.</summary>
public static class EspeciesDeArbol
{
    public static IReadOnlyList<string> Todas { get; } =
    [
        "Manzano", "Naranjo", "Limonero", "Cerezo", "Durazno",
        "Higuera", "Aguacate", "Mango", "Granado", "Olivo",
    ];
}
