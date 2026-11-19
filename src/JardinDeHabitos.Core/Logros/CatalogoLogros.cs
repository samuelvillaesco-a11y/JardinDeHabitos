namespace JardinDeHabitos.Core;

/// <summary>
/// Todos los logros. Los tiempos salen de la literatura médica citada en
/// docs/superpowers/specs/2026-11-19-jardin-secreto-logros-design.md ("Fundamentos").
/// </summary>
public static class CatalogoLogros
{
    public static LogroDef De(string id) => Todos.Single(d => d.Id == id);

    /// <summary>
    /// Lo que se ve en el estante: generales siempre; los de un hábito solo si se eligió alguna vez
    /// (los de días libres, solo si se moderó); ocultos, malditos y legendarios solo una vez ganados.
    /// </summary>
    public static IEnumerable<LogroDef> Visibles(IReadOnlyDictionary<string, int> stats, IReadOnlyList<NivelLogro> tenidos)
    {
        var ganados = tenidos.Select(t => t.Id).ToHashSet();
        return Todos.Where(d => d.Categoria switch
        {
            CategoriaLogro.General => true,
            CategoriaLogro.Habito => stats.GetValueOrDefault(Stat.De(Stat.Elegido, d.Habito!.Value)) > 0
                                     && (!d.SoloSiModera || stats.GetValueOrDefault(Stat.De(Stat.Moderado, d.Habito!.Value)) > 0),
            _ => ganados.Contains(d.Id),
        });
    }

    private static IEnumerable<LogroDef> Generales()
    {
        const CategoriaLogro G = CategoriaLogro.General;
        yield return new("gen.semilla", G, null, "Manos a la tierra", "Árboles plantados en tu jardín: {n}.", Stat.Plantados, [1, 3, 5],
            "Decidir cambiar es la etapa de \"preparación\": ya es un paso real.");
        yield return new("gen.brote", G, null, "Del brote al fruto", "Un árbol llegó a {n} días de crecimiento.", Stat.RachaMax, [7, 30, 180]);
        yield return new("gen.legendaria", G, null, "Fruta Legendaria", "Un árbol llegó a {n} días de crecimiento.", Stat.RachaMax, [365, 730, 1095],
            "Un año es el umbral de la remisión sostenida.");
        yield return new("gen.bitacora", G, null, "Diario de campo", "Hiciste check-in en {n} días distintos.", Stat.DiasConCheckIn, [7, 30, 100]);
        yield return new("gen.constancia", G, null, "Sol de cada mañana", "{n} días seguidos visitando tu jardín.", Stat.SeguidosConCheckIn, [7, 30, 90]);
        yield return new("gen.riego", G, null, "Regadera fiel", "Regaste tu jardín en {n} días distintos.", Stat.DiasRegados, [7, 30, 90]);
        yield return new("gen.poda", G, null, "Tijeras con cariño", "Podaste en {n} días distintos.", Stat.DiasPodados, [7, 30, 90]);
        yield return new("gen.primavera", G, null, "Primavera", "{n} árboles en flor (7+ días) al mismo tiempo.", Stat.EnFlorALaVez, [2, 3, 5]);
        yield return new("gen.huerto", G, null, "Huerto en marcha", "{n} árboles con 30+ días de crecimiento al mismo tiempo.", Stat.TreintaALaVez, [2, 3, 5]);
        yield return new("gen.honesto", G, null, "La verdad también abona",
            "Marcaste \"Lo rompí\" con honestidad. Decir la verdad es parte de crecer.", Stat.Honestos, [1],
            "La culpa tras un desliz aumenta el riesgo de recaída; la honestidad sin castigo ayuda a seguir.");
        yield return new("gen.cura", G, null, "Mano que sana", "Cuidaste un árbol enfermo hasta que sanó.", Stat.CuidadosTotales, [1]);
        yield return new("gen.apoyo", G, null, "Raíces compartidas", "Asistencias registradas a tu grupo de apoyo o terapia: {n}.", Stat.AsistenciasTotales, [1, 10, 50],
            "Pedir ayuda no es debilidad: el apoyo profesional y de grupo mejora la recuperación.");
        yield return new("gen.replantar", G, null, "Volver a plantar", "Tras perder un árbol, su sucesor llegó a 7 días.", Stat.Replantes, [1],
            "La mayoría logra el cambio después de varios intentos. Volver a empezar es parte del camino.");
    }

