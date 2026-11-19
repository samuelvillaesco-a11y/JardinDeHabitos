using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class CatalogoTests
{
    [Fact]
    public void Todos_AlfabeticoYOtrosAlFinal()
    {
        var nombres = CatalogoHabitos.Todos.Select(h => h.Nombre).ToList();

        Assert.Equal("Otros", nombres[^1]);
        Assert.Equal("Alcohol", nombres[0]);
        Assert.Equal(Enum.GetValues<MalHabito>().Length, nombres.Count);
        var sinOtros = nombres[..^1];
        Assert.Equal(sinOtros.OrderBy(n => n, StringComparer.Create(new("es-MX"), true)), sinOtros);
    }

    [Theory]
    [InlineData(MalHabito.Apuestas)]
    [InlineData(MalHabito.Pornografia)]
    [InlineData(MalHabito.Sustancias)]
    public void Graves_SoloAbstinencia(MalHabito h)
    {
        var info = CatalogoHabitos.De(h);
        Assert.False(info.PermiteModerar);
        Assert.Equal([HabitType.AbstinenciaTotal], info.Modos);
    }

    [Theory]
    [InlineData(MalHabito.Alcohol)]
    [InlineData(MalHabito.Cigarro)]
    [InlineData(MalHabito.Otros)]
    [InlineData(MalHabito.Sal)]
    public void Otros_PermitenAmbos(MalHabito h)
    {
        var info = CatalogoHabitos.De(h);
        Assert.True(info.PermiteModerar && info.PermiteAbstinencia);
    }

    [Theory]
    [InlineData(MalHabito.Alcohol)]
    [InlineData(MalHabito.Sustancias)]
    public void AlcoholYSustancias_TienenAvisoMedico(MalHabito h) =>
        Assert.Contains("800 911 2000", CatalogoHabitos.De(h).AvisoMedico);

    [Theory]
    [InlineData("cigarros", MalHabito.Cigarro)]
    [InlineData("Azúcar", MalHabito.Azucares)]
    [InlineData("Alcohol", MalHabito.Alcohol)]
    [InlineData("apuestas", MalHabito.Apuestas)]
    [InlineData("sal", MalHabito.Sal)]
    [InlineData("salir tarde", MalHabito.Otros)]
    [InlineData("xyz", MalHabito.Otros)]
    public void DesdeNombreLibre(string nombre, MalHabito esperado) =>
        Assert.Equal(esperado, CatalogoHabitos.DesdeNombreLibre(nombre));

    [Fact]
    public void Especies_SonDiezYUnicas()
    {
        Assert.Equal(10, EspeciesDeArbol.Todas.Count);
        Assert.Equal(10, EspeciesDeArbol.Todas.Distinct().Count());
    }
}
