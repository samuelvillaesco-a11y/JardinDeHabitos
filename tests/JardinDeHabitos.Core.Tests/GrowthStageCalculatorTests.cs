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
    public void CalcularEtapa_UsaElRelojInyectado()
    {
        var reloj = new RelojManual(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        var calc = new GrowthStageCalculator(reloj);
        var habito = new Habit
        {
            Nombre = "Cigarro",
            EspecieDeArbol = "Manzano",
            Tipo = HabitType.AbstinenciaTotal,
            FechaDeInicioRachaActual = Inicio,
        };

        Assert.Equal(GrowthStage.Semilla, calc.CalcularEtapa(habito));

        reloj.Ahora = reloj.Ahora.AddDays(7);
        Assert.Equal(GrowthStage.Flor, calc.CalcularEtapa(habito));
    }

    [Fact]
    public void RelojAcelerado_UnMinutoRealEsUnDia()
    {
        var relojBase = new RelojManual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var acelerado = RelojAcelerado.UnDiaCada(TimeSpan.FromMinutes(1), relojBase);
        var calc = new GrowthStageCalculator(acelerado);
        var habito = new Habit
        {
            Nombre = "Azúcar",
            EspecieDeArbol = "Naranjo",
            Tipo = HabitType.Moderable,
            FechaDeInicioRachaActual = calc.Hoy,
        };

        relojBase.Ahora = relojBase.Ahora.AddMinutes(30);
        Assert.Equal(30, calc.DiasTranscurridos(habito));
        Assert.Equal(GrowthStage.FrutaInmadura, calc.CalcularEtapa(habito));

        relojBase.Ahora = relojBase.Ahora.AddMinutes(335); // 365 min en total
        Assert.Equal(GrowthStage.FrutaLegendaria, calc.CalcularEtapa(habito));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void RelojAcelerado_RechazaFactorNoPositivo(double factor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelojAcelerado(factor));
    }
}
