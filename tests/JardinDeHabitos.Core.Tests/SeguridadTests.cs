using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class SeguridadTests
{
    // Pocas iteraciones en pruebas para que sean rápidas; la app usa ClaveLocal.Iteraciones.
    private const int ItPrueba = 1000;

    [Fact]
    public void PorDefecto_LogrosYBlogConLlave_GeneralYDobleSin()
    {
        Assert.True(Seguros.PorDefecto(Seguro.Logros));
        Assert.True(Seguros.PorDefecto(Seguro.Blog));
        Assert.False(Seguros.PorDefecto(Seguro.General));
        Assert.False(Seguros.PorDefecto(Seguro.DobleVerificacion));
    }

    [Fact]
    public void Clave_SeVerificaYNoGuardaTexto()
    {
        var h = ClaveLocal.Crear("girasol42", ItPrueba);
        var guardado = h.Serializar();

        Assert.DoesNotContain("girasol42", guardado);
        var leido = HashDeClave.Leer(guardado)!;
        Assert.True(ClaveLocal.Verificar("girasol42", leido));
        Assert.False(ClaveLocal.Verificar("girasol43", leido));
        Assert.False(ClaveLocal.Verificar(null, leido));
    }

    [Fact]
    public void Clave_MismaClaveDistintaSal()
    {
        var a = ClaveLocal.Crear("girasol42", ItPrueba);
        var b = ClaveLocal.Crear("girasol42", ItPrueba);
        Assert.NotEqual(a.Sal, b.Sal);
        Assert.NotEqual(a.Hash, b.Hash);
        Assert.Equal(16, a.Sal.Length);
    }

    [Fact]
    public void Clave_IteracionesDeProduccion_AlMenosOwasp() => Assert.True(ClaveLocal.Iteraciones >= 600_000);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("111111")]
    [InlineData("123456")]
    [InlineData("987654")]
    [InlineData("abcdef")]
    public void Clave_Debil_TieneProblema(string? clave) => Assert.NotNull(ClaveLocal.Problema(clave));

    [Theory]
    [InlineData("482915")]
    [InlineData("mi jardín")]
    public void Clave_Aceptable(string clave) => Assert.Null(ClaveLocal.Problema(clave));

    [Theory]
    [InlineData(null)]
    [InlineData("pbkdf2-sha256$x$a$b")]
    [InlineData("md5$1$a$b")]
    [InlineData("pbkdf2-sha256$10$!!$!!")]
    public void Hash_Corrupto_EsNull(string? texto) => Assert.Null(HashDeClave.Leer(texto));

    [Fact]
    public void Recuperacion_FormatoYAzar()
    {
        var c = CodigoDeRecuperacion.Generar();
        Assert.Matches("^[2-9A-HJKMNP-TV-Z]{4}(-[2-9A-HJKMNP-TV-Z]{4}){3}$", c);
        Assert.NotEqual(c, CodigoDeRecuperacion.Generar());
    }

    [Fact]
    public void Recuperacion_SeNormalizaComoSeEscriba() =>
        Assert.Equal("ABCD2345EFGH6789", CodigoDeRecuperacion.Normalizar(" abcd-2345 efgh-6789 "));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 30)]
    [InlineData(6, 60)]
    [InlineData(7, 120)]
    [InlineData(40, 3600)]
    public void Intentos_EsperaCreciente(int fallidos, int segundos) =>
        Assert.Equal(TimeSpan.FromSeconds(segundos), Intentos.Espera(fallidos));

    [Fact]
    public void Intentos_Restante_RelojAtrasado_NoPasaDeUnaHora()
    {
        var ahora = DateTimeOffset.UnixEpoch.AddDays(10);
        Assert.Equal(TimeSpan.FromHours(1), Intentos.Restante(5, ahora.AddDays(5), ahora));
        Assert.Equal(TimeSpan.Zero, Intentos.Restante(5, ahora.AddMinutes(-1), ahora));
    }
}
