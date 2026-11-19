using JardinDeHabitos.Core;

namespace JardinDeHabitos.Core.Tests;

public class AvanceDiarioTests
{
    private static readonly DateOnly D = new(2026, 1, 1); // jueves

    private readonly StrikeState _estado = new() { UltimoDiaEvaluado = D.AddDays(-1) };

    private static Habit Nuevo(HabitType tipo = HabitType.AbstinenciaTotal, params DayOfWeek[] cheat) => new()
    {
        Categoria = MalHabito.Azucares,
        EspecieDeArbol = "Naranjo",
        Tipo = tipo,
        FechaDeInicioRachaActual = D,
        DiasDeCheat = [.. cheat],
    };

    [Fact]
    public void DiaLibre_TrasCheckIn_Crece()
    {
        var h = Nuevo(HabitType.Moderable, DayOfWeek.Friday);
        _estado.FechaUltimoCheckIn = D; // jueves con check-in

        var ev = AvanceDiario.Avanzar(h, _estado, D.AddDays(2)); // cierra jueves y viernes

        Assert.Equal(1, h.DiasDeCrecimiento);
        Assert.Contains(ev, e => e.Tipo == TipoEvento.CrecioPorDiaLibre && e.Fecha == D.AddDays(1));
    }

    [Fact]
    public void DiaLibre_EnCuidado_NoCrece()
    {
        var h = Nuevo(HabitType.Moderable, DayOfWeek.Friday);
        _estado.FechaUltimoCheckIn = D;
        _estado.FinPlaga = D.AddDays(3);

        AvanceDiario.Avanzar(h, _estado, D.AddDays(2));

        Assert.Equal(0, h.DiasDeCrecimiento);
    }

    [Fact]
    public void DiaLibre_DuranteAbandono_NoCrece()
    {
        var h = Nuevo(HabitType.Moderable, DayOfWeek.Friday);
        // Jueves sin check-in: el viernes libre no crece.
        AvanceDiario.Avanzar(h, _estado, D.AddDays(2));

        Assert.Equal(0, h.DiasDeCrecimiento);
        Assert.Equal(1, _estado.DiasNormalesSinCheckIn);
    }

    [Fact]
    public void SinCheckIn_ElArbolSePausa()
    {
        var h = Nuevo();
        h.DiasDeCrecimiento = 5;

        AvanceDiario.Avanzar(h, _estado, D.AddDays(5));

        Assert.Equal(5, h.DiasDeCrecimiento);
        Assert.Equal(5, _estado.DiasNormalesSinCheckIn);
        Assert.Equal(0, _estado.StrikesAbandono);
    }

    [Fact]
    public void SieteDiasSinEntrar_MiniEvento()
    {
        var h = Nuevo();

        AvanceDiario.Avanzar(h, _estado, D.AddDays(6));
        Assert.Equal(MiniEvento.Ninguno, _estado.MiniEvento);

        var ev = AvanceDiario.Avanzar(h, _estado, D.AddDays(7));
        Assert.NotEqual(MiniEvento.Ninguno, _estado.MiniEvento);
        Assert.Single(ev, e => e.Tipo == TipoEvento.MiniEvento);
    }

    [Fact]
    public void Abandono14_Plaga_21_PlagaYRaices_28_Muere()
    {
        var h = Nuevo();
        h.DiasDeCrecimiento = 40;

        AvanceDiario.Avanzar(h, _estado, D.AddDays(14));
        Assert.Equal(1, _estado.StrikesAbandono);
        Assert.Equal(D.AddDays(17), _estado.FinPlaga);

        AvanceDiario.Avanzar(h, _estado, D.AddDays(21));
        Assert.Equal(2, _estado.StrikesAbandono);
        Assert.Equal(D.AddDays(24), _estado.FinPlaga);
        Assert.Equal(D.AddDays(29), _estado.FinRaices);

        var ev = AvanceDiario.Avanzar(h, _estado, D.AddDays(28));
        var muerte = Assert.Single(ev, e => e.Tipo == TipoEvento.MurioPorAbandono);
        Assert.Equal(40, muerte.Valor);
        Assert.Equal(0, h.DiasDeCrecimiento);
        Assert.Equal(D.AddDays(28), h.FechaDeInicioRachaActual);
    }

    [Fact]
    public void AbrirTras30Dias_AplicaTodoEnOrdenUnaSolaVez()
    {
        var h = Nuevo();

        var ev = AvanceDiario.Avanzar(h, _estado, D.AddDays(30));

        Assert.Equal(
            [TipoEvento.MiniEvento, TipoEvento.StrikeAbandono, TipoEvento.StrikeAbandono, TipoEvento.MurioPorAbandono],
            ev.Select(e => e.Tipo));
    }

    [Fact]
    public void Avanzar_DosVecesMismoDia_NoDuplica()
    {
        var h = Nuevo();
        AvanceDiario.Avanzar(h, _estado, D.AddDays(15));
        var segunda = AvanceDiario.Avanzar(h, _estado, D.AddDays(15));

        Assert.Empty(segunda);
        Assert.Equal(1, _estado.StrikesAbandono);
    }

    [Fact]
    public void AbandonoNoSeMezclaConDeclarados()
    {
        var h = Nuevo();
        _estado.StrikesDeclarados = 2;

        var ev = AvanceDiario.Avanzar(h, _estado, D.AddDays(14));

        Assert.DoesNotContain(ev, e => e.Tipo == TipoEvento.MurioPorAbandono);
        Assert.Equal(1, _estado.StrikesAbandono);
    }

    [Fact]
    public void DiaConCheckIn_NoCuentaComoFalta()
    {
        var h = Nuevo();
        _estado.FechaUltimoCheckIn = D;
        AvanceDiario.Avanzar(h, _estado, D.AddDays(1));

        Assert.Equal(0, _estado.DiasNormalesSinCheckIn);
    }
}
