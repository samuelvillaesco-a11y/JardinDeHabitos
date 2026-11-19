using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class CheatDayScheduleTests
{
    // 2026-01-01 es jueves.
    private static readonly DateOnly Jueves = new(2026, 1, 1);
    private static readonly DateOnly Sabado = new(2026, 1, 3);
    private static readonly DateOnly Domingo = new(2026, 1, 4);
    private static readonly DateOnly Lunes = new(2026, 1, 5);

    private sealed class RelojManual : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; }
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static Habit Moderable(params DayOfWeek[] dias) => new()
    {
        Categoria = MalHabito.Azucares,
        EspecieDeArbol = "Naranjo",
        Tipo = HabitType.Moderable,
        FechaDeInicioRachaActual = Jueves,
        DiasDeCheat = [.. dias],
    };

    private readonly RelojManual _reloj = new();
    private readonly StrikeState _estado = new();
    private readonly CheckInProcessor _proc;

    public CheatDayScheduleTests() => _proc = new CheckInProcessor(new GrowthStageCalculator(_reloj));

    private CheckInOutcome En(DateOnly dia, Habit h, CheckInResult r)
    {
        _reloj.Ahora = new DateTimeOffset(dia, new TimeOnly(12, 0), TimeSpan.Zero);
        return _proc.Procesar(h, _estado, r);
    }

    [Fact]
    public void EsDiaDeCheat_SoloEnDiasDelCalendario()
    {
        var h = Moderable(DayOfWeek.Sunday);
        Assert.True(CheatDaySchedule.EsDiaDeCheat(h, Domingo));
        Assert.False(CheatDaySchedule.EsDiaDeCheat(h, Lunes));
    }

    [Fact]
    public void EsDiaDeCheat_AbstinenciaTotalNunca()
    {
        var h = Moderable(DayOfWeek.Sunday);
        h.Tipo = HabitType.AbstinenciaTotal;
        Assert.False(CheatDaySchedule.EsDiaDeCheat(h, Domingo));
    }

    [Theory]
    [InlineData(new DayOfWeek[0])]
    [InlineData(new[] { DayOfWeek.Sunday })]
    [InlineData(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })]
    public void Validar_ModerableDeCeroADosDias_Ok(DayOfWeek[] dias)
    {
        CheatDaySchedule.Validar(Moderable(dias));
    }

    [Fact]
    public void Validar_TresDias_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            CheatDaySchedule.Validar(Moderable(DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday)));
    }

    [Fact]
    public void Validar_DiasRepetidos_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            CheatDaySchedule.Validar(Moderable(DayOfWeek.Sunday, DayOfWeek.Sunday)));
    }

    [Fact]
    public void Validar_AbstinenciaTotalConDias_Lanza()
    {
        var h = Moderable(DayOfWeek.Sunday);
        h.Tipo = HabitType.AbstinenciaTotal;
        Assert.Throws<ArgumentException>(() => CheatDaySchedule.Validar(h));
    }

    [Fact]
    public void PuedeEditar_SoloSabado()
    {
        Assert.True(CheatDaySchedule.PuedeEditar(Sabado));
        Assert.False(CheatDaySchedule.PuedeEditar(Domingo));
    }

    [Fact]
    public void CheatDay_FueraDelCalendario_Lanza()
    {
        var h = Moderable(DayOfWeek.Sunday);
        Assert.Throws<InvalidOperationException>(() => En(Jueves, h, CheckInResult.CheatDay));
    }

    [Fact]
    public void CumplidoORoto_EnDiaDeCheat_Lanza()
    {
        var h = Moderable(DayOfWeek.Sunday);
        Assert.Throws<InvalidOperationException>(() => En(Domingo, h, CheckInResult.Roto));
    }
}
