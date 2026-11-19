using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Plantar un árbol: se elige el hábito de una lista (nunca se escribe), el modo si el hábito lo permite,
/// un cuestionario opcional y un árbol que no esté ya en el jardín. Fuera de aquí solo se verá el árbol.
/// </summary>
public sealed class NuevoHabitoPage : ContentPage
{
    private readonly IReadOnlyList<HabitoInfo> _habitos = Servicios.Jardin.HabitosDisponibles();
    private readonly IReadOnlyList<string> _especies = Servicios.Jardin.EspeciesLibres();

    private readonly Picker _habito = new()
    {
        Title = "Elige el hábito",
        FontFamily = Tema.Fuente,
        FontSize = 18,
        TextColor = Tema.Hueso,
        TitleColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
    };

    private readonly Chips<HabitType> _tipo = new(
        [(HabitType.AbstinenciaTotal, "Dejarlo por completo"), (HabitType.Moderable, "Moderarlo")],
        iniciales: [HabitType.AbstinenciaTotal]);

    private readonly Chips<DayOfWeek> _dias = new(Dias.Semana, maximo: CheatDaySchedule.MaxDiasPorSemana);
    private readonly Chips<string> _especie;

    private readonly VerticalStackLayout _seccionTipo;
    private readonly VerticalStackLayout _seccionDias;
    private readonly Label _soloAbstinencia = Tema.Texto("", 14, color: Tema.HuesoTenue);
    private readonly Border _aviso;
    private readonly Label _textoAviso = Tema.Texto("", 14);

    private readonly Chips<bool> _tienePrevio = new([(false, "No"), (true, "Sí")], iniciales: [false]);
    private readonly Entry _previoCantidad = new()
    {
        Placeholder = "0",
        Keyboard = Keyboard.Numeric,
        FontFamily = Tema.Fuente,
        FontSize = 18,
        TextColor = Tema.Hueso,
        PlaceholderColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
        WidthRequest = 90,
    };
    private readonly Chips<int> _previoUnidad = new([(1, "días"), (30, "meses"), (365, "años")], iniciales: [1]);
    private readonly Grid _filaPrevio;
    private readonly Chips<string> _apoyo = new([("grupo", "Grupo de apoyo"), ("terapia", "Terapia")], maximo: 2);

    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private bool _plantando;

