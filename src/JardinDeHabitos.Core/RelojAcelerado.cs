namespace JardinDeHabitos.Core;

/// <summary>
/// Reloj para pruebas manuales: a partir del momento en que se crea, el tiempo
/// avanza <c>factor</c> veces más rápido que el reloj base.
/// Ejemplo: <c>RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1))</c> hace que
/// cada minuto real cuente como un día del jardín.
/// </summary>
public sealed class RelojAcelerado : TimeProvider
{
    private readonly TimeProvider _base;
    private readonly DateTimeOffset _ancla;

    public double Factor { get; }

    public RelojAcelerado(double factor, TimeProvider? relojBase = null)
    {
        if (!double.IsFinite(factor) || factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "El factor debe ser un número positivo.");

        Factor = factor;
        _base = relojBase ?? System;
        _ancla = _base.GetUtcNow();
    }

    /// <summary>Crea un reloj donde cada <paramref name="intervaloReal"/> equivale a un día.</summary>
    public static RelojAcelerado UnDiaCada(TimeSpan intervaloReal, TimeProvider? relojBase = null)
    {
        if (intervaloReal <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(intervaloReal), intervaloReal, "El intervalo debe ser positivo.");

        return new RelojAcelerado(TimeSpan.FromDays(1) / intervaloReal, relojBase);
    }

    public override DateTimeOffset GetUtcNow()
    {
        var transcurridoReal = _base.GetUtcNow() - _ancla;
        return _ancla + transcurridoReal * Factor;
    }

    public override TimeZoneInfo LocalTimeZone => _base.LocalTimeZone;
}
