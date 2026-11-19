using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class LogrosTests
{
    private static readonly DateOnly Hoy = new(2026, 1, 1);

    [Fact]
    public void CadaHabitoTieneAlMenosDiezSinContarDiasLibres()
    {
        foreach (var h in Enum.GetValues<MalHabito>())
            Assert.True(CatalogoLogros.Todos.Count(d => d.Habito == h && !d.SoloSiModera) >= 10, h.ToString());
    }

    [Fact]
    public void IdsUnicos() =>
        Assert.Equal(CatalogoLogros.Todos.Count, CatalogoLogros.Todos.Select(d => d.Id).Distinct().Count());

    [Fact]
    public void NivelesAscendentesYDeUnoATres()
    {
        foreach (var d in CatalogoLogros.Todos)
        {
            Assert.InRange(d.Niveles.Length, 1, 3);
            Assert.Equal(d.Niveles.Order(), d.Niveles);
            Assert.Equal(d.Niveles.Length, d.Niveles.Distinct().Count());
        }
    }

    [Fact]
    public void Visibles_SinElegir_SoloGenerales()
    {
        var visibles = CatalogoLogros.Visibles(new Dictionary<string, int>(), []).ToList();

        Assert.NotEmpty(visibles);
        Assert.All(visibles, d => Assert.Equal(CategoriaLogro.General, d.Categoria));
    }

    [Fact]
    public void Visibles_OcultosNoAparecenHastaGanarse()
    {
        var stats = new Dictionary<string, int> { [Stat.Ramas] = 59 };
        Assert.DoesNotContain(CatalogoLogros.Visibles(stats, []), d => d.Id == "oc.ramas");

        var tenidos = new[] { new NivelLogro("oc.ramas", 1, Hoy) };
        Assert.Contains(CatalogoLogros.Visibles(stats, tenidos), d => d.Id == "oc.ramas");
    }

    [Fact]
    public void Evaluador_SubeNivelesEnOrdenYNoRepite()
    {
        var stats = new Dictionary<string, int> { [Stat.Plantados] = 3 };

        var primero = EvaluadorLogros.Nuevos(stats, [], Hoy);
        Assert.Equal([1, 2], primero.Where(n => n.Id == "gen.semilla").Select(n => n.Nivel));

        var segundo = EvaluadorLogros.Nuevos(stats, primero, Hoy);
        Assert.Empty(segundo);

        stats[Stat.Plantados] = 5;
        Assert.Equal(3, Assert.Single(EvaluadorLogros.Nuevos(stats, primero, Hoy)).Nivel);
    }

    [Fact]
    public void Malditos_TienenDebuffYRedencion()
    {
        foreach (var m in CatalogoLogros.Todos.Where(d => d.Categoria == CategoriaLogro.Maldito))
        {
            Assert.NotNull(m.Debuff);
            Assert.Contains(CatalogoLogros.Todos, d => d.Categoria == CategoriaLogro.Legendario && d.Redime == m.Id);
        }
    }

    [Fact]
    public void Ocultos_NoSeGananEnLaPrimeraSemana()
    {
        // Con una semana de cuidado diario (1 riego, 1 poda de 3 ramas por día) no cae ninguno.
        var semana = new Dictionary<string, int>
        {
            [Stat.Ramas] = 21, [Stat.RiegosNocturnos] = 7, [Stat.RiegosMadrugada] = 7,
            [Stat.JardinCompleto] = 7, [Stat.CuidadoCompleto] = 7, [Stat.RachaRiego] = 7,
        };
        Assert.DoesNotContain(EvaluadorLogros.Nuevos(semana, [], Hoy), n => n.Id.StartsWith("oc."));
    }

    [Fact]
    public void TiemposDeAlcohol_SiguenNIAAA()
    {
        var t = CatalogoLogros.Todos.Where(d => d.Id.StartsWith("hab.alcohol.tiempo")).SelectMany(d => d.Niveles).ToList();
        Assert.Contains(90, t);   // fin de la etapa inicial
        Assert.Contains(365, t);  // fin de la temprana
        Assert.Contains(1825, t); // estable
    }

    [Fact]
    public void TextoNivel_ReemplazaMetaYHabito()
    {
        var d = CatalogoLogros.De("hab.cigarro.tiempo1");
        Assert.Equal("7 días cumpliendo tu meta con el cigarro y el vapeo.", d.TextoNivel(2));
    }
}

public class EstanteRangoTests
{
    private static EntradaEstante E(string id, int nivel) => new(CatalogoLogros.De(id), nivel, null, 0, null);

    [Theory]
    [InlineData("gen.semilla", 0, Rango.SinGanar)]
    [InlineData("gen.semilla", 1, Rango.Bronce)]
    [InlineData("gen.semilla", 2, Rango.Plata)]
    [InlineData("gen.semilla", 3, Rango.Oro)]
    [InlineData("gen.honesto", 1, Rango.Oro)]
    [InlineData("mal.ceniza", 0, Rango.SinGanar)]
    [InlineData("mal.ceniza", 1, Rango.Maldito)]
    [InlineData("leg.fenix", 1, Rango.Legendario)]
    public void Rango_SaleDeLasEstrellas(string id, int nivel, Rango esperado) =>
        Assert.Equal(esperado, E(id, nivel).Rango);
}