    public NuevoHabitoPage()
    {
        Title = "Plantar un árbol";
        BackgroundColor = Tema.Musgo;

        foreach (var h in _habitos) _habito.Items.Add(h.Nombre);
        _habito.SelectedIndexChanged += (_, _) => ActualizarHabito();

        _especie = new Chips<string>(_especies.Select(e => (e, e)), iniciales: _especies.Take(1));

        _seccionTipo = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { Tema.Texto("¿Cómo lo vas a dejar?", 17, Tema.FuenteNegrita), _tipo, _soloAbstinencia },
        };
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
        _aviso = Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 6,
            Children = { Tema.Texto("Antes de empezar, cuídate", 16, Tema.FuenteNegrita, Tema.Miel), _textoAviso },
        }, Tema.TierraOscura, Tema.Miel);

        _filaPrevio = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
        };
        _filaPrevio.Add(_previoCantidad, 0);
        _filaPrevio.Add(_previoUnidad, 1);
        _previoCantidad.VerticalOptions = LayoutOptions.Start;
        var notaPrevio = Tema.Texto("Tu árbol nacerá con esa edad.", 13, color: Tema.HuesoTenue);
        _tipo.Cambio += ActualizarTipo;
        _tienePrevio.Cambio += () => _filaPrevio.IsVisible = notaPrevio.IsVisible = _tienePrevio.Elegido;
        _filaPrevio.IsVisible = notaPrevio.IsVisible = false;

        var plantar = Tema.Boton("Plantar", Tema.Miel);
        plantar.Clicked += Plantar;

        var cuestionario = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Tema.Texto("Unas preguntas (opcionales)", 17, Tema.FuenteNegrita),
                Tema.Texto("¿Ya llevas tiempo limpio que puedas comprobar? Por ejemplo, una ficha de tu grupo.", 15),
                _tienePrevio,
                _filaPrevio,
                notaPrevio,
                Tema.Texto("¿Tienes acompañamiento?", 15),
                _apoyo,
                Tema.Texto("Si eliges alguno, podrás registrar cada vez que vayas.", 13, color: Tema.HuesoTenue),
            },
        };

        View contenido = _habitos.Count == 0 || _especies.Count == 0
            ? Tema.Texto("Tu jardín ya está lleno. Para plantar otro árbol, primero quita uno.", 16)
            : new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    Tema.Texto("¿Qué hábito quieres dejar?", 17, Tema.FuenteNegrita),
                    Tema.Texto("Solo tú sabrás a qué está dedicado tu árbol. En el jardín y en los avisos solo se verá el árbol.", 14, color: Tema.HuesoTenue),
                    _habito,
                    _aviso,
                    _seccionTipo,
                    _seccionDias,
                    cuestionario,
                    Tema.Texto("Elige tu árbol", 17, Tema.FuenteNegrita),
                    _especie,
                    _error,
                    plantar,
                },
            };

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = new Thickness(18, 20), Children = { contenido } } };
        ActualizarHabito();
    }

    private HabitoInfo? Elegido => _habito.SelectedIndex >= 0 ? _habitos[_habito.SelectedIndex] : null;

    private void ActualizarHabito()
    {
        var info = Elegido;
        bool ambos = info is { PermiteModerar: true, PermiteAbstinencia: true };
        _tipo.IsVisible = ambos;
        _seccionTipo.IsVisible = info is not null;
        _soloAbstinencia.Text = info is not null && !ambos
            ? "Este hábito se trabaja dejándolo por completo: aquí no hay días libres."
            : "";
        _soloAbstinencia.IsVisible = !ambos;
        _aviso.IsVisible = info?.AvisoMedico is not null;
        _textoAviso.Text = info?.AvisoMedico ?? "";
        ActualizarTipo();
    }

    private HabitType Tipo
    {
        get
        {
            var info = Elegido;
            if (info is null) return HabitType.AbstinenciaTotal;
            if (!info.PermiteModerar) return HabitType.AbstinenciaTotal;
            if (!info.PermiteAbstinencia) return HabitType.Moderable;
            return _tipo.Elegido;
        }
    }

    private void ActualizarTipo() => _seccionDias.IsVisible = Elegido is not null && Tipo == HabitType.Moderable;

    private async void Plantar(object? sender, EventArgs e)
    {
        // Un doble toque no debe plantar dos árboles.
        if (_plantando) return;
        _error.Text = "";
        if (Elegido is not { } info)
        {
            _error.Text = "Elige un hábito de la lista.";
            return;
        }
        if (_especie.Elegido is not { } especie)
        {
            _error.Text = "Elige un árbol.";
            return;
        }

        int diasPrevios = 0;
        if (_tienePrevio.Elegido)
        {
            if (!int.TryParse(_previoCantidad.Text, out int cantidad) || cantidad < 0 || cantidad > 50 * 365)
            {
                _error.Text = "Escribe el tiempo previo como un número (por ejemplo, 10).";
                return;
            }
            diasPrevios = Math.Min(cantidad * _previoUnidad.Elegido, GardenService.MaxDiasPrevios);
        }

        _plantando = true;
        try
        {
            var tipo = Tipo;
            var dias = tipo == HabitType.Moderable ? _dias.Elegidos : [];
            Servicios.Jardin.CrearHabito(info.Habito, especie, tipo, dias, diasPrevios,
                grupo: _apoyo.Elegidos.Contains("grupo"), terapia: _apoyo.Elegidos.Contains("terapia"));
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _error.Text = ex.Message;
            _plantando = false;
        }
    }
}
