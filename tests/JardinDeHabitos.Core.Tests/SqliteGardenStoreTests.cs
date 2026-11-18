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
}
