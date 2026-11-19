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

    public double Factor { get; }

    /// <summary>Momento real desde el que el tiempo corre acelerado. Guardarlo permite seguir tras reiniciar la app.</summary>
    public DateTimeOffset Ancla { get; }

    public RelojAcelerado(double factor, TimeProvider? relojBase = null, DateTimeOffset? ancla = null)
    {
        if (!double.IsFinite(factor) || factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "El factor debe ser un número positivo.");

        Factor = factor;
        _base = relojBase ?? System;
        var ahora = _base.GetUtcNow();
        if (ancla > ahora)
            throw new ArgumentOutOfRangeException(nameof(ancla), ancla, "El ancla no puede estar en el futuro.");
        Ancla = ancla ?? ahora;
    }

    /// <summary>Crea un reloj donde cada <paramref name="intervaloReal"/> equivale a un día.</summary>
    public static RelojAcelerado UnDiaCada(TimeSpan intervaloReal, TimeProvider? relojBase = null, DateTimeOffset? ancla = null)
    {
        if (intervaloReal <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(intervaloReal), intervaloReal, "El intervalo debe ser positivo.");

        return new RelojAcelerado(TimeSpan.FromDays(1) / intervaloReal, relojBase, ancla);
    }

    public override DateTimeOffset GetUtcNow()
    {
        var transcurridoReal = _base.GetUtcNow() - Ancla;
        return Ancla + transcurridoReal * Factor;
    }

    public override TimeZoneInfo LocalTimeZone => _base.LocalTimeZone;
}
