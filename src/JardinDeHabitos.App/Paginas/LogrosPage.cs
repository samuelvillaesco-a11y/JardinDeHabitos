using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// El estante de trofeos (se abre con el bloqueo del teléfono). Aquí, y solo aquí, se ve a qué hábito
/// está dedicado cada árbol. Los ocultos, malditos y legendarios aparecen hasta que se ganan.
/// </summary>
public sealed class LogrosPage : ContentPage
{
    private static readonly Color Oro = Color.FromArgb("#E9C46A");
    private static readonly Color Maldito = Color.FromArgb("#5A3B5C");

    public LogrosPage()
    {
        Title = "Tu estante";
        BackgroundColor = Tema.Musgo;

        var estante = Servicios.Jardin.Estante();
        var lista = new VerticalStackLayout { Padding = new Thickness(18, 20), Spacing = 12 };

        lista.Add(Tema.Titulo("🏆 Tu estante", 30));
        int ganados = estante.Count(e => e.Nivel > 0);
        lista.Add(Tema.Texto(ganados == 0
            ? "Aquí irán tus trofeos. Cada día que cuidas tu jardín cuenta."
            : "Cada trofeo es un paso real. Míralos cuando necesites recordar hasta dónde llegaste.", 15, color: Tema.HuesoTenue));

        // Lo descubierto (ocultos, malditos, legendarios) va primero: es lo más especial.
        var especiales = estante.Where(e => e.Def.Categoria is CategoriaLogro.Legendario or CategoriaLogro.Maldito or CategoriaLogro.Oculto)
            .OrderBy(e => e.Def.Categoria switch { CategoriaLogro.Legendario => 0, CategoriaLogro.Maldito => 1, _ => 2 })
            .ToList();
        if (especiales.Count > 0)
        {
            lista.Add(Seccion("Descubiertos"));
            foreach (var e in especiales) lista.Add(Tarjeta(e));
        }

        foreach (var grupo in estante.Where(e => e.Def.Categoria == CategoriaLogro.Habito).GroupBy(e => e.Def.Habito!.Value))
        {
            var nombre = CatalogoHabitos.De(grupo.Key).Nombre;
            var arbol = grupo.Select(e => e.Arbol).FirstOrDefault(a => a is not null);
            lista.Add(Seccion(arbol is null ? nombre : $"{arbol} · {nombre}"));
            foreach (var e in Ordenar(grupo)) lista.Add(Tarjeta(e));
        }

        lista.Add(Seccion("Tu jardín"));
        foreach (var e in Ordenar(estante.Where(e => e.Def.Categoria == CategoriaLogro.General))) lista.Add(Tarjeta(e));

        Content = new ScrollView { Content = lista };
    }

    /// <summary>Los ganados primero; luego los que están más cerca de su siguiente estrella.</summary>
    private static IEnumerable<EntradaEstante> Ordenar(IEnumerable<EntradaEstante> entradas) =>
        entradas.OrderByDescending(e => e.Nivel > 0).ThenByDescending(e => Avance(e));

    private static double Avance(EntradaEstante e) =>
        e.Nivel >= e.Def.Niveles.Length ? 1 : Math.Min(1, e.Progreso / (double)e.Def.Niveles[e.Nivel]);

    private static Label Seccion(string texto) =>
        Tema.Texto(texto, 19, Tema.FuenteTitulo, Tema.Hoja);

    private static View Tarjeta(EntradaEstante e)
    {
        var def = e.Def;
        bool ganado = e.Nivel > 0;
        bool completo = e.Nivel >= def.Niveles.Length;
        var (fondo, titulo, texto) = def.Categoria switch
        {
            CategoriaLogro.Legendario => (Oro, Tema.Musgo, Tema.Musgo),
            CategoriaLogro.Maldito => (Maldito, Tema.Hueso, Tema.Hueso),
            _ => (Tema.MusgoClaro, ganado ? Tema.Miel : Tema.HuesoTenue, Tema.Hueso),
        };

        var pila = new VerticalStackLayout { Spacing = 4 };
        pila.Add(Tema.Texto(def.Titulo, 19, Tema.FuenteTitulo, titulo));
        if (def.Niveles.Length > 1)
            pila.Add(Tema.Texto(Estrellas(e.Nivel, def.Niveles.Length), 18, Tema.FuenteNegrita, ganado ? Tema.Miel : Tema.HuesoTenue));

        // Lo ganado se describe en su nivel; lo pendiente, en la meta siguiente.
        pila.Add(Tema.Texto(def.TextoNivel(ganado ? e.Nivel : 1), 15, color: texto));
        if (!completo && def.Niveles.Length > 1)
        {
            int meta = def.Niveles[e.Nivel];
            pila.Add(Tema.Texto(ganado ? $"Siguiente estrella: {def.TextoNivel(e.Nivel + 1)} ({Math.Min(e.Progreso, meta)} de {meta})"
                                       : $"Llevas {Math.Min(e.Progreso, meta)} de {meta}.", 13, color: Tema.HuesoTenue));
        }
        if (def.Categoria == CategoriaLogro.Maldito && def.Debuff is { } d)
            pila.Add(Tema.Texto($"Efecto: {Efecto(d)}", 13, Tema.FuenteNegrita, Tema.Miel));
        if (def.Nota is { } nota && (ganado || def.Categoria == CategoriaLogro.Habito))
            pila.Add(Tema.Texto(nota, 13, color: def.Categoria == CategoriaLogro.Legendario ? Tema.Corteza : Tema.HuesoTenue));
        if (e.Fecha is { } f)
            pila.Add(Tema.Texto($"Conseguido el {Tema.FechaCorta(f)}", 12, color: def.Categoria == CategoriaLogro.Legendario ? Tema.Corteza : Tema.HuesoTenue));

        var tarjeta = Tema.Parcela(pila, fondo);
        if (!ganado) tarjeta.Opacity = 0.75;
        SemanticProperties.SetDescription(tarjeta, $"{def.Titulo}. {(ganado ? $"{e.Nivel} de {def.Niveles.Length} estrellas" : "Aún no conseguido")}.");
        return tarjeta;
    }

    private static string Estrellas(int nivel, int total) =>
        new string('★', nivel) + new string('☆', total - nivel);

    private static string Efecto(Debuff d) => d switch
    {
        Debuff.MasBichos => "las plagas traen más bichos.",
        Debuff.TierraSeca => "la tierra se seca más rápido; regar toma más tiempo.",
        Debuff.MasRamas => "crecen más ramas sueltas al podar.",
        _ => "las raíces podridas son más tercas.",
    };
}
