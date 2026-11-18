using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

public sealed class NuevoHabitoPage : ContentPage
{
    private readonly Entry _nombre = new()
    {
        Placeholder = "Por ejemplo: cigarro, azúcar, apuestas",
        FontFamily = Tema.Fuente,
        FontSize = 18,
        TextColor = Tema.Hueso,
        PlaceholderColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
    };

    private readonly Chips<HabitType> _tipo = new(
        [(HabitType.AbstinenciaTotal, "Dejarlo por completo"), (HabitType.Moderable, "Moderarlo")],
        iniciales: [HabitType.AbstinenciaTotal]);

    private readonly Chips<string> _especie = new(
        Especies.Todas.Select(e => (e.Nombre, e.Nombre)), iniciales: [Especies.Todas[0].Nombre]);

    private readonly Chips<DayOfWeek> _dias = new(Dias.Semana, maximo: CheatDaySchedule.MaxDiasPorSemana);

    private readonly VerticalStackLayout _seccionDias;
    private readonly Label _ayudaTipo = Tema.Texto("", 14, color: Tema.HuesoTenue);
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);

    public NuevoHabitoPage()
    {
        Title = "Plantar un árbol";
        BackgroundColor = Tema.Musgo;

        _seccionDias = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Tema.Texto("Días libres (hasta 2 por semana)", 17, Tema.FuenteNegrita),
                Tema.Texto("Esos días no se pide check-in y el árbol descansa en paz. Puedes cambiarlos cada sábado.", 14, color: Tema.HuesoTenue),
                _dias,
            },
        };

        _tipo.Cambio += ActualizarTipo;
        ActualizarTipo();

        var plantar = Tema.Boton("Plantar", Tema.Miel);
        plantar.Clicked += Plantar;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 20),
                Spacing = 14,
                Children =
                {
                    Tema.Texto("¿Qué hábito quieres dejar?", 17, Tema.FuenteNegrita),
                    _nombre,
                    Tema.Texto("¿Cómo lo vas a dejar?", 17, Tema.FuenteNegrita),
                    _tipo,
                    _ayudaTipo,
                    _seccionDias,
                    Tema.Texto("Elige tu árbol", 17, Tema.FuenteNegrita),
                    _especie,
                    _error,
                    plantar,
                },
            },
        };
    }

    private void ActualizarTipo()
    {
        bool moderable = _tipo.Elegido == HabitType.Moderable;
        _seccionDias.IsVisible = moderable;
        _ayudaTipo.Text = moderable
            ? "Para la dieta: azúcar, harinas, café. Permite días libres."
            : "Sin días libres: alcohol, pornografía, apuestas, cigarro.";
    }

    private async void Plantar(object? sender, EventArgs e)
    {
        try
        {
            var tipo = _tipo.Elegido;
            var dias = tipo == HabitType.Moderable ? _dias.Elegidos : [];
            Servicios.Jardin.CrearHabito(_nombre.Text ?? "", _especie.Elegido ?? Especies.Todas[0].Nombre, tipo, dias);
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _error.Text = ex.Message;
        }
    }
}
