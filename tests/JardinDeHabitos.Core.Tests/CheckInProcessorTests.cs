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

    private static readonly DateOnly D = new(2026, 1, 1); // jueves

    private readonly RelojManual _reloj = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly CheckInProcessor _proc;
    private readonly StrikeState _estado = new();
    private readonly Habit _habito = Nuevo();

    public CheckInProcessorTests() => _proc = new CheckInProcessor(new GrowthStageCalculator(_reloj));

    private static Habit Nuevo(HabitType tipo = HabitType.AbstinenciaTotal, params DayOfWeek[] cheat) => new()
    {
        Categoria = MalHabito.Cigarro,
        EspecieDeArbol = "Manzano",
        Tipo = tipo,
        FechaDeInicioRachaActual = D,
        DiasDeCheat = [.. cheat],
    };

    private CheckInOutcome En(int dia, CheckInResult r, Habit? h = null)
    {
        _reloj.Ahora = new DateTimeOffset(D.AddDays(dia), new TimeOnly(12, 0), TimeSpan.Zero);
        return _proc.Procesar(h ?? _habito, _estado, r);
    }

    [Fact]
    public void Cumplido_SanoSumaUnDia()
    {
        En(0, CheckInResult.Cumplido);
        En(1, CheckInResult.Cumplido);

        Assert.Equal(2, _habito.DiasDeCrecimiento);
    }

    [Fact]
    public void Cumplido_EnCuidadoNoSuma()
    {
        En(0, CheckInResult.Roto);
        En(1, CheckInResult.Cumplido);
        En(2, CheckInResult.Cumplido);
        En(3, CheckInResult.Cumplido); // la plaga terminó (fin exclusivo día 3)

        Assert.Equal(1, _habito.DiasDeCrecimiento);
    }

    [Fact]
    public void Strike1_Plaga3Dias()
    {
        var r = En(0, CheckInResult.Roto);

        Assert.True(r.EmpezoCuidado);
        Assert.Equal(D.AddDays(3), _estado.FinPlaga);
        Assert.Null(_estado.FinRaices);
        Assert.True(CheckInProcessor.TienePlaga(_estado, D));
        Assert.False(CheckInProcessor.TieneRaices(_estado, D));
    }

    [Fact]
    public void Strike2_ConPlagaActiva_SoloRaices_TotalOchoDias()
    {
        En(0, CheckInResult.Roto);
        En(1, CheckInResult.Roto);

        Assert.Equal(D.AddDays(3), _estado.FinPlaga);
        Assert.Equal(D.AddDays(8), _estado.FinRaices);
        Assert.Equal(D.AddDays(8), CheckInProcessor.FinCuidado(_estado, D.AddDays(1)));
    }

    [Fact]
    public void Strike2_SinPlagaActiva_PlagaYRaices()
    {
        En(0, CheckInResult.Roto);
        En(20, CheckInResult.Roto);

        Assert.Equal(D.AddDays(23), _estado.FinPlaga);
        Assert.Equal(D.AddDays(28), _estado.FinRaices);
        Assert.True(CheckInProcessor.TienePlaga(_estado, D.AddDays(22)));
        Assert.False(CheckInProcessor.TienePlaga(_estado, D.AddDays(23)));
        Assert.True(CheckInProcessor.TieneRaices(_estado, D.AddDays(27)));
    }

    [Fact]
    public void StrikesNoConsecutivosSeAcumulan()
    {
        En(0, CheckInResult.Roto);
        for (int d = 1; d <= 10; d++) En(d, CheckInResult.Cumplido);
        En(11, CheckInResult.Roto);

        Assert.Equal(2, _estado.StrikesDeclarados);
    }

    [Fact]
    public void Strike3_Muere_TodoACero()
    {
        for (int d = 0; d < 10; d++) En(d, CheckInResult.Cumplido);
        En(10, CheckInResult.Roto);
        En(30, CheckInResult.Roto);
        var r = En(60, CheckInResult.Roto);

        Assert.True(r.ArbolMurio);
        Assert.Equal(10, r.DiasAntesDeMorir);
        Assert.Equal(0, _habito.DiasDeCrecimiento);
        Assert.Equal(0, _estado.StrikesDeclarados);
        Assert.Null(_estado.FinPlaga);
        Assert.Null(_estado.FinRaices);
        Assert.Equal(D.AddDays(60), _habito.FechaDeInicioRachaActual);
    }

    [Fact]
    public void Borron_Antes90Dias_NoSePuede()
    {
        En(0, CheckInResult.Roto);
        _reloj.Ahora = _reloj.Ahora.AddDays(89);

        Assert.False(_proc.PuedeBorron(_estado));
        Assert.Throws<InvalidOperationException>(() => _proc.Borron(_estado));
    }

    [Fact]
    public void Borron_A90Dias_BorraAmbosContadores()
    {
        En(0, CheckInResult.Roto);
        _estado.StrikesAbandono = 1;
        _reloj.Ahora = _reloj.Ahora.AddDays(90);

        Assert.True(_proc.PuedeBorron(_estado));
        _proc.Borron(_estado);
        Assert.Equal(0, _estado.StrikesDeclarados);
        Assert.Equal(0, _estado.StrikesAbandono);
    }

    [Fact]
    public void Borron_SinStrikes_NoSeOfrece()
    {
        _reloj.Ahora = _reloj.Ahora.AddDays(200);
        Assert.False(_proc.PuedeBorron(_estado));
    }

    [Fact]
    public void Fenix_365SinStrikes_PrimerStrike_MarcaPendiente()
    {
        _habito.DiasDeCrecimiento = 365;
        En(0, CheckInResult.Roto);

        Assert.True(_estado.FenixPendiente);
    }

    [Fact]
    public void Fenix_ConStrikesPrevios_NoSeMarca()
    {
        En(0, CheckInResult.Roto);
        _habito.DiasDeCrecimiento = 400;
        En(10, CheckInResult.Roto);

        Assert.False(_estado.FenixPendiente);
    }

    [Fact]
    public void DosCheckInsElMismoDia_Lanza()
    {
        En(0, CheckInResult.Cumplido);
        Assert.Throws<InvalidOperationException>(() => En(0, CheckInResult.Cumplido));
    }

    [Fact]
    public void CheatDay_EnAbstinencia_Lanza() =>
        Assert.Throws<InvalidOperationException>(() => En(0, CheckInResult.CheatDay));

    [Fact]
    public void CheatDay_EnDiaNormal_Lanza()
    {
        var h = Nuevo(HabitType.Moderable, DayOfWeek.Sunday);
        Assert.Throws<InvalidOperationException>(() => En(0, CheckInResult.CheatDay, h)); // jueves
    }

    [Fact]
    public void CheckIn_ReiniciaFaltas()
    {
        _estado.DiasNormalesSinCheckIn = 9;
        En(0, CheckInResult.Cumplido);
        Assert.Equal(0, _estado.DiasNormalesSinCheckIn);
    }
}
