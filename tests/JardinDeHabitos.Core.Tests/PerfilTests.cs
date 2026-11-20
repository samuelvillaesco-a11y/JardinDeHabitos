using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public sealed class PerfilTests : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"perfil-{Guid.NewGuid():N}.db3");

    public void Dispose()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    [Fact]
    public void PerfilVacio_EsValido() => Assert.Null(new Perfil().Problema());

    [Theory]
    [InlineData("hola@ejemplo.com", null)]
    [InlineData("no-es-correo", "correo")]
    [InlineData("a@b", "correo")]
    public void Correo_SeValida(string correo, string? palabra)
    {
        var p = new Perfil(Correo: correo).Problema();
        if (palabra is null) Assert.Null(p);
        else Assert.Contains(palabra, p);
    }

    [Theory]
    [InlineData(10.0, null)]
    [InlineData(400.0, null)]
    [InlineData(null, 50.0)]
    [InlineData(null, 300.0)]
    public void PesoYAltura_FueraDeRango_TienenProblema(double? peso, double? altura)
    {
        var p = new Perfil(PesoKg: peso ?? 70, AlturaCm: altura ?? 170);
        Assert.NotNull(p.Problema());
    }

    [Fact]
    public void Jardinero_LimpiaElNumero()
    {
        var j = Jardinero.Nuevo("  Ana ", "+52 (81) 1234-5678");
        Assert.Equal("Ana", j.Nombre);
        Assert.Equal("+528112345678", j.Telefono);
    }

    [Theory]
    [InlineData("", "8112345678")]
    [InlineData("Ana", "12")]
    public void Jardinero_Invalido_Lanza(string nombre, string tel) =>
        Assert.Throws<ArgumentException>(() => Jardinero.Nuevo(nombre, tel));

    [Fact]
    public void Jardinero_911_EsValido() => Assert.Equal("911", Jardinero.Nuevo("Emergencias", "911").Telefono);

    [Fact]
    public void Sqlite_PerfilYJardineros_IdaYVuelta_Cifrados()
    {
        var llave = SqliteGardenStore.NuevaLlave();
        var j1 = Jardinero.Nuevo("Ana", "8112345678");
        var j2 = Jardinero.Nuevo("Beto", "8187654321");
        using (var s = new SqliteGardenStore(_ruta, llave: llave))
        {
            s.GuardarPerfil(new Perfil("Sam", "s@ejemplo.com", "8100000000", 72.5, 175, ActividadFisica.Moderada));
            s.GuardarJardinero(j1);
            s.GuardarJardinero(j2);
            s.EliminarJardinero(j1.Id);
        }

        Assert.DoesNotContain("Beto", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(_ruta)));
        using var otra = new SqliteGardenStore(_ruta, llave: llave);
        Assert.Equal(new Perfil("Sam", "s@ejemplo.com", "8100000000", 72.5, 175, ActividadFisica.Moderada), otra.Perfil());
        Assert.Equal([j2], otra.Jardineros());
    }

    [Fact]
    public void Sqlite_BaseV2_SubeAV3_SinPerderNada()
    {
        using (var s = new SqliteGardenStore(_ruta)) s.GuardarEstadisticas(new Dictionary<string, int> { ["x"] = 7 });
        using (var db = new SQLite.SQLiteConnection(_ruta))
        {
            db.Execute("DROP TABLE Perfil");
            db.Execute("DROP TABLE Jardineros");
            db.Execute("PRAGMA user_version = 2");
        }

        using var nueva = new SqliteGardenStore(_ruta);
        Assert.Equal(3, nueva.VersionEsquema);
        Assert.Equal(7, nueva.Estadisticas()["x"]);
        Assert.Equal(new Perfil(), nueva.Perfil());
        Assert.Empty(nueva.Jardineros());
    }
}
