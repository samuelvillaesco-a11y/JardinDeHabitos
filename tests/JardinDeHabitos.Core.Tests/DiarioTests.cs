using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public sealed class DiarioTests : IDisposable
{
    private sealed class Reloj : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateOnly Hoy = new(2026, 3, 10);
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"diario-{Guid.NewGuid():N}.db3");
    private readonly Reloj _reloj = new();
    private readonly InMemoryGardenStore _memoria = new();
    private readonly GardenService _svc;

    public DiarioTests() => _svc = new GardenService(_memoria, new GrowthStageCalculator(_reloj));

    public void Dispose()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    private static EntradaDiario Completa(DateOnly dia) => new(
        dia, Animo: 4, Ganas: 2,
        Emociones: new HashSet<Emocion> { Emocion.Calma, Emocion.Ansiedad },
        Detonantes: new HashSet<Detonante> { Detonante.Estres },
        Sintomas: new Dictionary<Sintoma, int> { [Sintoma.MalDormir] = 3, [Sintoma.Irritabilidad] = 1 },
        Nota: "Hoy fue un buen día. ¡Ñandú! 'comillas' y emoji 🌳");

    [Fact]
    public void Sqlite_GuardaYLeeEntradaCompleta_Cifrada()
    {
        var llave = SqliteGardenStore.NuevaLlave();
        var e = Completa(Hoy);
        using (var store = new SqliteGardenStore(_ruta, llave: llave)) store.GuardarEntrada(e);
        using var otra = new SqliteGardenStore(_ruta, llave: llave);
        var leida = Assert.Single(otra.Diario());
        Assert.Equal(e.Dia, leida.Dia);
        Assert.Equal(e.Animo, leida.Animo);
        Assert.Equal(e.Ganas, leida.Ganas);
        Assert.Equal(e.EmocionesO.Order(), leida.EmocionesO.Order());
        Assert.Equal(e.DetonantesO, leida.DetonantesO);
        Assert.Equal(e.SintomasO.OrderBy(k => k.Key), leida.SintomasO.OrderBy(k => k.Key));
        Assert.Equal(e.Nota, leida.Nota);
        // Nada del texto queda legible en el archivo.
        Assert.DoesNotContain("buen día", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(_ruta)));
    }

    [Fact]
    public void Sqlite_ReemplazaElMismoDia_YElimina()
    {
        using var store = new SqliteGardenStore(_ruta);
        store.GuardarEntrada(new EntradaDiario(Hoy, Animo: 2));
        store.GuardarEntrada(new EntradaDiario(Hoy, Animo: 5));
        store.GuardarEntrada(new EntradaDiario(Hoy.AddDays(-1), Ganas: 0));
        Assert.Equal(2, store.Diario().Count);
        Assert.Equal(5, store.Diario().Single(x => x.Dia == Hoy).Animo);
        store.EliminarEntrada(Hoy);
        Assert.Equal(Hoy.AddDays(-1), Assert.Single(store.Diario()).Dia);
    }

    [Fact]
    public void Sqlite_BaseV3_MigraAV4SinPerderNada()
    {
        using (var store = new SqliteGardenStore(_ruta)) store.GuardarPerfil(new Perfil(Nombre: "Prueba"));
        using (var c = new SQLite.SQLiteConnection(_ruta))
        {
            c.Execute("DROP TABLE Diario");
            c.Execute("PRAGMA user_version = 3");
        }
        using var migrada = new SqliteGardenStore(_ruta);
        Assert.Equal(4, migrada.VersionEsquema);
        Assert.Equal("Prueba", migrada.Perfil().Nombre);
        Assert.Empty(migrada.Diario());
        migrada.GuardarEntrada(new EntradaDiario(Hoy, Animo: 3));
        Assert.Single(migrada.Diario());
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(6, null)]
    [InlineData(null, -1)]
    [InlineData(null, 5)]
    public void Servicio_RechazaValoresFueraDeEscala(int? animo, int? ganas) =>
        Assert.Throws<ArgumentException>(() => _svc.GuardarEntrada(new EntradaDiario(Hoy, animo, ganas ?? (animo is null ? null : 1))));

    [Fact]
    public void Servicio_RechazaEntradaVaciaYDiaFuturo()
    {
        Assert.Throws<ArgumentException>(() => _svc.GuardarEntrada(new EntradaDiario(Hoy, Nota: "   ",
            Sintomas: new Dictionary<Sintoma, int> { [Sintoma.Ansiedad] = 0 })));
        Assert.Throws<ArgumentException>(() => _svc.GuardarEntrada(new EntradaDiario(Hoy.AddDays(1), Animo: 3)));
        Assert.Throws<ArgumentException>(() => _svc.GuardarEntrada(new EntradaDiario(Hoy, Nota: new string('a', EntradaDiario.LargoMaximoNota + 1))));
    }

    [Fact]
    public void Servicio_LimpiaNotaYSintomasEnCero()
    {
        _svc.GuardarEntrada(new EntradaDiario(Hoy, Animo: 3, Nota: "  ",
            Sintomas: new Dictionary<Sintoma, int> { [Sintoma.Ansiedad] = 0, [Sintoma.Temblor] = 2 }));
        var e = _svc.Entrada(Hoy)!;
        Assert.Null(e.Nota);
        Assert.Equal([Sintoma.Temblor], e.SintomasO.Keys);
    }

    [Theory]
    [InlineData(Sintoma.Autolesion, 1, Alerta.Crisis)]
    [InlineData(Sintoma.Alucinaciones, 1, Alerta.Urgente)]
    [InlineData(Sintoma.Confusion, 2, Alerta.Urgente)]
    [InlineData(Sintoma.Convulsiones, 1, Alerta.Urgente)]
    [InlineData(Sintoma.Temblor, 3, Alerta.MedicoHoy)]
    [InlineData(Sintoma.Sudor, 4, Alerta.MedicoHoy)]
    [InlineData(Sintoma.Temblor, 2, Alerta.Ninguna)]
    [InlineData(Sintoma.Ansiedad, 4, Alerta.Ninguna)]
    public void Servicio_AvisaSenalesDeAlarma(Sintoma s, int v, Alerta esperada) =>
        Assert.Equal(esperada, _svc.GuardarEntrada(new EntradaDiario(Hoy, Sintomas: new Dictionary<Sintoma, int> { [s] = v })));

    [Fact]
    public void Catalogo_SintomasSegunLoPlantado()
    {
        var soloPantallas = CatalogoDiario.Para(new HashSet<MalHabito> { MalHabito.Pantallas }).Select(s => s.Sintoma).ToList();
        Assert.Contains(Sintoma.Ansiedad, soloPantallas);
        Assert.Contains(Sintoma.Autolesion, soloPantallas); // la pregunta de crisis va siempre
        Assert.DoesNotContain(Sintoma.Temblor, soloPantallas);
        Assert.DoesNotContain(Sintoma.Alucinaciones, soloPantallas);

        var alcohol = CatalogoDiario.Para(new HashSet<MalHabito> { MalHabito.Alcohol }).Select(s => s.Sintoma).ToList();
        Assert.Contains(Sintoma.Temblor, alcohol);
        Assert.Contains(Sintoma.Alucinaciones, alcohol);
        Assert.DoesNotContain(Sintoma.NarizLagrimeo, alcohol);
        Assert.Equal(Enum.GetValues<Sintoma>().Length, CatalogoDiario.Sintomas.Count);
        Assert.Equal(Enum.GetValues<Emocion>().Length, CatalogoDiario.Emociones.Count);
        Assert.Equal(Enum.GetValues<Detonante>().Length, CatalogoDiario.Detonantes.Count);
    }

    [Fact]
    public void Resumen_SeriesMapaYFrecuentes()
    {
        var entradas = new List<EntradaDiario>
        {
            new(Hoy, Animo: 4, Ganas: 1, Emociones: new HashSet<Emocion> { Emocion.Calma },
                Detonantes: new HashSet<Detonante> { Detonante.Estres }, Sintomas: new Dictionary<Sintoma, int> { [Sintoma.MalDormir] = 2 }),
            new(Hoy.AddDays(-1), Animo: 2, Ganas: 3, Emociones: new HashSet<Emocion> { Emocion.Calma, Emocion.Ansiedad },
                Detonantes: new HashSet<Detonante> { Detonante.Estres }, Sintomas: new Dictionary<Sintoma, int> { [Sintoma.MalDormir] = 4, [Sintoma.Ansiedad] = 1 }),
            new(Hoy.AddDays(-9), Ganas: 4),
            new(Hoy.AddDays(-40), Animo: 1), // fuera del rango
        };
        var r = ResumenDiario.Calcular(entradas, Hoy, 14);
        Assert.Equal(14, r.Animo.Count);
        Assert.Equal(Hoy.AddDays(-13), r.Desde);
        Assert.Equal(3, r.DiasEscritos);
        Assert.Equal(2, r.RachaEscritura);
        Assert.Equal(4, r.Animo[^1].Valor);
        Assert.Null(r.Animo[^3].Valor);
        Assert.False(r.PorSemana);
        Assert.Equal(14, r.Columnas.Count);
        Assert.Equal(Sintoma.MalDormir, r.Mapa[0].Sintoma);   // más intenso primero
        Assert.Equal(4, r.Mapa[0].Valores[^2]);
        Assert.Null(r.Mapa[0].Valores[0]);                     // día sin registro ≠ 0
        Assert.Equal(0, r.Mapa[1].Valores[^1]);                // registrado sin ese síntoma = 0
        Assert.Equal(2, r.GanasSemana);
        Assert.Equal(4, r.GanasSemanaAnterior);
        Assert.Equal((Emocion.Calma, 2), r.Emociones[0]);
        Assert.Equal((Detonante.Estres, 2), r.Detonantes[0]);
    }

    [Fact]
    public void Resumen_TresMesesVaPorSemana_YRachaSinHoy()
    {
        var entradas = new List<EntradaDiario> { new(Hoy.AddDays(-1), Animo: 3), new(Hoy.AddDays(-2), Animo: 3),
            new(Hoy.AddDays(-3), Sintomas: new Dictionary<Sintoma, int> { [Sintoma.Ansiedad] = 2 }) };
        var r = ResumenDiario.Calcular(entradas, Hoy, 90);
        Assert.True(r.PorSemana);
        Assert.Equal(13, r.Columnas.Count);
        Assert.Equal(90, r.Ganas.Count);
        Assert.Equal(3, r.RachaEscritura); // hoy aún no se escribe: la racha sigue viva
    }

    [Fact]
    public void Logro_CuadernoDeRaicesALos7Dias_YNoSePierdeAlBorrar()
    {
        for (int i = 6; i >= 0; i--) _svc.GuardarEntrada(new EntradaDiario(Hoy.AddDays(-i), Animo: 3));
        Assert.Contains(_memoria.NivelesLogro(), n => n.Id == "gen.blog" && n.Nivel == 1);
        _svc.EliminarEntrada(Hoy);
        _svc.GuardarEntrada(new EntradaDiario(Hoy, Animo: 4));
        Assert.Single(_memoria.NivelesLogro(), n => n.Id == "gen.blog");
        Assert.Equal(Hoy, _svc.Diario()[0].Dia); // la más nueva primero
    }
}