    /// <summary>Metas de tiempo por hábito (días de crecimiento), con su fundamento.</summary>
    private static readonly (MalHabito H, (string Titulo, int[] Niveles, string Nota)[] Tiempos)[] TiemposPorHabito =
    [
        (MalHabito.Alcohol,
        [
            ("Brindis con agua de riego", [3, 14, 30], "La abstinencia aguda suele pasar en la primera semana; las convulsiones y el delirium aparecen entre las 12 y 72 h."),
            ("Raíces sobrias", [90, 180, 365], "De 3 meses a 1 año es la recuperación temprana (NIAAA)."),
            ("Cosecha sin resaca", [730, 1095, 1825], "De 1 a 5 años es recuperación sostenida; más de 5, estable."),
        ]),
        (MalHabito.Apuestas,
        [
            ("La suerte se siembra", [7, 14, 30], "Las primeras semanas son de impulso alto: cada día cuenta."),
            ("Sin jugarse la cosecha", [60, 90, 180], "Tres meses sin apostar marcan la remisión temprana (DSM-5)."),
            ("Apuesta segura: la raíz", [270, 365, 510], "La recaída típica llega cerca de 1.4 años después; pasar ese punto es enorme."),
            ("Fortuna de roble", [730, 1095, 1825], "Años sin apostar: tu jardín ya es un bosque."),
        ]),
        (MalHabito.Azucares,
        [
            ("Menos miel, más raíz", [14, 30, 66], "Un hábito nuevo tarda en promedio 66 días en volverse automático."),
            ("Fruta en su punto", [90, 180, 254], "Hay personas que tardan hasta 254 días: no hay prisa."),
            ("La dulzura de la fruta", [365, 545, 730], "El gusto por lo dulce se ajusta mejor reduciendo poco a poco."),
        ]),
        (MalHabito.Cafeina,
        [
            ("Despertar sin grano", [3, 9, 30], "La abstinencia de cafeína dura de 2 a 9 días; el pico es entre las 20 y 51 h."),
            ("Savia propia", [66, 120, 180], "A los 66 días, en promedio, el hábito nuevo ya es automático."),
            ("Amanecer natural", [365, 545, 730], "Tu energía ya brota sola."),
        ]),
        (MalHabito.Cigarro,
        [
            ("Aire para las hojas", [3, 7, 28], "Los síntomas pican en los primeros 3 días, la 1.ª semana es la de más riesgo y la mayoría pasa en 3 a 4 semanas."),
            ("Sin humo en el huerto", [90, 180, 365], "Muchos lo logran tras 3 o 4 intentos: este es el tuyo."),
            ("Pulmones de bosque", [730, 1095, 1825], "Años sin humo: tu jardín respira contigo."),
        ]),
        (MalHabito.Compras,
        [
            ("Cartera en flor", [14, 30, 66], "Un hábito nuevo tarda en promedio 66 días en volverse automático."),
            ("Ahorro de temporada", [90, 180, 254], "Tres meses de control son una base sólida."),
            ("Semillas que valen oro", [365, 545, 730], "Lo que no gastaste también creció."),
        ]),
        (MalHabito.Harinas,
        [
            ("Grano entero", [14, 30, 66], "Un hábito nuevo tarda en promedio 66 días en volverse automático."),
            ("Masa madre de voluntad", [90, 180, 254], "Hay personas que tardan hasta 254 días: no hay prisa."),
            ("Cosecha integral", [365, 545, 730], "Un año entero cuidando lo que comes."),
        ]),
        (MalHabito.Pornografia,
        [
            ("Mirada al jardín", [7, 14, 30], "La terapia cognitivo-conductual es lo que más respaldo tiene: no lo hagas solo si pesa."),
            ("Tierra que se reconecta", [60, 90, 180], "Tres meses marcan una base estable de cambio."),
            ("Raíces presentes", [270, 365, 545], "Un año de decisiones conscientes."),
            ("Viento entre las hojas", [730, 1095, 1825], "Años de libertad: tu bosque es tuyo."),
        ]),
        (MalHabito.Pantallas,
        [
            ("Desconectar para echar raíz", [14, 30, 66], "Un hábito nuevo tarda en promedio 66 días en volverse automático."),
            ("Más hojas, menos scroll", [90, 180, 254], "Tres meses recuperando tu tiempo."),
            ("Tiempo de jardín", [365, 545, 730], "Un año mirando más allá de la pantalla."),
        ]),
        (MalHabito.Sal,
        [
            ("Paladar en barbecho", [28, 56, 84], "El gusto por la sal tarda de 8 a 12 semanas en adaptarse."),
            ("Tierra sin salitre", [120, 180, 254], "Tu paladar ya cambió: lo de antes ahora sabe muy salado."),
            ("Sabor de huerta", [365, 545, 730], "Un año cuidando tu corazón."),
        ]),
        (MalHabito.Sustancias,
        [
            ("Tierra limpia", [3, 7, 14], "La primera etapa es la más dura. Si hay abstinencia física, que te acompañe un médico."),
            ("Raíces que respiran", [30, 60, 90], "A los 3 meses empieza la remisión temprana (DSM-5)."),
            ("Remisión en flor", [180, 270, 365], "Algunos síntomas pueden durar meses: es normal y pasa."),
            ("Bosque que perdura", [730, 1095, 1825], "De 1 a 5 años es remisión sostenida; más de 5, estable."),
        ]),
        (MalHabito.Otros,
        [
            ("Primeros brotes", [14, 30, 66], "Un hábito nuevo tarda en promedio 66 días en volverse automático."),
            ("Tallo firme", [90, 180, 254], "Hay personas que tardan hasta 254 días: no hay prisa."),
            ("Árbol de pie", [365, 545, 730], "Un año sosteniendo tu decisión."),
        ]),
    ];

