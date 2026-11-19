using SQLite;

namespace JardinDeHabitos.Core;

/// <summary>Persistencia local en SQLite. Fechas guardadas como DayNumber.</summary>
public sealed class SqliteGardenStore : IGardenStore, IDisposable
{
    /// <summary>Versión del esquema que entiende este código (PRAGMA user_version).</summary>
    public const int VersionActual = 2;

    private readonly SQLiteConnection _db;
    private readonly DateOnly _hoyMigracion;

    /// <param name="hoy">Fecha usada solo para migrar árboles viejos (por defecto, hoy del sistema).</param>
    public SqliteGardenStore(string rutaArchivo, DateOnly? hoy = null)
    {
        _hoyMigracion = hoy ?? DateOnly.FromDateTime(DateTime.Now);
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
            // CreateTable crea la tabla o agrega las columnas que falten, sin borrar datos.
            _db.CreateTable<HabitoFila>();
            _db.CreateTable<EstadisticaFila>();
            _db.CreateTable<NivelFila>();

            // Las bases sin versión ya tienen el esquema 1 (o están vacías).
            if (version < 2 && TablaExiste("Logros")) MigrarA2();

            _db.Execute($"PRAGMA user_version = {VersionActual}");
        });
    }

    private bool TablaExiste(string nombre) =>
        _db.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name=?", nombre) > 0;

    /// <summary>
    /// v1 → v2: hábitos de nombre libre pasan a la lista; el crecimiento conserva los días ya vividos;
    /// las cicatrices pasan a strikes declarados (máx. 2, para no matar un árbol al actualizar);
    /// los logros viejos pasan a niveles del catálogo nuevo.
    /// </summary>
    private void MigrarA2()
    {
        var stats = new Dictionary<string, int>();
        void Max(string k, int v) => stats[k] = Math.Max(stats.GetValueOrDefault(k), v);

        foreach (var f in _db.Table<HabitoFila>().ToList())
        {
            var cat = CatalogoHabitos.DesdeNombreLibre(f.Nombre);
            f.Categoria = (int)cat;
            f.DiasCrecimiento = Math.Max(0, _hoyMigracion.DayNumber - f.InicioRacha);
            f.StrikesDeclarados = Math.Min(2, f.Cicatrices);
            f.UltimoStrike = f.StrikesDeclarados > 0 ? _hoyMigracion.DayNumber : null;
            f.DiasSinStrikeBase = f.StrikesDeclarados > 0 ? f.DiasCrecimiento : 0;
            if (f.FinPenalizacion is { } fin && fin > _hoyMigracion.DayNumber)
            {
                if (f.Nivel == 2) f.FinRaices = fin;
                else f.FinPlaga = fin;
            }
            f.UltimoDiaEvaluado = _hoyMigracion.DayNumber - 1;
            _db.Update(f);

            stats[Stat.De(Stat.Elegido, cat)] = 1;
            if (f.Tipo == (int)HabitType.Moderable) stats[Stat.De(Stat.Moderado, cat)] = 1;
            Max(Stat.De(Stat.Racha, cat), f.DiasCrecimiento);
            Max(Stat.RachaMax, f.DiasCrecimiento);
            stats[$"avisoEtapa:{f.Id}"] = (int)GrowthStageCalculator.EtapaParaDias(f.DiasCrecimiento);
            stats[Stat.Plantados] = stats.GetValueOrDefault(Stat.Plantados) + 1;
        }

        foreach (var l in _db.Query<LogroViejo>("SELECT Logro, Fecha FROM Logros"))
        {
            var (id, clave) = l.Logro == 1 ? ("mal.leyenda_caida", Stat.LeyendaCaida) : ("gen.legendaria", (string?)null);
            if (_db.Table<NivelFila>().Any(n => n.LogroId == id)) continue;
            _db.Insert(new NivelFila { LogroId = id, Nivel = 1, Fecha = l.Fecha });
            if (clave is not null) stats[clave] = 1;
            else Max(Stat.RachaMax, 365);
        }

        GuardarEstadisticas(stats);
    }

    public void EnTransaccion(Action accion)
    {
        // RunInTransaction no se puede anidar: dentro de una transacción solo se ejecuta.
        if (_db.IsInTransaction) accion();
        else _db.RunInTransaction(accion);
    }

    public IReadOnlyList<HabitRecord> Cargar() =>
        [.. _db.Table<HabitoFila>().OrderBy(f => f.Orden).ToList().Select(AHabitRecord)];

    public void Guardar(HabitRecord r) => _db.InsertOrReplace(AFila(r));

    public void Eliminar(Guid habitoId) => _db.Delete<HabitoFila>(habitoId.ToString());

    public IReadOnlyDictionary<string, int> Estadisticas() =>
        _db.Table<EstadisticaFila>().ToList().ToDictionary(f => f.Clave, f => f.Valor);

    public void GuardarEstadisticas(IReadOnlyDictionary<string, int> cambios)
    {
        foreach (var (k, v) in cambios) _db.InsertOrReplace(new EstadisticaFila { Clave = k, Valor = v });
    }

    public IReadOnlyList<NivelLogro> NivelesLogro() =>
        [.. _db.Table<NivelFila>().OrderBy(f => f.Id).ToList().Select(f => new NivelLogro(f.LogroId, f.Nivel, DateOnly.FromDayNumber(f.Fecha)))];

    public void AgregarNivel(NivelLogro n) =>
        _db.Insert(new NivelFila { LogroId = n.Id, Nivel = n.Nivel, Fecha = n.Fecha.DayNumber });

    public void Dispose() => _db.Dispose();

    private static HabitoFila AFila(HabitRecord r)
    {
        var h = r.Habito;
        var e = r.Estado;
        return new()
        {
            Id = h.Id.ToString(),
            Nombre = "",
            Categoria = (int)h.Categoria,
            Especie = h.EspecieDeArbol,
            Tipo = (int)h.Tipo,
            InicioRacha = h.FechaDeInicioRachaActual.DayNumber,
            DiasCrecimiento = h.DiasDeCrecimiento,
            ApoyoGrupo = h.ApoyoGrupo,
            ApoyoTerapia = h.ApoyoTerapia,
            DiasDeCheat = Mascara(h.DiasDeCheat),
            DiasDeCheatAnteriores = Mascara(h.DiasDeCheatAnteriores),
            CambioDeCheatDesde = h.CambioDeCheatDesde?.DayNumber,
            StrikesDeclarados = e.StrikesDeclarados,
            StrikesAbandono = e.StrikesAbandono,
            FinPlaga = e.FinPlaga?.DayNumber,
            FinRaices = e.FinRaices?.DayNumber,
            UltimoCheckIn = e.FechaUltimoCheckIn?.DayNumber,
            UltimoStrike = e.FechaUltimoStrike?.DayNumber,
            DiasSinStrikeBase = e.DiasSinStrikeBase,
            FenixPendiente = e.FenixPendiente,
            Faltas = e.DiasNormalesSinCheckIn,
            UltimoDiaEvaluado = e.UltimoDiaEvaluado.DayNumber,
            MiniEvento = (int)e.MiniEvento,
            UltimoRiego = r.UltimoRiego?.DayNumber,
            UltimaPoda = r.UltimaPoda?.DayNumber,
            UltimoTratamiento = r.UltimoTratamiento?.DayNumber,
            UltimaAsistencia = r.UltimaAsistencia?.DayNumber,
            UltimoCumplido = r.UltimoCumplido?.DayNumber,
            CuidadoPendiente = r.CuidadoPendiente,
            Tratamientos = r.TratamientosEnCuidado,
            ConstanciaActual = r.ConstanciaActual,
            Orden = r.Orden,
        };
    }

    private static HabitRecord AHabitRecord(HabitoFila f) => new()
    {
        Habito = new Habit
        {
            Id = Guid.Parse(f.Id),
            Categoria = (MalHabito)f.Categoria,
            EspecieDeArbol = f.Especie,
            Tipo = (HabitType)f.Tipo,
            FechaDeInicioRachaActual = DateOnly.FromDayNumber(f.InicioRacha),
            DiasDeCrecimiento = f.DiasCrecimiento,
            ApoyoGrupo = f.ApoyoGrupo,
            ApoyoTerapia = f.ApoyoTerapia,
            DiasDeCheat = Dias(f.DiasDeCheat),
            DiasDeCheatAnteriores = Dias(f.DiasDeCheatAnteriores),
            CambioDeCheatDesde = Fecha(f.CambioDeCheatDesde),
        },
        Estado = new StrikeState
        {
            StrikesDeclarados = f.StrikesDeclarados,
            StrikesAbandono = f.StrikesAbandono,
            FinPlaga = Fecha(f.FinPlaga),
            FinRaices = Fecha(f.FinRaices),
            FechaUltimoCheckIn = Fecha(f.UltimoCheckIn),
            FechaUltimoStrike = Fecha(f.UltimoStrike),
            DiasSinStrikeBase = f.DiasSinStrikeBase,
            FenixPendiente = f.FenixPendiente,
            DiasNormalesSinCheckIn = f.Faltas,
            UltimoDiaEvaluado = DateOnly.FromDayNumber(f.UltimoDiaEvaluado),
            MiniEvento = (MiniEvento)f.MiniEvento,
        },
        UltimoRiego = Fecha(f.UltimoRiego),
        UltimaPoda = Fecha(f.UltimaPoda),
        UltimoTratamiento = Fecha(f.UltimoTratamiento),
        UltimaAsistencia = Fecha(f.UltimaAsistencia),
        UltimoCumplido = Fecha(f.UltimoCumplido),
        CuidadoPendiente = f.CuidadoPendiente,
        TratamientosEnCuidado = f.Tratamientos,
        ConstanciaActual = f.ConstanciaActual,
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
        public string Especie { get; set; } = "";
        public int Tipo { get; set; }
        public int InicioRacha { get; set; }
        public int DiasDeCheat { get; set; }
        public int DiasDeCheatAnteriores { get; set; }
        public int? CambioDeCheatDesde { get; set; }
        public int? UltimoCheckIn { get; set; }
        public int? UltimoRiego { get; set; }
        public int? UltimaPoda { get; set; }
        public int? UltimoTratamiento { get; set; }
        public int Orden { get; set; }

        // Solo v1 (se leen al migrar).
        public string Nombre { get; set; } = "";
        public int Racha { get; set; }
        public int Cicatrices { get; set; }
        public int Nivel { get; set; }
        public int? FinPenalizacion { get; set; }

        // v2.
        public int Categoria { get; set; }
        public int DiasCrecimiento { get; set; }
        public bool ApoyoGrupo { get; set; }
        public bool ApoyoTerapia { get; set; }
        public int StrikesDeclarados { get; set; }
        public int StrikesAbandono { get; set; }
        public int? FinPlaga { get; set; }
        public int? FinRaices { get; set; }
        public int? UltimoStrike { get; set; }
        public int DiasSinStrikeBase { get; set; }
        public bool FenixPendiente { get; set; }
        public int Faltas { get; set; }
        public int UltimoDiaEvaluado { get; set; }
        public int MiniEvento { get; set; }
        public int? UltimaAsistencia { get; set; }
        public int? UltimoCumplido { get; set; }
        public bool CuidadoPendiente { get; set; }
        public int Tratamientos { get; set; }
        public int ConstanciaActual { get; set; }
    }

    [Table("Estadisticas")]
    private sealed class EstadisticaFila
    {
        [PrimaryKey] public string Clave { get; set; } = "";
        public int Valor { get; set; }
    }

    [Table("NivelesLogro")]
    private sealed class NivelFila
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string LogroId { get; set; } = "";
        public int Nivel { get; set; }
        public int Fecha { get; set; }
    }

    private sealed class LogroViejo
    {
        public int Logro { get; set; }
        public int Fecha { get; set; }
    }
}
