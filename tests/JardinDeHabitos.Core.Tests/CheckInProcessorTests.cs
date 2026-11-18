using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class CheckInProcessorTests
{
    private sealed class RelojManual(DateTimeOffset inicio) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = inicio;
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateOnly D = new(2026, 1, 1);

    private readonly RelojManual _reloj = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly CheckInProcessor _proc;
    private readonly StrikeState _estado = new();

    public CheckInProcessorTests()
    {
        _proc = new CheckInProcessor(new GrowthStageCalculator(_reloj));
    }

    private static Habit NuevoHabito(DateOnly inicioRacha, HabitType tipo = HabitType.AbstinenciaTotal) => new()
    {
        Nombre = "Cigarro",
        EspecieDeArbol = "Manzano",
        Tipo = tipo,
        FechaDeInicioRachaActual = inicioRacha,
    };

    private CheckInOutcome En(int dia, Habit habito, CheckInResult resultado)
    {
        _reloj.Ahora = new DateTimeOffset(D.AddDays(dia), new TimeOnly(12, 0), TimeSpan.Zero);
        return _proc.Procesar(habito, _estado, resultado);
    }

    [Fact]
    public void Cumplido_NoCambiaNada()
    {
        var r = En(0, NuevoHabito(D), CheckInResult.Cumplido);

        Assert.False(r.ArbolMurio);
        Assert.Equal(PenaltyLevel.Ninguna, r.Penalizacion);
        Assert.Equal(0, _estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(0, _estado.StrikesTotalesDelArbolActual);
    }

    [Fact]
    public void UnRoto_Strike1_DosDiasDeCuidado()
    {
        var r = En(0, NuevoHabito(D), CheckInResult.Roto);

        Assert.Equal(PenaltyLevel.Strike1, r.Penalizacion);
        Assert.Equal(D.AddDays(2), r.FinDePenalizacion);
        Assert.Equal(1, _estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(1, _estado.StrikesTotalesDelArbolActual);
    }

    [Fact]
    public void DosRotosSeguidos_Strike2_UnaSemanaTotal()
    {
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        var r = En(1, h, CheckInResult.Roto);

        Assert.False(r.ArbolMurio);
        Assert.Equal(PenaltyLevel.Strike2, r.Penalizacion);
        Assert.Equal(D.AddDays(7), r.FinDePenalizacion);
        Assert.Equal(2, _estado.StrikesTotalesDelArbolActual);
    }

    [Fact]
    public void TresRotosSeguidos_ArbolMuereYResetea()
    {
        var h = NuevoHabito(D.AddDays(-20));
        En(0, h, CheckInResult.Roto);
        En(1, h, CheckInResult.Roto);
        var r = En(2, h, CheckInResult.Roto);

        Assert.True(r.ArbolMurio);
        Assert.Equal(PenaltyLevel.Ninguna, r.Penalizacion);
        Assert.Null(r.FinDePenalizacion);
        Assert.Equal(0, _estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(0, _estado.StrikesTotalesDelArbolActual);
        Assert.Equal(PenaltyLevel.Ninguna, _estado.NivelDePenalizacion);
        Assert.Null(_estado.FechaFinDePenalizacionActiva);
        Assert.Equal(D.AddDays(2), h.FechaDeInicioRachaActual);
    }

    [Fact]
    public void DiaLimpioEnMedio_ReiniciaRachaPeroNoCicatrices()
    {
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        En(1, h, CheckInResult.Cumplido);
        var r = En(2, h, CheckInResult.Roto);

        Assert.False(r.ArbolMurio);
        Assert.Equal(1, _estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(2, _estado.StrikesTotalesDelArbolActual);
        Assert.Equal(PenaltyLevel.Strike1, r.Penalizacion);
        Assert.Equal(D.AddDays(4), r.FinDePenalizacion);
    }

    [Fact]
    public void Cumplido_NoAcortaPenalizacionEnCurso()
    {
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        var r = En(1, h, CheckInResult.Cumplido);

        Assert.Equal(0, _estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(PenaltyLevel.Strike1, r.Penalizacion);
        Assert.Equal(D.AddDays(2), r.FinDePenalizacion);
    }

    [Fact]
    public void Cumplido_NoBajaStrike2PorUnRotoNuevo()
    {
        // Roto, Roto (Strike2 hasta D+7), Cumplido, Roto → sigue Strike2, fin no se acorta.
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        En(1, h, CheckInResult.Roto);
        En(2, h, CheckInResult.Cumplido);
        var r = En(3, h, CheckInResult.Roto);

        Assert.Equal(PenaltyLevel.Strike2, r.Penalizacion);
        Assert.Equal(D.AddDays(7), r.FinDePenalizacion);
    }

    [Fact]
    public void PenalizacionExpira()
    {
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        var r = En(2, h, CheckInResult.Cumplido);

        Assert.Equal(PenaltyLevel.Ninguna, r.Penalizacion);
        Assert.Null(r.FinDePenalizacion);
        Assert.Equal(1, _estado.StrikesTotalesDelArbolActual);
    }

    [Fact]
    public void RotoTrasExpirar_NuevaPenalizacionDesdeHoy()
    {
        var h = NuevoHabito(D);
        En(0, h, CheckInResult.Roto);
        En(1, h, CheckInResult.Cumplido);
        En(5, h, CheckInResult.Cumplido);
        var r = En(6, h, CheckInResult.Roto);

        Assert.Equal(PenaltyLevel.Strike1, r.Penalizacion);
        Assert.Equal(D.AddDays(8), r.FinDePenalizacion);
    }

    [Fact]
    public void StrikeNoReiniciaCrecimiento()
    {
        var h = NuevoHabito(D.AddDays(-10));
        En(0, h, CheckInResult.Roto);
        En(1, h, CheckInResult.Roto);

        Assert.Equal(D.AddDays(-10), h.FechaDeInicioRachaActual);
    }
}
