using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class GrowthStageCalculatorTests
{
    private sealed class RelojManual(DateTimeOffset inicio) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = inicio;
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateOnly Inicio = new(2026, 1, 1);

    [Theory]
    [InlineData(0, GrowthStage.Semilla)]
    [InlineData(1, GrowthStage.Brote)]
    [InlineData(2, GrowthStage.Brote)]
    [InlineData(3, GrowthStage.Arbusto)]
    [InlineData(4, GrowthStage.Arbusto)]
    [InlineData(5, GrowthStage.ArbolJoven)]
    [InlineData(6, GrowthStage.ArbolJoven)]
    [InlineData(7, GrowthStage.Flor)]
    [InlineData(29, GrowthStage.Flor)]
    [InlineData(30, GrowthStage.FrutaInmadura)]
    [InlineData(179, GrowthStage.FrutaInmadura)]
    [InlineData(180, GrowthStage.FrutaComestible)]
    [InlineData(364, GrowthStage.FrutaComestible)]
    [InlineData(365, GrowthStage.FrutaLegendaria)]
    [InlineData(5000, GrowthStage.FrutaLegendaria)]
    public void EtapaParaDias_RespetaUmbrales(int dias, GrowthStage esperada)
    {
        Assert.Equal(esperada, GrowthStageCalculator.EtapaParaDias(dias));
    }

    [Fact]
    public void DiasTranscurridos_UsaDiasFijosNoMesesDeCalendario()
    {
        // Feb 2026 tiene 28 días: 1 mes de calendario serían 31 días desde el 1 de enero,
        // pero el umbral es 30 días fijos.
        var hoy = Inicio.AddDays(30);
        Assert.Equal(30, GrowthStageCalculator.DiasTranscurridos(Inicio, hoy));
        Assert.Equal(GrowthStage.FrutaInmadura,
            GrowthStageCalculator.EtapaParaDias(GrowthStageCalculator.DiasTranscurridos(Inicio, hoy)));
    }

    [Fact]
    public void DiasTranscurridos_FechaFuturaCuentaComoCero()
    {
        Assert.Equal(0, GrowthStageCalculator.DiasTranscurridos(Inicio, Inicio.AddDays(-3)));
    }

    [Fact]
    public void CalcularEtapa_UsaDiasDeCrecimientoNoElCalendario()
    {
        var reloj = new RelojManual(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        var calc = new GrowthStageCalculator(reloj);
        var habito = new Habit
        {
            Categoria = MalHabito.Cigarro,
            EspecieDeArbol = "Manzano",
            Tipo = HabitType.AbstinenciaTotal,
            FechaDeInicioRachaActual = Inicio,
        };

        reloj.Ahora = reloj.Ahora.AddDays(30);
        Assert.Equal(GrowthStage.Semilla, calc.CalcularEtapa(habito));

        habito.DiasDeCrecimiento = 7;
        Assert.Equal(GrowthStage.Flor, calc.CalcularEtapa(habito));
    }

    [Fact]
    public void RelojAcelerado_UnMinutoRealEsUnDia()
    {
        var relojBase = new RelojManual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var acelerado = RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1), relojBase);
        var calc = new GrowthStageCalculator(acelerado);
        var inicio = calc.Hoy;

        relojBase.Ahora = relojBase.Ahora.AddMinutes(30);
        Assert.Equal(30, GrowthStageCalculator.DiasTranscurridos(inicio, calc.Hoy));
    }

    [Fact]
    public void RelojAcelerado_ConAnclaGuardada_SigueDondeIbaTrasReiniciar()
    {
        var ancla = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // La app se reabre 10 minutos reales después de activar el modo.
        var relojBase = new RelojManual(ancla.AddMinutes(10));

        var acelerado = RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1), relojBase, ancla);

        Assert.Equal(ancla.AddDays(10), acelerado.GetUtcNow());
        Assert.Equal(ancla, acelerado.Ancla);
    }

    [Fact]
    public void RelojAcelerado_AnclaEnElFuturo_Lanza()
    {
        var relojBase = new RelojManual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1), relojBase, relojBase.Ahora.AddHours(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void RelojAcelerado_RechazaFactorNoPositivo(double factor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelojAcelerado(factor));
    }
}