    private static IEnumerable<LogroDef> PorHabito()
    {
        const CategoriaLogro C = CategoriaLogro.Habito;
        foreach (var (h, tiempos) in TiemposPorHabito)
        {
            string p = $"hab.{h.ToString().ToLowerInvariant()}";
            for (int i = 0; i < tiempos.Length; i++)
            {
                var (titulo, niveles, nota) = tiempos[i];
                yield return new($"{p}.tiempo{i + 1}", C, h, titulo, "{n} días cumpliendo tu meta con {h}.", Stat.De(Stat.Racha, h), niveles, nota);
            }
            yield return new($"{p}.constancia", C, h, "Jardinero de cada día", "{n} días seguidos registrando tu avance con {h}, sin faltar uno.", Stat.De(Stat.Constancia, h), [7, 30, 90]);
            yield return new($"{p}.bitacora", C, h, "Bitácora de campo", "{n} check-ins honestos sobre {h}.", Stat.De(Stat.CheckIns, h), [30, 100, 365]);
            yield return new($"{p}.replantar", C, h, "Volver a sembrar", "Después de perder el árbol dedicado a {h}, el nuevo llegó a {n} días.", Stat.De(Stat.TrasMuerte, h), [7, 30, 90],
                "Recaer no borra lo aprendido.");
            yield return new($"{p}.cura", C, h, "Mano que cura", "Tuviste un tropiezo con {h}, lo dijiste y cuidaste tu árbol hasta sanarlo.", Stat.De(Stat.Cuidados, h), [1]);
            yield return new($"{p}.apoyo", C, h, "No se crece solo", "Fuiste {n} veces a tu grupo o a terapia por {h}.", Stat.De(Stat.Asistencias, h), [4, 12, 52]);
            yield return new($"{p}.borron", C, h, "Tierra nueva", "90 días sin strikes con {h}: te ganaste el borrón y cuenta nueva.", Stat.De(Stat.Borron, h), [1],
                "Tres meses sin recaer es la remisión temprana del DSM-5.");
            yield return new($"{p}.findesemana", C, h, "Fin de semana en flor", "{n} fines de semana completos cumpliendo con {h}.", Stat.De(Stat.FinesDeSemana, h), [4, 12, 26],
                "Los fines de semana suelen ser de más riesgo.");
            if (CatalogoHabitos.De(h).PermiteModerar)
            {
                yield return new($"{p}.libres", C, h, "Disfrutar sin desbordar", "{n} días libres con {h} vividos sin perder el rumbo.", Stat.De(Stat.Libres, h), [8, 26, 104],
                    SoloSiModera: true);
            }
        }
    }

