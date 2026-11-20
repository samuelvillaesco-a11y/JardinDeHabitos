using JardinDeHabitos.Core;
using ColorJ = JardinDeHabitos.Core.ColorHex;

namespace JardinDeHabitos.Core.Tests;

public class FondoTests
{
    [Fact]
    public void PorDefecto_EsVerdeMusgo_YNoNecesitaVelo()
    {
        Assert.Equal(TipoFondo.Solido, Fondo.PorDefecto.Tipo);
        Assert.Equal(Paleta.Musgo, Fondo.PorDefecto.ColorA);
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
    public void FondoClaro_LlevaVelo_YConElVeloElTextoSeLee()
    {
        var f = Fondo.Solido("#F3E3B0"); // mantequilla
        Assert.True(f.Velo > 0.5);
        var visto = ColorJ.Mezclar(f.ColorA, Paleta.Musgo, f.Velo);
        Assert.True(Contraste.Razon(Paleta.TextoClaro, visto) >= Contraste.MinimoTexto);
    }

    [Fact]
    public void TodosLosPredefinidos_SonLegiblesConSuVelo()
    {
        var fondos = Paleta.Solidos.Select(s => Fondo.Solido(s.Color))
            .Concat(Paleta.Degradados.Select(d => Fondo.Degradado(d.A, d.B)))
            .Concat(Enum.GetValues<EscenaFondo>().Select(Fondo.DeEscena));
        foreach (var f in fondos)
            foreach (var c in new[] { f.ColorA, f.ColorB })
                Assert.True(Contraste.Razon(Paleta.TextoClaro, ColorJ.Mezclar(c, Paleta.Musgo, f.Velo)) >= Contraste.MinimoTexto, f.ToString());
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
