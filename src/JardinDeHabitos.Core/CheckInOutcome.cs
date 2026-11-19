namespace JardinDeHabitos.Core;

/// <summary>Qué pasó al procesar un check-in.</summary>
/// <param name="DiasAntesDeMorir">Días de crecimiento que tenía el árbol justo antes de morir.</param>
public sealed record CheckInOutcome(bool ArbolMurio, int DiasAntesDeMorir, bool EmpezoCuidado);
