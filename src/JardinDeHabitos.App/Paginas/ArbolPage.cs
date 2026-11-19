using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>Un árbol de cerca: check-in, cuidado diario con gestos y tratamientos.</summary>
public sealed class ArbolPage : ContentPage
{
    private readonly Guid _id;
    private readonly Escena _escena = new();
    private readonly GraphicsView _lienzo;
    private readonly ScrollView _scroll;
    private IDispatcherTimer? _timer;
    private VistaArbol _vista = null!;

    private readonly Label _titulo = Tema.Titulo("", 30);
    private readonly Label _etapa = Tema.Texto("", 16, Tema.FuenteNegrita, Tema.Hoja);
    private readonly Label _detalle = Tema.Texto("", 15, color: Tema.HuesoTenue);
    private readonly Label _ayuda = Tema.Texto("", 16, Tema.FuenteNegrita, Tema.Miel);
    private readonly Button _cancelar = Tema.Boton("Dejarlo para después", Tema.MusgoClaro, Tema.Hueso);

    private readonly VerticalStackLayout _checkIn = new() { Spacing = 10 };
    private readonly VerticalStackLayout _cuidado = new() { Spacing = 10 };
    private readonly VerticalStackLayout _extras = new() { Spacing = 10 };

    public ArbolPage(Guid id)
    {
        _id = id;
        BackgroundColor = Tema.Musgo;

        _lienzo = new GraphicsView { Drawable = _escena, HeightRequest = 320 };
        _lienzo.StartInteraction += (_, e) => { _escena.Tocar(e.Touches[0]); Repintar(); };
        _lienzo.DragInteraction += (_, e) => { _escena.Arrastrar(e.Touches[0]); Repintar(); };
        _lienzo.EndInteraction += (_, _) => { _escena.Soltar(); Repintar(); };
        _escena.Completado += JuegoCompletado;

        _cancelar.Clicked += (_, _) => { _escena.Cancelar(); Refrescar(); };

        var quitar = Tema.Boton("Quitar este árbol del jardín", Colors.Transparent, Tema.HuesoTenue);
        quitar.Clicked += Quitar;

        // El lienzo va fuera del ScrollView: así el desplazamiento no le roba los arrastres.
        _lienzo.Margin = new Thickness(18, 12, 18, 0);
        _scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 12, 18, 28),
                Spacing = 14,
                Children =
                {
                    _ayuda,
                    _cancelar,
                    new VerticalStackLayout { Spacing = 2, Children = { _titulo, _etapa, _detalle } },
                    _checkIn,
                    _cuidado,
                    _extras,
                    quitar,
                },
            },
        };
        var raiz = new Grid { RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star)] };
        raiz.Add(_lienzo, 0, 0);
        raiz.Add(_scroll, 0, 1);
        Content = raiz;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refrescar();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += (_, _) =>
        {
            if (_escena.Tick(0.033f) || _escena.Juego != MiniJuego.Ninguno) Repintar();
        };
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private void Repintar()
    {
        _lienzo.Invalidate();
        if (_escena.Juego != MiniJuego.Ninguno) _ayuda.Text = $"{Instruccion(_escena.Juego)} {_escena.Progreso}";
    }

    private void Refrescar()
    {
        _vista = Servicios.Jardin.Arbol(_id);
        var v = _vista;
        Title = "";
        _escena.Arbol = EstadoArbol.De(v);

        bool jugando = _escena.Juego != MiniJuego.Ninguno;
        _ayuda.IsVisible = jugando;
        _cancelar.IsVisible = jugando;
        _checkIn.IsVisible = !jugando;
        _cuidado.IsVisible = !jugando;
        _extras.IsVisible = !jugando;

        _titulo.Text = v.Nombre;
        _etapa.Text = $"{Tema.NombreEtapa(v.Etapa)}, día {v.Dias}";
        _detalle.Text = Detalle(v);

        PintarCheckIn(v);
        PintarCuidado(v);
        PintarExtras(v);
        Repintar();
    }

    private static string Detalle(VistaArbol v)
    {
        var partes = new List<string>();
        var siguiente = GrowthStageCalculator.Umbrales.FirstOrDefault(u => u.DiaMinimo > v.Dias);
        if (siguiente != default)
        {
            int faltan = siguiente.DiaMinimo - v.Dias;
            partes.Add(faltan == 1
                ? $"Mañana llega a {Tema.NombreEtapa(siguiente.Etapa).ToLower(Tema.Cultura)}."
                : $"Faltan {faltan} días para {Tema.NombreEtapa(siguiente.Etapa).ToLower(Tema.Cultura)}.");
        }
        if (v.Cicatrices > 0)
            partes.Add(v.Cicatrices == 1 ? "Tiene 1 cicatriz." : $"Tiene {v.Cicatrices} cicatrices.");
        if (v.Racha > 0)
            partes.Add(v.Racha == 1
                ? "Un día roto. Si el próximo también, salen raíces podridas."
                : "Dos días rotos seguidos: un tercero y el árbol muere.");
        return string.Join(" ", partes);
    }

    private void PintarCheckIn(VistaArbol v)
    {
        _checkIn.Clear();
        if (v.EsCheatDayHoy)
        {
            _checkIn.Add(Tema.Parcela(Tema.Texto("Hoy es tu día libre. El árbol descansa en paz. Disfruta con moderación.", 15)));
            return;
        }
        if (!v.PideCheckInHoy)
        {
            _checkIn.Add(Tema.Texto("Check-in de hoy listo. Vuelve mañana.", 15, color: Tema.HuesoTenue));
            return;
        }

        var cumpli = Tema.Boton("Lo cumplí", Tema.Hoja);
        var rompi = Tema.Boton("Lo rompí", Tema.Tierra, Tema.Hueso);
        cumpli.Clicked += async (_, _) => await HacerCheckIn(CheckInResult.Cumplido);
        rompi.Clicked += async (_, _) => await HacerCheckIn(CheckInResult.Roto);

        var botones = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
        };
        botones.Add(cumpli, 0);
        botones.Add(rompi, 1);

        _checkIn.Add(Tema.Texto($"¿Cómo te fue hoy con {v.Nombre.ToLower(Tema.Cultura)}?", 17, Tema.FuenteNegrita));
        _checkIn.Add(botones);
    }

    private void PintarCuidado(VistaArbol v)
    {
        _cuidado.Clear();
        _cuidado.Add(Tema.Texto("Cuidado de hoy", 17, Tema.FuenteNegrita));

        if (v.Penalizacion != PenaltyLevel.Ninguna && !v.TratadoHoy)
        {
            string que = v.Penalizacion == PenaltyLevel.Strike2 ? "plaga y raíces podridas" : "plaga";
            string hasta = v.FinPenalizacion is { } fin ? $" Cuidado diario hasta el {Tema.FechaCorta(fin.AddDays(-1))}." : "";
            _cuidado.Add(Tema.Texto($"Tiene {que}.{hasta}", 15, color: Tema.Miel));
            var curar = Tema.Boton("Curar el árbol", Tema.Miel);
            curar.Clicked += (_, _) => Jugar(MiniJuego.Plaga);
            _cuidado.Add(curar);
        }
        else if (v.Penalizacion != PenaltyLevel.Ninguna)
        {
            _cuidado.Add(Tema.Texto("Tratamiento de hoy hecho. Mañana toca otra vez.", 15, color: Tema.HuesoTenue));
        }

        var fila = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
        };
        var regar = Tema.Boton(v.RegadoHoy ? "Regado hoy" : "Regar", Tema.MusgoClaro, Tema.Hueso);
        var podar = Tema.Boton(v.PodadoHoy ? "Podado hoy" : "Podar", Tema.MusgoClaro, Tema.Hueso);
        regar.Clicked += (_, _) => Jugar(MiniJuego.Regar);
        podar.Clicked += (_, _) => Jugar(MiniJuego.Podar);
        fila.Add(regar, 0);
        fila.Add(podar, 1);
        _cuidado.Add(fila);
    }

    private void PintarExtras(VistaArbol v)
    {
        _extras.Clear();

        if (v.PuedeBorrarCicatrices)
        {
            var borrar = Tema.Boton("Borrar cicatrices", Tema.MusgoClaro, Tema.Hueso);
            borrar.Clicked += async (_, _) =>
            {
                bool si = await DisplayAlertAsync("Seis meses de racha",
                    "Puedes borrar las cicatrices de este árbol o dejarlas como recuerdo. Es tu decisión.",
                    "Borrarlas", "Dejarlas");
                if (!si) return;
                Servicios.Jardin.BorrarCicatrices(_id);
                Refrescar();
            };
            _extras.Add(Tema.Texto("Tu árbol cumplió seis meses.", 15, Tema.FuenteNegrita));
            _extras.Add(borrar);
        }

        if (v.Tipo == HabitType.Moderable)
        {
            string dias = v.DiasDeCheat.Count == 0
                ? "Sin días libres."
                : "Días libres: " + string.Join(" y ", v.DiasDeCheat.Select(Tema.NombreDia)) + ".";
            _extras.Add(Tema.Texto(dias, 15, color: Tema.HuesoTenue));

            if (CheatDaySchedule.PuedeEditar(Servicios.Jardin.Hoy))
            {
                var chips = new Chips<DayOfWeek>(Dias.Semana, CheatDaySchedule.MaxDiasPorSemana, v.DiasDeCheat);
                var guardar = Tema.Boton("Guardar días libres", Tema.MusgoClaro, Tema.Hueso);
                guardar.Clicked += (_, _) =>
                {
                    Servicios.Jardin.EditarDiasDeCheat(_id, chips.Elegidos);
                    Refrescar();
                };
                _extras.Add(Tema.Texto("Hoy es sábado: puedes cambiar los días libres de la semana.", 15));
                _extras.Add(chips);
                _extras.Add(guardar);
            }
        }
    }

    private async Task HacerCheckIn(CheckInResult resultado)
    {
        if (resultado == CheckInResult.Roto)
        {
            bool seguro = await DisplayAlertAsync("¿Lo rompiste hoy?",
                "Ser honesto es lo que hace crecer el jardín.", "Sí, lo rompí", "Cancelar");
            if (!seguro) return;
        }

        ResultadoCheckIn r;
        try
        {
            r = Servicios.Jardin.CheckIn(_id, resultado);
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("No se pudo", ex.Message, "Entendido");
            Refrescar();
            return;
        }

        var o = r.Outcome;
        if (o.ArbolMurio)
        {
            await DisplayAlertAsync("El árbol murió",
                "Tres días rotos seguidos. Todo vuelve a empezar desde una semilla. Puedes volver a intentarlo hoy mismo.",
                "Plantar de nuevo");
        }
        else if (resultado == CheckInResult.Roto)
        {
            string msg = o.Penalizacion == PenaltyLevel.Strike2
                ? "Dos días seguidos: le salieron raíces podridas. Necesita cuidado diario durante una semana."
                : "Le cayó una plaga. Necesita cuidado durante 2 días. Mañana es otro día.";
            await DisplayAlertAsync("Día roto", msg, "Cuidarlo");
        }

        foreach (var logro in r.NuevosLogros)
        {
            var info = LogroInfo.De(logro);
            await DisplayAlertAsync($"Logro: {info.Nombre}", info.Descripcion, "Seguir");
        }
        Refrescar();
    }

    private void Jugar(MiniJuego juego)
    {
        _escena.Iniciar(juego);
        Refrescar();
    }

    private async void JuegoCompletado(MiniJuego juego)
    {
        var jardin = Servicios.Jardin;
        switch (juego)
        {
            case MiniJuego.Regar:
                jardin.Regar(_id);
                break;
            case MiniJuego.Podar:
                jardin.Podar(_id);
                break;
            case MiniJuego.Plaga when _vista.Penalizacion == PenaltyLevel.Strike2:
                // Falta sacar las raíces podridas.
                _escena.Iniciar(MiniJuego.Raices);
                Repintar();
                return;
            case MiniJuego.Plaga:
            case MiniJuego.Raices:
                jardin.RegistrarTratamiento(_id);
                break;
        }
        Refrescar();
        await _lienzo.ScaleToAsync(1.03, 120);
        await _lienzo.ScaleToAsync(1.0, 120);
    }

    private static string Instruccion(MiniJuego juego) => juego switch
    {
        MiniJuego.Plaga => "Toca cada bicho para quitarlo.",
        MiniJuego.Raices => "Arrastra las raíces podridas fuera de la tierra.",
        MiniJuego.Regar => "Arrastra la regadera sobre el árbol.",
        MiniJuego.Podar => "Desliza el dedo sobre las ramas sueltas y hierbas para cortarlas.",
        _ => "",
    };

    private async void Quitar(object? sender, EventArgs e)
    {
        bool si = await DisplayAlertAsync("Quitar árbol",
            $"Se borra {_vista.Nombre} del jardín. Tus logros se quedan.", "Quitar", "Cancelar");
        if (!si) return;
        Servicios.Jardin.EliminarHabito(_id);
        await Navigation.PopAsync();
    }
}
