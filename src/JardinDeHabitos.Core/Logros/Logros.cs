namespace JardinDeHabitos.Core;

public enum CategoriaLogro
{
    /// <summary>Visibles desde el inicio: cuidar el jardín, constancia, honestidad, pedir ayuda.</summary>
    General,

    /// <summary>Visibles solo después de plantar ese hábito.</summary>
    Habito,

    /// <summary>De los mini-juegos. No existen para el usuario hasta ganarlos.</summary>
    Oculto,

    /// <summary>Consecuencias raras y graves. Ocultos; traen un debuff hasta su redención.</summary>
    Maldito,

    /// <summary>Hazañas de un año o más y redenciones. Ocultos.</summary>
    Legendario,
}

/// <summary>Efectos de un logro maldito sobre los mini-juegos. Nunca alargan días reales de cuidado.</summary>
public enum Debuff
{
    MasBichos,
    TierraSeca,
    MasRamas,
    RaicesTercas,
}

/// <summary>
/// Un logro. <see cref="Descripcion"/> admite {n} (meta del nivel) y {h} (nombre del hábito).
/// <see cref="Niveles"/> tiene de 1 a 3 metas ascendentes (★, ★★, ★★★).
/// </summary>
public sealed record LogroDef(
    string Id,
    CategoriaLogro Categoria,
    MalHabito? Habito,
    string Titulo,
    string Descripcion,
    string Clave,
    int[] Niveles,
    string? Nota = null,
    Debuff? Debuff = null,
    string? Redime = null,
    bool SoloSiModera = false)
{
    public string TextoNivel(int nivel)
    {
        int meta = Niveles[Math.Clamp(nivel, 1, Niveles.Length) - 1];
        var habito = Habito is { } h ? CatalogoHabitos.De(h).ConArticulo : "";
        return Descripcion.Replace("{n}", meta.ToString()).Replace("{h}", habito);
    }
}

public sealed record NivelLogro(string Id, int Nivel, DateOnly Fecha);

/// <summary>Una fila del estante: el logro, el nivel ganado (0 = aún no) y cuánto lleva.</summary>
public sealed record EntradaEstante(LogroDef Def, int Nivel, DateOnly? Fecha, int Progreso, string? Arbol);

/// <summary>Claves de las estadísticas que alimentan los logros.</summary>
public static class Stat
{
    // Por hábito (sufijo :{MalHabito}).
    public const string Racha = "racha";
    public const string TrasMuerte = "trasMuerte";
    public const string CheckIns = "checkins";
    public const string Constancia = "constancia";
    public const string Cuidados = "cuidados";
    public const string Asistencias = "asistencias";
    public const string Borron = "borron";
    public const string FinesDeSemana = "finesDeSemana";
    public const string Libres = "libres";
    public const string Muertes = "muertes";
    public const string Elegido = "elegido";
    public const string Moderado = "moderado";

    // Generales.
    public const string Plantados = "plantados";
    public const string RachaMax = "rachaMax";
    public const string DiasConCheckIn = "diasConCheckIn";
    public const string SeguidosConCheckIn = "seguidosConCheckIn";
    public const string DiasRegados = "diasRegados";
    public const string DiasPodados = "diasPodados";
    public const string EnFlorALaVez = "enFlorALaVez";
    public const string TreintaALaVez = "treintaALaVez";
    public const string Honestos = "honestos";
    public const string CuidadosTotales = "cuidadosTotales";
    public const string AsistenciasTotales = "asistenciasTotales";
    public const string Replantes = "replantes";

    // Ocultos (mini-juegos).
    public const string Ramas = "ramas";
    public const string Bichos = "bichos";
    public const string Raices = "raices";
    public const string MiniEventos = "miniEventos";
    public const string RiegosNocturnos = "riegosNocturnos";
    public const string RiegosMadrugada = "riegosMadrugada";
    public const string JardinCompleto = "jardinCompleto";
    public const string CuidadoCompleto = "cuidadoCompleto";
    public const string RachaRiego = "rachaRiego";

    // Malditos y legendarios (0/1).
    public const string LeyendaCaida = "leyendaCaida";
    public const string TierraBaldia = "tierraBaldia";
    public const string JardinOlvidado = "jardinOlvidado";
    public const string Ceniza = "ceniza";
    public const string Corona = "corona";
    public const string BosqueQueVuelve = "bosqueQueVuelve";
    public const string Regreso = "regreso";
    public const string FenixDeCeniza = "fenixDeCeniza";
    public const string Fenix = "fenix";
    public const string HuertoEterno = "huertoEterno";
    public const string CincoInviernos = "cincoInviernos";

    public static string De(string clave, MalHabito h) => $"{clave}:{h}";
}

public static class EvaluadorLogros
{
    /// <summary>Niveles nuevos alcanzados, en orden, sin repetir los ya tenidos.</summary>
    public static IReadOnlyList<NivelLogro> Nuevos(
        IReadOnlyDictionary<string, int> stats, IReadOnlyList<NivelLogro> tenidos, DateOnly hoy)
    {
        var maximo = tenidos.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.Max(t => t.Nivel));
        var nuevos = new List<NivelLogro>();
        foreach (var def in CatalogoLogros.Todos)
        {
            int valor = stats.GetValueOrDefault(def.Clave);
            int ya = maximo.GetValueOrDefault(def.Id);
            for (int i = ya; i < def.Niveles.Length && valor >= def.Niveles[i]; i++)
                nuevos.Add(new NivelLogro(def.Id, i + 1, hoy));
        }
        return nuevos;
    }

    /// <summary>Debuffs vigentes: malditos ganados cuya redención aún no se gana.</summary>
    public static IReadOnlySet<Debuff> Debuffs(IReadOnlyList<NivelLogro> tenidos)
    {
        var ids = tenidos.Select(t => t.Id).ToHashSet();
        var redimidos = CatalogoLogros.Todos.Where(d => d.Redime is not null && ids.Contains(d.Id)).Select(d => d.Redime!).ToHashSet();
        return CatalogoLogros.Todos
            .Where(d => d.Categoria == CategoriaLogro.Maldito && d.Debuff is not null && ids.Contains(d.Id) && !redimidos.Contains(d.Id))
            .Select(d => d.Debuff!.Value)
            .ToHashSet();
    }
}
