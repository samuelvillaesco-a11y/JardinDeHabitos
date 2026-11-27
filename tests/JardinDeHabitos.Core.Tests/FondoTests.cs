using JardinDeHabitos.Core;
using ColorJ = JardinDeHabitos.Core.ColorHex;

namespace JardinDeHabitos.Core.Tests;

public class FondoTests
{
    [Fact]
    public void Escenas_LlevanVeloMinimo_PorSusDetalles()
    {
        foreach (var e in Enum.GetValues<EscenaFondo>())
            Assert.True(Fondo.DeEscena(e).Velo >= Fondo.VeloMinimoEscena, e.ToString());
    }

    [Fact]
    public void PorDefecto_EsPapel_YNoNecesitaVelo()
    {
        Assert.Equal(TipoFondo.Solido, Fondo.PorDefecto.Tipo);
        Assert.Equal(Paleta.Papel, Fondo.PorDefecto.ColorA);
        Assert.Equal(0, Fondo.PorDefecto.Velo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Solido|#XYZ123|#000000|Bosque")]
    [InlineData("Arcoiris|#000000|#000000|Bosque")]
    [InlineData("EscenaFondo|#000000|#000000|Luna")]
    public void Leer_DatosDanados_VuelveAlPorDefecto(string? texto) => Assert.Equal(Fondo.PorDefecto, Fondo.Leer(texto));

    [Fact]
    public void Serializar_YLeer_IdaYVuelta()
    {
        var d = Fondo.Degradado("#f4c76e", "#6E7E3A");
        Assert.Equal(d, Fondo.Leer(d.Serializar()));
        Assert.Equal("#F4C76E", d.ColorA);

        var e = Fondo.DeEscena(EscenaFondo.Suburbio);
        Assert.Equal(e, Fondo.Leer(e.Serializar()));
    }

    [Fact]
    public void Contraste_BlancoNegro_Es21() => Assert.Equal(21, Contraste.Razon("#FFFFFF", "#000000"), 1);

    [Fact]
    public void FondoOscuro_UsaLaPaletaDeNoche_SinDeslavarse()
    {
        var f = Fondo.Solido("#1F3A2B"); // bosque profundo
        Assert.True(f.Oscuro);
        Assert.Equal(0, f.Velo);
        Assert.Equal(Paleta.TextoClaro, f.ColorTexto);
        Assert.False(Fondo.PorDefecto.Oscuro);
    }

    [Fact]
    public void TodosLosPredefinidos_SonLegiblesConSuPaletaYSuVelo()
    {
        var fondos = Paleta.Solidos.Select(s => Fondo.Solido(s.Color))
            .Concat(Paleta.Degradados.Select(d => Fondo.Degradado(d.A, d.B)))
            .Concat(Enum.GetValues<EscenaFondo>().Select(Fondo.DeEscena));
        foreach (var f in fondos)
            foreach (var c in new[] { f.ColorA, f.ColorB })
                Assert.True(Contraste.Razon(f.ColorTexto, ColorJ.Mezclar(c, f.ColorVelo, f.Velo)) >= Contraste.MinimoTexto, f.ToString());
    }

    [Theory]
    [InlineData("#27331F")]
    [InlineData("#F4C76E")]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    public void Hsv_IdaYVuelta(string hex)
    {
        var (h, s, v) = ColorJ.Hsv(hex);
        Assert.Equal(hex, ColorJ.DesdeHsv(h, s, v));
    }

    [Fact]
    public void ColorInvalido_Lanza() => Assert.Throws<ArgumentException>(() => Fondo.Solido("verde"));
}
