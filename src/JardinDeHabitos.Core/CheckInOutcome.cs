namespace JardinDeHabitos.Core;

/// <summary>Qué pasó al procesar un check-in.</summary>
public sealed record CheckInOutcome(
    bool ArbolMurio,
    PenaltyLevel Penalizacion,
    DateOnly? FinDePenalizacion,
    bool LeyendaCaida);
