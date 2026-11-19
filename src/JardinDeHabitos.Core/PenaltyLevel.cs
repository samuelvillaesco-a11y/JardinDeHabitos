namespace JardinDeHabitos.Core;

/// <summary>Lo más grave que tiene el árbol hoy (se calcula, no se guarda).</summary>
public enum PenaltyLevel
{
    Ninguna,
    Strike1, // plaga
    Strike2, // raíces podridas (con o sin plaga)
}