    private static IEnumerable<LogroDef> Ocultos()
    {
        const CategoriaLogro O = CategoriaLogro.Oculto;
        yield return new("oc.ramas", O, null, "Tijera de plata", "Cortaste {n} ramas sueltas y hierbas.", Stat.Ramas, [60, 135, 270]);
        yield return new("oc.bichos", O, null, "Exterminador de bolsillo", "Quitaste {n} bichos de tus árboles.", Stat.Bichos, [20, 50, 100]);
        yield return new("oc.raices", O, null, "Arqueólogo de raíces", "Sacaste {n} raíces podridas de la tierra.", Stat.Raices, [9, 24, 45]);
        yield return new("oc.rescate", O, null, "Rescate a tiempo", "Emergencias del jardín resueltas: {n}.", Stat.MiniEventos, [1, 3, 6]);
        yield return new("oc.nocturno", O, null, "Riego de luna", "Regaste de noche (después de las 9) en {n} días.", Stat.RiegosNocturnos, [10, 25, 50]);
        yield return new("oc.madrugada", O, null, "Rocío de madrugada", "Regaste antes de las 7 de la mañana en {n} días.", Stat.RiegosMadrugada, [10, 25, 50]);
        yield return new("oc.completo", O, null, "Ni una hoja seca", "Regaste todos tus árboles el mismo día en {n} días.", Stat.JardinCompleto, [21, 45, 90]);
        yield return new("oc.mimo", O, null, "Mimo completo", "Regaste y podaste el mismo árbol el mismo día en {n} días.", Stat.CuidadoCompleto, [21, 45, 90]);
        yield return new("oc.rachariego", O, null, "La regadera no descansa", "Regaste {n} días seguidos.", Stat.RachaRiego, [21, 45, 90]);
    }

    private static IEnumerable<LogroDef> Malditos()
    {
        const CategoriaLogro M = CategoriaLogro.Maldito;
        yield return new("mal.leyenda_caida", M, null, "Leyenda Caída",
            "Murió un árbol que ya era legendario. Ahora las plagas llegan con más bichos.", Stat.LeyendaCaida, [1],
            "Una caída después de mucho tiempo no borra el año que viviste. Se sana volviendo a crecer.", Debuff: Debuff.MasBichos);
        yield return new("mal.tierra_baldia", M, null, "Tierra Baldía",
            "Murieron 3 árboles en menos de 14 días. Ahora la tierra se seca más rápido y regar cuesta más.", Stat.TierraBaldia, [1],
            "Las crisis pasan. Tu jardín puede volver a llenarse.", Debuff: Debuff.TierraSeca);
        yield return new("mal.jardin_olvidado", M, null, "Jardín Olvidado",
            "Un árbol de 6 meses o más murió por abandono. Ahora crecen más ramas sueltas.", Stat.JardinOlvidado, [1],
            "Volver siempre es posible.", Debuff: Debuff.MasRamas);
        yield return new("mal.ceniza", M, null, "Raíces de Ceniza",
            "Un mismo hábito perdió 3 árboles en menos de 180 días. Ahora las raíces podridas son más tercas.", Stat.Ceniza, [1],
            "Muchas personas necesitan varios intentos. Considera pedir ayuda profesional si este hábito pesa mucho.", Debuff: Debuff.RaicesTercas);
    }

    private static IEnumerable<LogroDef> Legendarios()
    {
        const CategoriaLogro L = CategoriaLogro.Legendario;
        yield return new("leg.fenix", L, null, "Fruta del Fénix",
            "Tras un año limpio tuviste tu primer tropiezo, y en vez de rendirte cuidaste tu árbol hasta que volvió a crecer.", Stat.Fenix, [1]);
        yield return new("leg.corona", L, null, "Corona de raíces",
            "Después de una Leyenda Caída, otro árbol volvió a ser legendario. La maldición se rompió.", Stat.Corona, [1], Redime: "mal.leyenda_caida");
        yield return new("leg.bosque", L, null, "El bosque que vuelve",
            "Después de la Tierra Baldía, 3 árboles llegaron a 90 días al mismo tiempo.", Stat.BosqueQueVuelve, [1], Redime: "mal.tierra_baldia");
        yield return new("leg.regreso", L, null, "El jardinero que regresa",
            "Después del Jardín Olvidado, un árbol llegó a 6 meses sin un solo strike por abandono.", Stat.Regreso, [1], Redime: "mal.jardin_olvidado");
        yield return new("leg.fenix_ceniza", L, null, "De las cenizas, flor",
            "El hábito de las Raíces de Ceniza volvió a sostener un árbol por 6 meses.", Stat.FenixDeCeniza, [1], Redime: "mal.ceniza");
        yield return new("leg.huerto_eterno", L, null, "Huerto eterno", "Tres árboles legendarios al mismo tiempo.", Stat.HuertoEterno, [1]);
        yield return new("leg.cinco_inviernos", L, null, "Cinco inviernos",
            "Un árbol llegó a 5 años. La recuperación ya es estable.", Stat.CincoInviernos, [1]);
    }

    // Al final: los inicializadores estáticos corren en orden y este usa TiemposPorHabito.
    public static IReadOnlyList<LogroDef> Todos { get; } = [.. Generales(), .. PorHabito(), .. Ocultos(), .. Malditos(), .. Legendarios()];
}
