using SQLite;

namespace JardinDeHabitos.Core;

/// <summary>Persistencia local en SQLite. Fechas guardadas como DayNumber.</summary>
public sealed class SqliteGardenStore : IGardenStore, IDisposable
{
    /// <summary>Versión del esquema que entiende este código (PRAGMA user_version).</summary>
    public const int VersionActual = 1;

    private readonly SQLiteConnection _db;

    public SqliteGardenStore(string rutaArchivo)
    {
        _db = new SQLiteConnection(rutaArchivo);
        try
        {
            Migrar();
        }
        catch
        {
            _db.Dispose();
            throw;
        }
    }

    public int VersionEsquema => _db.ExecuteScalar<int>("PRAGMA user_version");

    /// <summary>Lleva la base a <see cref="VersionActual"/>. Cada versión nueva agrega un paso aquí.</summary>
    private void Migrar()
    {
        int version = VersionEsquema;
        if (version > VersionActual)
            throw new InvalidOperationException(
                $"La base de datos es de una versión más nueva de la app ({version}). Actualiza la app.");

        _db.RunInTransaction(() =>
        {
            // 0 → 1: tablas iniciales (las bases sin versión ya las tienen; CreateTable no borra datos).
            _db.CreateTable<HabitoFila>();
            _db.CreateTable<LogroFila>();
            _db.Execute($"PRAGMA user_version = {VersionActual}");
        });
    }

    public void EnTransaccion(Action accion) => _db.RunInTransaction(accion);

    public IReadOnlyList<HabitRecord> Cargar() =>
        [.. _db.Table<HabitoFila>().OrderBy(f => f.Orden).ToList().Select(AHabitRecord)];

    public void Guardar(HabitRecord r) => _db.InsertOrReplace(AFila(r));

    public void Eliminar(Guid habitoId) => _db.Delete<HabitoFila>(habitoId.ToString());

    public IReadOnlyList<LogroDesbloqueado> Logros() =>
        [.. _db.Table<LogroFila>().OrderBy(f => f.Id).ToList()
            .Select(f => new LogroDesbloqueado((Logro)f.Logro, DateOnly.FromDayNumber(f.Fecha), f.Habito))];

    public void AgregarLogro(LogroDesbloqueado l) =>
        _db.Insert(new LogroFila { Logro = (int)l.Logro, Fecha = l.Fecha.DayNumber, Habito = l.Habito });

    public void Dispose() => _db.Dispose();

    private static HabitoFila AFila(HabitRecord r) => new()
    {
        Id = r.Habito.Id.ToString(),
        Nombre = r.Habito.Nombre,
        Especie = r.Habito.EspecieDeArbol,
        Tipo = (int)r.Habito.Tipo,
        InicioRacha = r.Habito.FechaDeInicioRachaActual.DayNumber,
        DiasDeCheat = Mascara(r.Habito.DiasDeCheat),
        DiasDeCheatAnteriores = Mascara(r.Habito.DiasDeCheatAnteriores),
        CambioDeCheatDesde = r.Habito.CambioDeCheatDesde?.DayNumber,
        Racha = r.Estado.RachaConsecutivaDeDiasRotos,
        Cicatrices = r.Estado.StrikesTotalesDelArbolActual,
        Nivel = (int)r.Estado.NivelDePenalizacion,
        FinPenalizacion = r.Estado.FechaFinDePenalizacionActiva?.DayNumber,
        UltimoCheckIn = r.Estado.FechaUltimoCheckIn?.DayNumber,
        UltimoRiego = r.UltimoRiego?.DayNumber,
        UltimaPoda = r.UltimaPoda?.DayNumber,
        UltimoTratamiento = r.UltimoTratamiento?.DayNumber,
        Orden = r.Orden,
    };

    private static HabitRecord AHabitRecord(HabitoFila f) => new()
    {
        Habito = new Habit
        {
            Id = Guid.Parse(f.Id),
            Nombre = f.Nombre,
            EspecieDeArbol = f.Especie,
            Tipo = (HabitType)f.Tipo,
            FechaDeInicioRachaActual = DateOnly.FromDayNumber(f.InicioRacha),
            DiasDeCheat = Dias(f.DiasDeCheat),
            DiasDeCheatAnteriores = Dias(f.DiasDeCheatAnteriores),
            CambioDeCheatDesde = Fecha(f.CambioDeCheatDesde),
        },
        Estado = new StrikeState
        {
            RachaConsecutivaDeDiasRotos = f.Racha,
            StrikesTotalesDelArbolActual = f.Cicatrices,
            NivelDePenalizacion = (PenaltyLevel)f.Nivel,
            FechaFinDePenalizacionActiva = Fecha(f.FinPenalizacion),
            FechaUltimoCheckIn = Fecha(f.UltimoCheckIn),
        },
        UltimoRiego = Fecha(f.UltimoRiego),
        UltimaPoda = Fecha(f.UltimaPoda),
        UltimoTratamiento = Fecha(f.UltimoTratamiento),
        Orden = f.Orden,
    };

    private static int Mascara(IEnumerable<DayOfWeek> dias) => dias.Aggregate(0, (m, d) => m | (1 << (int)d));

    private static List<DayOfWeek> Dias(int mascara) =>
        [.. Enum.GetValues<DayOfWeek>().Where(d => (mascara & (1 << (int)d)) != 0)];

    private static DateOnly? Fecha(int? dayNumber) => dayNumber is { } n ? DateOnly.FromDayNumber(n) : null;

    [Table("Habitos")]
    private sealed class HabitoFila
    {
        [PrimaryKey] public string Id { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Especie { get; set; } = "";
        public int Tipo { get; set; }
        public int InicioRacha { get; set; }
        public int DiasDeCheat { get; set; }
        public int DiasDeCheatAnteriores { get; set; }
        public int? CambioDeCheatDesde { get; set; }
        public int Racha { get; set; }
        public int Cicatrices { get; set; }
        public int Nivel { get; set; }
        public int? FinPenalizacion { get; set; }
        public int? UltimoCheckIn { get; set; }
        public int? UltimoRiego { get; set; }
        public int? UltimaPoda { get; set; }
        public int? UltimoTratamiento { get; set; }
        public int Orden { get; set; }
    }

    [Table("Logros")]
    private sealed class LogroFila
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public int Logro { get; set; }
        public int Fecha { get; set; }
        public string Habito { get; set; } = "";
    }
}
