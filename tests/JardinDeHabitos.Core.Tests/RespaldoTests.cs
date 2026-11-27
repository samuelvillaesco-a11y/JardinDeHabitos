using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public sealed class RespaldoTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("jardin-respaldo-").FullName;
    private string Base => Path.Combine(_dir, "jardin.db3");
    private string Archivo => Path.Combine(_dir, "mi-jardin.jardin");
    private const string Contrasena = "girasol de mayo 7";

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static HabitRecord Registro() => new()
    {
        Habito = new Habit { Categoria = MalHabito.Azucares, EspecieDeArbol = "Naranjo", Tipo = HabitType.Moderable, FechaDeInicioRachaActual = new DateOnly(2026, 1, 1), DiasDeCrecimiento = 42 },
        Estado = new StrikeState(),
    };

    private string ExportarJardin(string llave)
    {
        using var store = new SqliteGardenStore(Base, llave: llave);
        store.Guardar(Registro());
        store.GuardarJardinero(new Jardinero(Guid.NewGuid(), "Ana", "5512345678"));
        store.GuardarEntrada(new EntradaDiario(new DateOnly(2026, 11, 20), Animo: 4));
        Respaldo.Exportar(store, Archivo, Contrasena);
        return llave;
    }

    [Fact]
    public void Exportar_EImportarEnOtroTelefono_ConservaTodo()
    {
        ExportarJardin(SqliteGardenStore.NuevaLlave());
        File.Delete(Base); // otro teléfono: no hay base ni la misma llave
        var llaveNueva = SqliteGardenStore.NuevaLlave();

        string preparada = Respaldo.Preparar(Archivo, Contrasena, Base, llaveNueva);
        string? anterior = Respaldo.Instalar(preparada, Base);

        Assert.Null(anterior);
        using var store = new SqliteGardenStore(Base, llave: llaveNueva);
        Assert.Equal("Naranjo", Assert.Single(store.Cargar()).Habito.EspecieDeArbol);
        Assert.Equal("Ana", Assert.Single(store.Jardineros()).Nombre);
        Assert.Equal(4, Assert.Single(store.Diario()).Animo);
        Assert.Equal(SqliteGardenStore.VersionActual, store.VersionEsquema);
    }

    [Fact]
    public void ElArchivo_EstaCifrado()
    {
        ExportarJardin(SqliteGardenStore.NuevaLlave());
        Assert.False(SqliteGardenStore.EstaEnClaro(Archivo));
        Assert.DoesNotContain("Naranjo", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Archivo)));
    }

    [Fact]
    public void ContrasenaIncorrecta_NoTocaNada()
    {
        var llave = ExportarJardin(SqliteGardenStore.NuevaLlave());
        var antes = File.ReadAllBytes(Base);

        Assert.Throws<ContrasenaIncorrectaException>(() => Respaldo.Preparar(Archivo, "otra contraseña", Base, llave));

        Assert.Equal(antes, File.ReadAllBytes(Base));
        Assert.Equal(["jardin.db3", "mi-jardin.jardin"], Directory.GetFiles(_dir).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void ArchivoQueNoEsRespaldo_Lanza()
    {
        File.WriteAllBytes(Archivo, System.Security.Cryptography.RandomNumberGenerator.GetBytes(8192));
        Assert.Throws<ContrasenaIncorrectaException>(() => Respaldo.Preparar(Archivo, Contrasena, Base, SqliteGardenStore.NuevaLlave()));
    }

    [Fact]
    public void Instalar_ApartaLaBaseActual_SinBorrarla()
    {
        var llave = ExportarJardin(SqliteGardenStore.NuevaLlave());
        var actual = File.ReadAllBytes(Base);

        string? anterior = Respaldo.Instalar(Respaldo.Preparar(Archivo, Contrasena, Base, llave), Base);

        Assert.NotNull(anterior);
        Assert.Equal(actual, File.ReadAllBytes(anterior));
        using var store = new SqliteGardenStore(Base, llave: llave);
        Assert.Single(store.Cargar());
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("         ")]
    public void ContrasenaDebil_NoExporta(string debil)
    {
        using var store = new SqliteGardenStore(Base, llave: SqliteGardenStore.NuevaLlave());
        Assert.Throws<ArgumentException>(() => Respaldo.Exportar(store, Archivo, debil));
        Assert.False(File.Exists(Archivo));
    }

    [Fact]
    public void Exportar_SobreUnArchivoExistente_LoReemplaza()
    {
        File.WriteAllText(Archivo, "viejo");
        ExportarJardin(SqliteGardenStore.NuevaLlave());
        Assert.False(SqliteGardenStore.EstaEnClaro(Archivo));
        Assert.NotEqual("viejo", File.ReadAllText(Archivo));
    }
}
