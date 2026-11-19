using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public sealed class SqliteGardenStoreTests : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"jardin-{Guid.NewGuid():N}.db3");

    public void Dispose()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    [Fact]
    public void Guardar_YCargar_IdaYVuelta()
    {
        var registro = new HabitRecord
        {
            Habito = new Habit
            {
                Nombre = "Azúcar",
                EspecieDeArbol = "Naranjo",
                Tipo = HabitType.Moderable,
                FechaDeInicioRachaActual = new DateOnly(2026, 1, 1),
                DiasDeCheat = [DayOfWeek.Saturday, DayOfWeek.Sunday],
                DiasDeCheatAnteriores = [DayOfWeek.Wednesday],
                CambioDeCheatDesde = new DateOnly(2026, 1, 4),
            },
            Estado = new StrikeState
            {
                RachaConsecutivaDeDiasRotos = 2,
                StrikesTotalesDelArbolActual = 5,
                NivelDePenalizacion = PenaltyLevel.Strike2,
                FechaFinDePenalizacionActiva = new DateOnly(2026, 2, 3),
                FechaUltimoCheckIn = new DateOnly(2026, 1, 30),
            },
            UltimoRiego = new DateOnly(2026, 1, 29),
            UltimaPoda = null,
            UltimoTratamiento = new DateOnly(2026, 1, 30),
            Orden = 3,
        };

        using (var store = new SqliteGardenStore(_ruta))
        {
            store.Guardar(registro);
            store.AgregarLogro(new LogroDesbloqueado(Logro.FrutaLegendaria, new DateOnly(2026, 1, 5), "Azúcar"));
        }

        using var otra = new SqliteGardenStore(_ruta);
        var leido = Assert.Single(otra.Cargar());

        Assert.Equal(registro.Habito.Id, leido.Habito.Id);
        Assert.Equal("Azúcar", leido.Habito.Nombre);
        Assert.Equal("Naranjo", leido.Habito.EspecieDeArbol);
        Assert.Equal(HabitType.Moderable, leido.Habito.Tipo);
        Assert.Equal(new DateOnly(2026, 1, 1), leido.Habito.FechaDeInicioRachaActual);
        Assert.Equal([DayOfWeek.Sunday, DayOfWeek.Saturday], leido.Habito.DiasDeCheat.Order());
        Assert.Equal([DayOfWeek.Wednesday], leido.Habito.DiasDeCheatAnteriores);
        Assert.Equal(new DateOnly(2026, 1, 4), leido.Habito.CambioDeCheatDesde);
        Assert.Equal(2, leido.Estado.RachaConsecutivaDeDiasRotos);
        Assert.Equal(5, leido.Estado.StrikesTotalesDelArbolActual);
        Assert.Equal(PenaltyLevel.Strike2, leido.Estado.NivelDePenalizacion);
        Assert.Equal(new DateOnly(2026, 2, 3), leido.Estado.FechaFinDePenalizacionActiva);
        Assert.Equal(new DateOnly(2026, 1, 30), leido.Estado.FechaUltimoCheckIn);
        Assert.Equal(new DateOnly(2026, 1, 29), leido.UltimoRiego);
        Assert.Null(leido.UltimaPoda);
        Assert.Equal(new DateOnly(2026, 1, 30), leido.UltimoTratamiento);
        Assert.Equal(3, leido.Orden);

        var logro = Assert.Single(otra.Logros());
        Assert.Equal(Logro.FrutaLegendaria, logro.Logro);
        Assert.Equal("Azúcar", logro.Habito);
    }

    [Fact]
    public void Guardar_DosVeces_Actualiza_YEliminar_Borra()
    {
        using var store = new SqliteGardenStore(_ruta);
        var r = new HabitRecord
        {
            Habito = new Habit { Nombre = "A", EspecieDeArbol = "Limonero", Tipo = HabitType.AbstinenciaTotal },
        };
        store.Guardar(r);
        r.Habito.Nombre = "B";
        store.Guardar(r);

        Assert.Equal("B", Assert.Single(store.Cargar()).Habito.Nombre);

        store.Eliminar(r.Habito.Id);
        Assert.Empty(store.Cargar());
    }

    [Fact]
    public void EnTransaccion_SiFalla_NoQuedaNadaGuardado()
    {
        using var store = new SqliteGardenStore(_ruta);
        var r = new HabitRecord
        {
            Habito = new Habit { Nombre = "A", EspecieDeArbol = "Limonero", Tipo = HabitType.AbstinenciaTotal },
        };

        Assert.Throws<IOException>(() => store.EnTransaccion(() =>
        {
            store.Guardar(r);
            store.AgregarLogro(new LogroDesbloqueado(Logro.FrutaLegendaria, new DateOnly(2026, 1, 1), "A"));
            throw new IOException("disco lleno");
        }));

        Assert.Empty(store.Cargar());
        Assert.Empty(store.Logros());
    }

    [Fact]
    public void MuerteLegendaria_SiFallaGuardarElLogro_ElArbolNoMuere()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        using var sqlite = new SqliteGardenStore(_ruta);
        var store = new StoreQueFallaEnLeyenda(sqlite);
        var svc = new GardenService(store, new GrowthStageCalculator(reloj));
        var v = svc.CrearHabito("Cigarro", "Manzano", HabitType.AbstinenciaTotal, []);
        reloj.Ahora = reloj.Ahora.AddDays(400);
        svc.CheckIn(v.Id, CheckInResult.Roto);
        reloj.Ahora = reloj.Ahora.AddDays(1);
        svc.CheckIn(v.Id, CheckInResult.Roto);
        reloj.Ahora = reloj.Ahora.AddDays(1);

        Assert.Throws<IOException>(() => svc.CheckIn(v.Id, CheckInResult.Roto));

        // Todo o nada: el árbol sigue legendario y puede volver a intentarlo.
        Assert.Equal(GrowthStage.FrutaLegendaria, svc.Jardin().Single().Etapa);
        Assert.Empty(sqlite.Logros());
    }

    [Fact]
    public void BaseNueva_QuedaConLaVersionDeEsquemaActual()
    {
        using var store = new SqliteGardenStore(_ruta);

        Assert.Equal(SqliteGardenStore.VersionActual, store.VersionEsquema);
    }

    [Fact]
    public void BaseSinVersion_SeActualiza_YConservaDatos()
    {
        using (var store = new SqliteGardenStore(_ruta))
            store.Guardar(new HabitRecord { Habito = new Habit { Nombre = "A", EspecieDeArbol = "Limonero", Tipo = HabitType.AbstinenciaTotal } });
        PonerVersion(0);

        using var otra = new SqliteGardenStore(_ruta);

        Assert.Equal(SqliteGardenStore.VersionActual, otra.VersionEsquema);
        Assert.Equal("A", Assert.Single(otra.Cargar()).Habito.Nombre);
    }

    [Fact]
    public void BaseDeVersionMasNueva_Lanza()
    {
        using (new SqliteGardenStore(_ruta)) { }
        PonerVersion(SqliteGardenStore.VersionActual + 1);

        Assert.Throws<InvalidOperationException>(() => new SqliteGardenStore(_ruta));
    }

    private void PonerVersion(int version)
    {
        using var db = new SQLite.SQLiteConnection(_ruta);
        db.Execute($"PRAGMA user_version = {version}");
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>Simula que el disco falla justo al guardar LeyendaCaida.</summary>
    private sealed class StoreQueFallaEnLeyenda(SqliteGardenStore real) : IGardenStore
    {
        public IReadOnlyList<HabitRecord> Cargar() => real.Cargar();
        public void Guardar(HabitRecord registro) => real.Guardar(registro);
        public void Eliminar(Guid habitoId) => real.Eliminar(habitoId);
        public IReadOnlyList<LogroDesbloqueado> Logros() => real.Logros();
        public void EnTransaccion(Action accion) => real.EnTransaccion(accion);
        public void AgregarLogro(LogroDesbloqueado logro)
        {
            if (logro.Logro == Logro.LeyendaCaida) throw new IOException("disco lleno");
            real.AgregarLogro(logro);
        }
    }
}
