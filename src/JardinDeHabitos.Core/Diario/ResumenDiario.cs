namespace JardinDeHabitos.Core;

/// <summary>Un valor por día (null si ese día no se registró).</summary>
public sealed record PuntoDia(DateOnly Dia, double? Valor);

/// <summary>Una fila del mapa de síntomas: intensidad 0–4 por columna (día o semana), null sin registro.</summary>
public sealed record FilaMapa(Sintoma Sintoma, string Nombre, IReadOnlyList<double?> Valores);

/// <summary>Lo que se grafica en el blog para un rango de días.</summary>
public sealed record ResumenDiario(
    DateOnly Desde,
    DateOnly Hasta,
    int DiasEscritos,
    int RachaEscritura,
    IReadOnlyList<PuntoDia> Animo,
    IReadOnlyList<PuntoDia> Ganas,
    IReadOnlyList<DateOnly> Columnas,
    bool PorSemana,
    IReadOnlyList<FilaMapa> Mapa,
    double? GanasSemana,
    double? GanasSemanaAnterior,
    IReadOnlyList<(Emocion Emocion, int Veces)> Emociones,
    IReadOnlyList<(Detonante Detonante, int Veces)> Detonantes)
{
    public const int MaxFilasMapa = 8;
    public const int MaxFrecuentes = 3;

    /// <summary>Hasta 30 días el mapa va por día; con más, por semana (si no, las celdas no caben).</summary>
    public const int DiasMaxPorDia = 30;

    public static ResumenDiario Calcular(IReadOnlyList<EntradaDiario> entradas, DateOnly hoy, int dias)
    {
        if (dias < 1) throw new ArgumentOutOfRangeException(nameof(dias));
        var desde = hoy.AddDays(-(dias - 1));
        var porDia = entradas.Where(e => e.Dia >= desde && e.Dia <= hoy).ToDictionary(e => e.Dia);
        var todosLosDias = Enumerable.Range(0, dias).Select(i => desde.AddDays(i)).ToList();

        List<PuntoDia> Serie(Func<EntradaDiario, int?> valor) =>
            [.. todosLosDias.Select(d => new PuntoDia(d, porDia.TryGetValue(d, out var e) ? valor(e) : null))];

        bool porSemana = dias > DiasMaxPorDia;
        var columnas = porSemana
            ? Enumerable.Range(0, (dias + 6) / 7).Select(i => desde.AddDays(i * 7)).ToList()
            : todosLosDias;
        IEnumerable<EntradaDiario> EnColumna(DateOnly inicio) =>
            porSemana
                ? porDia.Values.Where(e => e.Dia >= inicio && e.Dia < inicio.AddDays(7))
                : porDia.TryGetValue(inicio, out var e) ? [e] : [];

        var conSintomas = porDia.Values.SelectMany(e => e.SintomasO.Where(kv => kv.Value > 0))
            .GroupBy(kv => kv.Key)
            .Select(g => (Sintoma: g.Key, Total: g.Sum(kv => kv.Value)))
            .OrderByDescending(x => CatalogoDiario.De(x.Sintoma).Alarma)
            .ThenByDescending(x => x.Total)
            .ThenBy(x => x.Sintoma)
            .Take(MaxFilasMapa);
        var mapa = conSintomas.Select(x => new FilaMapa(x.Sintoma, CatalogoDiario.De(x.Sintoma).Nombre,
            [.. columnas.Select(c =>
            {
                var es = EnColumna(c).ToList();
                return es.Count == 0 ? (double?)null : es.Average(e => (double)e.SintomasO.GetValueOrDefault(x.Sintoma));
            })])).ToList();

        double? PromedioGanas(DateOnly a, DateOnly b)
        {
            var v = entradas.Where(e => e.Dia >= a && e.Dia <= b && e.Ganas is not null).Select(e => (double)e.Ganas!.Value).ToList();
            return v.Count == 0 ? null : v.Average();
        }

        int racha = 0;
        var fechas = entradas.Select(e => e.Dia).ToHashSet();
        // La racha sigue viva aunque hoy todavía no se haya escrito.
        for (var d = fechas.Contains(hoy) ? hoy : hoy.AddDays(-1); fechas.Contains(d); d = d.AddDays(-1)) racha++;

        return new ResumenDiario(
            desde, hoy, porDia.Count, racha,
            Serie(e => e.Animo), Serie(e => e.Ganas),
            columnas, porSemana, mapa,
            PromedioGanas(hoy.AddDays(-6), hoy), PromedioGanas(hoy.AddDays(-13), hoy.AddDays(-7)),
            Frecuentes(porDia.Values.SelectMany(e => e.EmocionesO)),
            Frecuentes(porDia.Values.SelectMany(e => e.DetonantesO)));
    }

    private static List<(T, int)> Frecuentes<T>(IEnumerable<T> valores) where T : struct, Enum =>
        [.. valores.GroupBy(v => v).Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2).ThenBy(x => x.Key).Take(MaxFrecuentes)];
}
