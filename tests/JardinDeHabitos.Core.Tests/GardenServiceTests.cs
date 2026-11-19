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

    private VistaArbol Crear(MalHabito h = MalHabito.Cigarro, string especie = "Manzano",
        HabitType tipo = HabitType.AbstinenciaTotal, params DayOfWeek[] cheat) =>
        _svc.CrearHabito(h, especie, tipo, cheat);

    /// <summary>Check-in "cumplido" n días seguidos, empezando hoy.</summary>
    private void Cumplir(Guid id, int dias)
    {
        for (int i = 0; i < dias; i++)
        {
            _svc.CheckIn(id, CheckInResult.Cumplido);
            _reloj.AvanzarDias(1);
        }
    }

    private bool Tiene(string id) => _store.NivelesLogro().Any(n => n.Id == id);

    [Fact]
    public void CrearHabito_EmpiezaEnSemillaYPideCheckIn()
    {
        var v = Crear();

        Assert.Equal(GrowthStage.Semilla, v.Etapa);
        Assert.Equal(0, v.Dias);
        Assert.True(v.PideCheckInHoy);
        Assert.Equal("Manzano", v.Especie);
    }

    [Fact]
    public void CrearHabito_EspecieUsada_Lanza()
    {
        Crear(MalHabito.Cigarro, "Manzano");
        Assert.Throws<InvalidOperationException>(() => Crear(MalHabito.Sal, "Manzano", HabitType.Moderable));
        Assert.DoesNotContain("Manzano", _svc.EspeciesLibres());
    }

    [Fact]
    public void CrearHabito_HabitoRepetido_Lanza_SalvoOtros()
    {
        Crear(MalHabito.Cigarro, "Manzano");
        Assert.Throws<InvalidOperationException>(() => Crear(MalHabito.Cigarro, "Naranjo"));
        Assert.DoesNotContain(_svc.HabitosDisponibles(), h => h.Habito == MalHabito.Cigarro);

        Crear(MalHabito.Otros, "Naranjo");
        Crear(MalHabito.Otros, "Limonero");
        Assert.Contains(_svc.HabitosDisponibles(), h => h.Habito == MalHabito.Otros);
    }

    [Fact]
    public void CrearHabito_ModoNoPermitido_Lanza() =>
        Assert.Throws<InvalidOperationException>(() => Crear(MalHabito.Apuestas, "Manzano", HabitType.Moderable));

    [Fact]
    public void CrearHabito_DiasPrevios_NaceCrecido()
    {
        var v = _svc.CrearHabito(MalHabito.Alcohol, "Cerezo", HabitType.AbstinenciaTotal, [], diasPrevios: 400);

        Assert.Equal(GrowthStage.FrutaLegendaria, v.Etapa);
        Assert.Contains(_svc.Estante(), e => e.Def.Id == "hab.alcohol.tiempo2" && e.Nivel == 3);
    }

    [Fact]
    public void CrearHabito_MasDeDiez_Lanza()
    {
        var especies = EspeciesDeArbol.Todas;
        for (var i = 0; i < 10; i++) Crear(MalHabito.Otros, especies[i]);

        Assert.Throws<InvalidOperationException>(() => Crear(MalHabito.Sal, "Manzano", HabitType.Moderable));
    }

    [Fact]
    public void CheckIn_PersisteYNoPideOtraVezHoy()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Cumplido);

        var hoy = _svc.Jardin().Single();
        Assert.False(hoy.PideCheckInHoy);
        Assert.Equal(1, hoy.Dias);
        Assert.Throws<InvalidOperationException>(() => _svc.CheckIn(v.Id, CheckInResult.Cumplido));
    }

    [Fact]
    public void SinCheckIn_NoCrece()
    {
        var v = Crear();
        _reloj.AvanzarDias(10);

        var hoy = _svc.Arbol(v.Id);
        Assert.Equal(0, hoy.Dias);
        Assert.Equal(10, hoy.DiasSinVisita);
    }

    [Fact]
    public void DosStrikesSeguidos_CuidadoDeOchoDias()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        _svc.CheckIn(v.Id, CheckInResult.Roto);

        var hoy = _svc.Arbol(v.Id);
        Assert.True(hoy.Plaga && hoy.Raices);
        Assert.Equal(new DateOnly(2026, 1, 9), hoy.FinCuidado);
    }

    [Fact]
    public void Estante_GeneralesVisibles_HabitoAlPlantar_OcultosNunca()
    {
        Assert.Contains(_svc.Estante(), e => e.Def.Id == "gen.semilla");
        Assert.DoesNotContain(_svc.Estante(), e => e.Def.Categoria == CategoriaLogro.Habito);

        Crear(MalHabito.Apuestas, "Cerezo");
        var estante = _svc.Estante();
        Assert.True(estante.Count(e => e.Def.Habito == MalHabito.Apuestas) >= 10);
        Assert.DoesNotContain(estante, e => e.Def.Habito is { } h && h != MalHabito.Apuestas);
        Assert.DoesNotContain(estante, e => e.Def.Categoria is CategoriaLogro.Oculto or CategoriaLogro.Maldito or CategoriaLogro.Legendario);
        Assert.Equal("Cerezo", estante.First(e => e.Def.Habito == MalHabito.Apuestas).Arbol);
    }

    [Fact]
    public void Estante_DiasLibres_SoloSiModera()
    {
        Crear(MalHabito.Sal, "Naranjo", HabitType.AbstinenciaTotal);
        Assert.DoesNotContain(_svc.Estante(), e => e.Def.Id == "hab.sal.libres");

        Crear(MalHabito.Azucares, "Mango", HabitType.Moderable, DayOfWeek.Sunday);
        Assert.Contains(_svc.Estante(), e => e.Def.Id == "hab.azucares.libres");
    }

    [Fact]
    public void LogrosNuevos_SeAnuncianUnaSolaVez()
    {
        Crear();
        Assert.Equal(1, _svc.TomarLogrosNuevos()); // Manos a la tierra ★
        Assert.Equal(0, _svc.TomarLogrosNuevos());
        _svc.Refrescar();
        Assert.Equal(0, _svc.TomarLogrosNuevos());
    }

    [Fact]
    public void Riego_DeNoche_CuentaParaOculto_SinMostrarlo()
    {
        var v = Crear();
        for (int i = 0; i < 10; i++)
        {
            _reloj.Ahora = new DateTimeOffset(2026, 1, 1 + i, 22, 0, 0, TimeSpan.Zero);
            _svc.RegistrarJuego(v.Id, MiniJuegoTipo.Regar);
        }

        Assert.True(Tiene("oc.nocturno"));
        Assert.Contains(_svc.Estante(), e => e.Def.Id == "oc.nocturno" && e.Nivel == 1);
    }

    [Fact]
    public void Poda_RepetidaElMismoDia_NoSumaRamas()
    {
        var v = Crear();
        _svc.RegistrarJuego(v.Id, MiniJuegoTipo.Podar, 3);
        _svc.RegistrarJuego(v.Id, MiniJuegoTipo.Podar, 3);

        Assert.Equal(3, _store.Estadisticas()[Stat.Ramas]);
    }

    [Fact]
    public void Muerte_DeArbolLegendario_LeyendaCaidaConDebuff_YCoronaLoRedime()
    {
        var v = _svc.CrearHabito(MalHabito.Cigarro, "Manzano", HabitType.AbstinenciaTotal, [], diasPrevios: 365);
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        _svc.CheckIn(v.Id, CheckInResult.Roto);

        Assert.True(Tiene("mal.leyenda_caida"));
        Assert.Contains(Debuff.MasBichos, _svc.Debuffs());

        _svc.CrearHabito(MalHabito.Sal, "Naranjo", HabitType.AbstinenciaTotal, [], diasPrevios: 365);
        Assert.True(Tiene("leg.corona"));
        Assert.Empty(_svc.Debuffs());
    }

    [Fact]
    public void Muerte_NoLegendaria_NoEsMaldita()
    {
        var v = Crear();
        for (int i = 0; i < 3; i++)
        {
            _svc.CheckIn(v.Id, CheckInResult.Roto);
            _reloj.AvanzarDias(1);
        }

        Assert.DoesNotContain(_store.NivelesLogro(), n => n.Id.StartsWith("mal."));
    }

    [Fact]
    public void Fenix_UnAnoLimpio_Recaida_CuidadoCompleto()
    {
        var v = _svc.CrearHabito(MalHabito.Alcohol, "Cerezo", HabitType.AbstinenciaTotal, [], diasPrevios: 365);
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        for (int i = 0; i < 3; i++)
        {
            _svc.RegistrarTratamiento(v.Id);
            _reloj.AvanzarDias(1);
            if (i < 2) _svc.CheckIn(v.Id, CheckInResult.Cumplido);
        }
        _svc.Refrescar();

        Assert.True(Tiene("leg.fenix"));
        Assert.True(Tiene("hab.alcohol.cura"));
    }

    [Fact]
    public void Cuidado_SinTratar_NoCuentaComoCurado()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(4);
        _svc.Refrescar();

        Assert.False(Tiene("gen.cura"));
    }

    [Fact]
    public void Abandono_GeneraAvisosSinNombreDeHabito()
    {
        Crear(MalHabito.Pornografia, "Limonero");
        _reloj.AvanzarDias(7);
        var avisos = _svc.TomarAvisos();

        var a = Assert.Single(avisos);
        Assert.Equal(TipoAviso.MiniEvento, a.Tipo);
        Assert.Equal("Limonero", a.Especie);
        Assert.Empty(_svc.TomarAvisos());

        _reloj.AvanzarDias(7);
        Assert.Equal(TipoAviso.PlagaPorAbandono, Assert.Single(_svc.TomarAvisos()).Tipo);
    }

    [Fact]
    public void Avisos_EtapaNueva_UnaVez()
    {
        var v = Crear();
        Cumplir(v.Id, 7);

        var a = Assert.Single(_svc.TomarAvisos());
        Assert.Equal(GrowthStage.Flor, a.Etapa);
        Assert.Empty(_svc.TomarAvisos());
    }

    [Fact]
    public void MiniEvento_SeResuelve_YCuenta()
    {
        var v = Crear();
        _reloj.AvanzarDias(8);
        Assert.NotEqual(MiniEvento.Ninguno, _svc.Arbol(v.Id).MiniEvento);

        _svc.ResolverMiniEvento(v.Id, bichos: 2);

        Assert.Equal(MiniEvento.Ninguno, _svc.Arbol(v.Id).MiniEvento);
        Assert.True(Tiene("oc.rescate"));
        Assert.Throws<InvalidOperationException>(() => _svc.ResolverMiniEvento(v.Id));
    }

    [Fact]
    public void Asistencia_SoloConApoyo_UnaVezAlDia()
    {
        var sin = Crear();
        Assert.Throws<InvalidOperationException>(() => _svc.RegistrarAsistencia(sin.Id));

        var con = _svc.CrearHabito(MalHabito.Alcohol, "Cerezo", HabitType.AbstinenciaTotal, [], grupo: true);
        _svc.RegistrarAsistencia(con.Id);
        Assert.True(_svc.Arbol(con.Id).AsistioHoy);
        Assert.Throws<InvalidOperationException>(() => _svc.RegistrarAsistencia(con.Id));
        Assert.True(Tiene("gen.apoyo"));
    }

    [Fact]
    public void Borron_A90Dias_YLogroTierraNueva()
    {
        var v = Crear();
        _svc.CheckIn(v.Id, CheckInResult.Roto);
        _reloj.AvanzarDias(1);
        Cumplir(v.Id, 88);
        Assert.False(_svc.Arbol(v.Id).PuedeBorron); // día 89
        _svc.CheckIn(v.Id, CheckInResult.Cumplido);
        _reloj.AvanzarDias(1);                       // día 90

        Assert.True(_svc.Arbol(v.Id).PuedeBorron);
        Assert.True(Tiene("hab.cigarro.borron"));
        _svc.Borron(v.Id);
        Assert.Equal(0, _svc.Arbol(v.Id).Strikes);
    }

    [Fact]
    public void DiaLibre_CreceSoloSiHuboCheckIn()
    {
        var v = Crear(MalHabito.Azucares, "Mango", HabitType.Moderable, DayOfWeek.Friday);
        _svc.CheckIn(v.Id, CheckInResult.Cumplido); // jueves
        _reloj.AvanzarDias(2);                      // sábado (viernes libre cerrado)

        Assert.Equal(2, _svc.Arbol(v.Id).Dias);
    }

    [Fact]
    public void EditarDiasDeCheat_SoloSabado()
    {
        var v = Crear(MalHabito.Cafeina, "Olivo", HabitType.Moderable, DayOfWeek.Sunday);
        Assert.Throws<InvalidOperationException>(() => _svc.EditarDiasDeCheat(v.Id, [DayOfWeek.Friday]));

        _reloj.AvanzarDias(2); // sábado
        _svc.EditarDiasDeCheat(v.Id, [DayOfWeek.Friday, DayOfWeek.Saturday]);
        _reloj.AvanzarDias(1);
        Assert.Equal([DayOfWeek.Friday, DayOfWeek.Saturday], _svc.Jardin().Single().DiasDeCheat);
    }

    [Fact]
    public void EditarDiasDeCheat_NoConvierteHoyEnDiaLibre()
    {
        var v = Crear(MalHabito.Cafeina, "Olivo", HabitType.Moderable, DayOfWeek.Sunday);
        _reloj.AvanzarDias(2); // sábado
        _svc.EditarDiasDeCheat(v.Id, [DayOfWeek.Saturday]);

        var hoy = _svc.Jardin().Single();
        Assert.False(hoy.EsCheatDayHoy);
        Assert.True(hoy.PideCheckInHoy);
    }

    [Fact]
    public void LogrosSobrevivenAlEliminarHabito()
    {
        var v = Crear();
        _svc.EliminarHabito(v.Id);

        Assert.Empty(_svc.Jardin());
        Assert.True(Tiene("gen.semilla"));
        Assert.Contains(_svc.Estante(), e => e.Def.Habito == MalHabito.Cigarro);
    }

    [Fact]
    public void Estante_NuevoHastaAbrirlo_YVuelveConLaSiguienteEstrella()
    {
        Crear();
        Assert.True(_svc.Estante().Single(e => e.Def.Id == "gen.semilla").Nuevo);

        _svc.MarcarVisto("gen.semilla");
        Assert.False(_svc.Estante().Single(e => e.Def.Id == "gen.semilla").Nuevo);
        Assert.False(_svc.Estante().Single(e => e.Def.Id == "gen.cura").Nuevo); // sin ganar: nunca "nuevo"

        Crear(MalHabito.Sal, "Naranjo");
        Crear(MalHabito.Azucares, "Mango");
        Assert.True(_svc.Estante().Single(e => e.Def.Id == "gen.semilla").Nuevo); // ★★
    }

    [Fact]
    public void HabitoInexistente_Lanza() =>
        Assert.Throws<KeyNotFoundException>(() => _svc.RegistrarJuego(Guid.NewGuid(), MiniJuegoTipo.Regar));
}
