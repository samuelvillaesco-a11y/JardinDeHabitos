using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public sealed class SqliteGardenStoreTests : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"jardin-{Guid.NewGuid():N}.db3");

    public void Dispose()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    private static HabitRecord Registro() => new()
    {
        Habito = new Habit
        {
            Categoria = MalHabito.Azucares,
            EspecieDeArbol = "Naranjo",
            Tipo = HabitType.Moderable,
            FechaDeInicioRachaActual = new DateOnly(2026, 1, 1),
            DiasDeCrecimiento = 42,
            ApoyoTerapia = true,
            DiasDeCheat = [DayOfWeek.Saturday, DayOfWeek.Sunday],
            DiasDeCheatAnteriores = [DayOfWeek.Wednesday],
            CambioDeCheatDesde = new DateOnly(2026, 1, 4),
        },
        Estado = new StrikeState
        {
            StrikesDeclarados = 2,
            StrikesAbandono = 1,
            FinPlaga = new DateOnly(2026, 2, 1),
            FinRaices = new DateOnly(2026, 2, 6),
            FechaUltimoCheckIn = new DateOnly(2026, 1, 30),
            FechaUltimoStrike = new DateOnly(2026, 1, 29),
            DiasSinStrikeBase = 40,
            FenixPendiente = true,
            DiasNormalesSinCheckIn = 3,
            UltimoDiaEvaluado = new DateOnly(2026, 1, 30),
            MiniEvento = MiniEvento.RamaSeca,
        },
        UltimoRiego = new DateOnly(2026, 1, 29),
        UltimoTratamiento = new DateOnly(2026, 1, 30),
        UltimaAsistencia = new DateOnly(2026, 1, 28),
        UltimoCumplido = new DateOnly(2026, 1, 27),
        CuidadoPendiente = true,
        TratamientosEnCuidado = 2,
        ConstanciaActual = 5,
        Orden = 3,
    };

    [Fact]
    public void Guardar_YCargar_IdaYVuelta()
    {
        var r = Registro();
        using (var store = new SqliteGardenStore(_ruta))
        {
            store.Guardar(r);
            store.GuardarEstadisticas(new Dictionary<string, int> { ["racha:Azucares"] = 42 });
            store.AgregarNivel(new NivelLogro("hab.azucares.tiempo1", 2, new DateOnly(2026, 1, 5)));
        }

        using var otra = new SqliteGardenStore(_ruta);
        var l = Assert.Single(otra.Cargar());

        Assert.Equivalent(r.Habito, l.Habito);
        Assert.Equivalent(r.Estado, l.Estado);
        Assert.Equal(r.UltimaAsistencia, l.UltimaAsistencia);
        Assert.Equal(r.UltimoCumplido, l.UltimoCumplido);
        Assert.True(l.CuidadoPendiente);
        Assert.Equal(2, l.TratamientosEnCuidado);
        Assert.Equal(5, l.ConstanciaActual);
        Assert.Equal(3, l.Orden);
        Assert.Equal(42, otra.Estadisticas()["racha:Azucares"]);
        Assert.Equal(new NivelLogro("hab.azucares.tiempo1", 2, new DateOnly(2026, 1, 5)), Assert.Single(otra.NivelesLogro()));
    }

    [Fact]
    public void Eliminar_Borra()
    {
        using var store = new SqliteGardenStore(_ruta);
        var r = Registro();
        store.Guardar(r);
        store.Eliminar(r.Habito.Id);
        Assert.Empty(store.Cargar());
    }

    [Fact]
    public void EnTransaccion_SiFalla_NoQuedaNadaGuardado()
    {
        using var store = new SqliteGardenStore(_ruta);

        Assert.Throws<IOException>(() => store.EnTransaccion(() =>
        {
            store.Guardar(Registro());
            store.AgregarNivel(new NivelLogro("gen.semilla", 1, new DateOnly(2026, 1, 1)));
            store.EnTransaccion(() => store.GuardarEstadisticas(new Dictionary<string, int> { ["x"] = 1 }));
            throw new IOException("disco lleno");
        }));

        Assert.Empty(store.Cargar());
        Assert.Empty(store.NivelesLogro());
        Assert.Empty(store.Estadisticas());
    }

    [Fact]
    public void Servicio_SobreSqlite_FuncionaDeIdaYVuelta()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        Guid id;
        using (var store = new SqliteGardenStore(_ruta))
        {
            var svc = new GardenService(store, new GrowthStageCalculator(reloj));
            id = svc.CrearHabito(MalHabito.Cigarro, "Manzano", HabitType.AbstinenciaTotal, []).Id;
            svc.CheckIn(id, CheckInResult.Cumplido);
            reloj.Ahora = reloj.Ahora.AddDays(15);
            svc.Refrescar();
        }

        using var otra = new SqliteGardenStore(_ruta);
        var v = new GardenService(otra, new GrowthStageCalculator(reloj)).Arbol(id);
        Assert.Equal(1, v.Dias);
        Assert.Equal(1, v.StrikesAbandono);
        Assert.True(v.Plaga);
    }

    [Fact]
    public void BaseNueva_QuedaConLaVersionDeEsquemaActual()
    {
        using var store = new SqliteGardenStore(_ruta);
        Assert.Equal(SqliteGardenStore.VersionActual, store.VersionEsquema);
    }

    [Fact]
    public void BaseDeVersionMasNueva_Lanza()
    {
        using (new SqliteGardenStore(_ruta)) { }
        PonerVersion(SqliteGardenStore.VersionActual + 1);

        Assert.Throws<InvalidOperationException>(() => new SqliteGardenStore(_ruta));
    }

    [Fact]
    public void Sqlite_MigraV1()
    {
        CrearBaseV1();
        var hoy = new DateOnly(2026, 1, 11);

        using var store = new SqliteGardenStore(_ruta, hoy);

        Assert.Equal(2, store.VersionEsquema);
        var arboles = store.Cargar();
        Assert.Equal(3, arboles.Count);

        var cig = arboles.Single(a => a.Habito.Categoria == MalHabito.Cigarro);
        Assert.Equal(10, cig.Habito.DiasDeCrecimiento);
        Assert.Equal(2, cig.Estado.StrikesDeclarados); // 5 cicatrices, tope 2
        Assert.Equal(new DateOnly(2026, 1, 13), cig.Estado.FinRaices);
        Assert.Equal(hoy.AddDays(-1), cig.Estado.UltimoDiaEvaluado);

        Assert.Contains(arboles, a => a.Habito.Categoria == MalHabito.Otros);
        // Especies repetidas de v1 se conservan.
        Assert.Equal(2, arboles.Count(a => a.Habito.EspecieDeArbol == "Manzano"));

        var stats = store.Estadisticas();
        Assert.Equal(1, stats["elegido:Cigarro"]);
        Assert.Equal(1, stats[Stat.LeyendaCaida]);
        Assert.Contains(store.NivelesLogro(), n => n.Id == "mal.leyenda_caida");

        // Y el servicio funciona sobre la base migrada.
        var reloj = new RelojFijo(new DateTimeOffset(hoy.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
        var svc = new GardenService(store, new GrowthStageCalculator(reloj));
        Assert.Equal(3, svc.Jardin().Count);
        Assert.Contains(svc.Estante(), e => e.Def.Id == "mal.leyenda_caida" && e.Nivel == 1);
    }

    private void CrearBaseV1()
    {
        using var db = new SQLite.SQLiteConnection(_ruta);
        db.Execute("""
            CREATE TABLE Habitos (Id varchar primary key not null, Nombre varchar, Especie varchar, Tipo integer,
              InicioRacha integer, DiasDeCheat integer, DiasDeCheatAnteriores integer, CambioDeCheatDesde integer,
              Racha integer, Cicatrices integer, Nivel integer, FinPenalizacion integer, UltimoCheckIn integer,
              UltimoRiego integer, UltimaPoda integer, UltimoTratamiento integer, Orden integer)
            """);
        db.Execute("CREATE TABLE Logros (Id integer primary key autoincrement not null, Logro integer, Fecha integer, Habito varchar)");
        int inicio = new DateOnly(2026, 1, 1).DayNumber;
        db.Execute("INSERT INTO Habitos VALUES (?, 'Cigarros', 'Manzano', 0, ?, 0, 0, NULL, 2, 5, 2, ?, ?, NULL, NULL, NULL, 0)",
            Guid.NewGuid().ToString(), inicio, new DateOnly(2026, 1, 13).DayNumber, new DateOnly(2026, 1, 10).DayNumber);
        db.Execute("INSERT INTO Habitos VALUES (?, 'Morderme las uñas', 'Manzano', 0, ?, 0, 0, NULL, 0, 0, 0, NULL, NULL, NULL, NULL, NULL, 1)",
            Guid.NewGuid().ToString(), inicio);
        db.Execute("INSERT INTO Habitos VALUES (?, 'Azúcar', 'Naranjo', 1, ?, 1, 0, NULL, 0, 0, 0, NULL, NULL, NULL, NULL, NULL, 2)",
            Guid.NewGuid().ToString(), inicio);
        db.Execute("INSERT INTO Logros (Logro, Fecha, Habito) VALUES (1, ?, 'Alcohol')", inicio);
        db.Execute("PRAGMA user_version = 1");
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
}
