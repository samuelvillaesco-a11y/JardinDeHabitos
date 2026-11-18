using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class GardenServiceTests
{
    private sealed class RelojManual : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero); // jueves
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public void AvanzarDias(int n) => Ahora = Ahora.AddDays(n);
    }

    private readonly RelojManual _reloj = new();
    private readonly InMemoryGardenStore _store = new();
    private readonly GardenService _svc;

    public GardenServiceTests() => _svc = new GardenService(_store, new GrowthStageCalculator(_reloj));

    private VistaArbol Crear(string nombre = "Cigarro", HabitType tipo = HabitType.AbstinenciaTotal, params DayOfWeek[] cheat) =>
        _svc.CrearHabito(nombre, "Manzano", tipo, cheat);

    [Fact]
    public void CrearHabito_EmpiezaEnSemillaYPideCheckIn()
    {
        var v = Crear();

        Assert.Equal(GrowthStage.Semilla, v.Etapa);
        Assert.Equal(0, v.Dias);
        Assert.True(v.PideCheckInHoy);
        Assert.Single(_svc.Jardin());
    }

    [Fact]
    public void CrearHabito_MasDeDiez_Lanza()
    {
        for (var i = 0; i < 10; i++) Crear($"h{i}");

        Assert.Throws<InvalidOperationException>(() => Crear("once"));
    }

    [Fact]
    public void CrearHabito_NombreVacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => Crear("  "));
    }

    [Fact]
    public void CrearHabito_AbstinenciaConCheat_Lanza()
    {
        Assert.Throws<ArgumentException>(() => Crear("Alcohol", HabitType.AbstinenciaTotal, DayOfWeek.Sunday));
    }

    [Fact]
    public void CheckIn_PersisteYNoPideOtraVezHoy()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);

        var hoy = _svc.Jardin().Single();
        Assert.False(hoy.PideCheckInHoy);
        Assert.Equal(1, hoy.Racha);
        Assert.Equal(1, hoy.Cicatrices);
        Assert.Equal(PenaltyLevel.Strike1, hoy.Penalizacion);

        _reloj.AvanzarDias(1);
        Assert.True(_svc.Jardin().Single().PideCheckInHoy);
    }

    [Fact]
    public void Vista_PenalizacionVencida_SeMuestraNinguna()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(2);

        Assert.Equal(PenaltyLevel.Ninguna, _svc.Jardin().Single().Penalizacion);
    }

    [Fact]
    public void CheatDayHoy_NoPideCheckIn()
    {
        _reloj.AvanzarDias(3); // domingo
        var v = Crear("Azúcar", HabitType.Moderable, DayOfWeek.Sunday);

        Assert.True(v.EsCheatDayHoy);
        Assert.False(v.PideCheckInHoy);
    }

    [Fact]
    public void Muerte_DeArbolLegendario_DesbloqueaLeyendaCaida()
    {
        var v = Crear();
        _reloj.AvanzarDias(400);
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        var r = _svc.CheckIn(v.Id, CheckInResult.Roto);

        Assert.True(r.Outcome.ArbolMurio);
        Assert.Contains(Logro.LeyendaCaida, r.NuevosLogros);
        Assert.Contains(_svc.LogrosDesbloqueados(), l => l.Logro == Logro.LeyendaCaida);
        Assert.Contains(_svc.LogrosDesbloqueados(), l => l.Logro == Logro.FrutaLegendaria);
        Assert.Equal(GrowthStage.Semilla, _svc.Jardin().Single().Etapa);
    }

    [Fact]
    public void Muerte_NoLegendaria_NoDesbloqueaNada()
    {
        var v = Crear();
        for (var i = 0; i < 3; i++)
        {
            _svc.CheckIn(v.Id, CheckInResult.Roto);
            _reloj.AvanzarDias(1);
        }

        Assert.Empty(_svc.LogrosDesbloqueados());
    }

    [Fact]
    public void Refrescar_RegistraFrutaLegendariaUnaSolaVez()
    {
        Crear("a");
        Crear("b");
        _reloj.AvanzarDias(365);

        Assert.Equal([Logro.FrutaLegendaria], _svc.Refrescar());
        Assert.Empty(_svc.Refrescar());
        Assert.Single(_svc.LogrosDesbloqueados());
    }

    [Fact]
    public void LeyendaCaida_EsSecretaHastaDesbloquear()
    {
        Assert.DoesNotContain(Logro.LeyendaCaida, _svc.LogrosPorDesbloquear());
        Assert.Contains(Logro.FrutaLegendaria, _svc.LogrosPorDesbloquear());
        Assert.True(LogroInfo.De(Logro.LeyendaCaida).EsSecreto);
    }

    [Fact]
    public void LogrosSobrevivenAlEliminarHabito()
    {
        var v = Crear();
        _reloj.AvanzarDias(365);
        _svc.Refrescar();
        _svc.EliminarHabito(v.Id);

        Assert.Empty(_svc.Jardin());
        Assert.Single(_svc.LogrosDesbloqueados());
    }

    [Fact]
    public void Regar_YPodar_SoloCuentanHoy()
    {
        var v = Crear();
        _svc.Regar(v.Id);
        _svc.Podar(v.Id);

        var hoy = _svc.Jardin().Single();
        Assert.True(hoy.RegadoHoy);
        Assert.True(hoy.PodadoHoy);

        _reloj.AvanzarDias(1);
        var manana = _svc.Jardin().Single();
        Assert.False(manana.RegadoHoy);
        Assert.False(manana.PodadoHoy);
    }

    [Fact]
    public void Tratamiento_SinPenalizacion_Lanza()
    {
        var v = Crear();
        Assert.Throws<InvalidOperationException>(() => _svc.RegistrarTratamiento(v.Id));
    }

    [Fact]
    public void Tratamiento_ConPenalizacion_MarcaTratadoHoy()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _svc.RegistrarTratamiento(v.Id);

        Assert.True(_svc.Jardin().Single().TratadoHoy);
        _reloj.AvanzarDias(1);
        Assert.False(_svc.Jardin().Single().TratadoHoy);
    }

    [Fact]
    public void EditarDiasDeCheat_SoloSabado()
    {
        var v = Crear("Café", HabitType.Moderable, DayOfWeek.Sunday);
        Assert.Throws<InvalidOperationException>(() => _svc.EditarDiasDeCheat(v.Id, [DayOfWeek.Friday]));

        _reloj.AvanzarDias(2); // sábado
        _svc.EditarDiasDeCheat(v.Id, [DayOfWeek.Friday, DayOfWeek.Saturday]);
        Assert.Equal([DayOfWeek.Friday, DayOfWeek.Saturday], _svc.Jardin().Single().DiasDeCheat);
    }

    [Fact]
    public void BorrarCicatrices_A180Dias()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(180);

        Assert.True(_svc.Jardin().Single().PuedeBorrarCicatrices);
        _svc.BorrarCicatrices(v.Id);
        Assert.Equal(0, _svc.Jardin().Single().Cicatrices);
    }

    [Fact]
    public void HabitoInexistente_Lanza()
    {
        Assert.Throws<KeyNotFoundException>(() => _svc.Regar(Guid.NewGuid()));
    }
}
