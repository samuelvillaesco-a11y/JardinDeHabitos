using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>Un árbol de cerca: check-in, cuidado diario con gestos y tratamientos.</summary>
public sealed class ArbolPage : ContentPage, IRefrescable
{
    private readonly Guid _id;
    private readonly Escena _escena = new();
    private readonly GraphicsView _lienzo;
    private readonly ScrollView _scroll;
    private IDispatcherTimer? _timer;
    private VistaArbol _vista = null!;
    private DateOnly _hoyMostrado;
    private bool _checkInEnCurso;
    private static readonly TimeSpan Rapido = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan Lento = TimeSpan.FromSeconds(1);

    private readonly Label _titulo = Tema.Titulo("", 30);
    private readonly Label _etapa = Tema.Texto("", 16, Tema.FuenteNegrita, Tema.Hoja);
    private readonly Label _detalle = Tema.Texto("", 15, color: Tema.HuesoTenue);
    private readonly Label _ayuda = Tema.Texto("", 16, Tema.FuenteNegrita, Tema.Miel);
    private readonly Button _cancelar = Tema.Boton("Dejarlo para después", Tema.MusgoClaro, Tema.Hueso);

    private readonly Label _animoCara = new() { FontSize = 22, VerticalTextAlignment = TextAlignment.Center };
    private readonly Label _animoTexto = Tema.Texto("", 13, Tema.FuenteNegrita);
    private readonly Border _animo;

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
        _escena.Cosechado += Cosechado;
        // Respeta "quitar animaciones" de Android: el árbol pasa directo a su estado final.
        _escena.MovimientoReducido = !Android.Animation.ValueAnimator.AreAnimatorsEnabled();

        // Ánimo del árbol: una calcomanía en la esquina (el árbol no tiene cara).
        _animoTexto.VerticalTextAlignment = TextAlignment.Center;
        _animo = new Border
        {
            Background = Tema.Musgo.WithAlpha(0.92f),
            Stroke = Tema.Linea,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(8, 2, 10, 2),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 22, 28, 0),
            InputTransparent = true,
            Content = new HorizontalStackLayout { Spacing = 4, Children = { _animoCara, _animoTexto } },
        };

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
        raiz.Add(new Grid { Children = { _lienzo, _animo } }, 0, 0);
        raiz.Add(_scroll, 0, 1);
        Content = raiz;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refrescar();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Lento;
        _timer.Tick += (_, _) =>
        {
            // 30 Hz solo durante un mini-juego o una animación; si no, un tic por segundo para ver si cambió el día.
            bool jugando = _escena.Juego != MiniJuego.Ninguno || _escena.Animando;
            if (jugando)
            {
                _escena.Tick(0.033f);
                Repintar();
            }
            else if (Servicios.Jardin.Hoy != _hoyMostrado)
            {
                AlVolver();
            }
            else if (DateTime.Now.Minute != _escena.Ahora.Minute)
            {
                // El sol y la luna avanzan con el reloj.
                _escena.Ahora = DateTime.Now;
                _lienzo.Invalidate();
            }
            var intervalo = _escena.Juego != MiniJuego.Ninguno || _escena.Animando ? Rapido : Lento;
            if (_timer.Interval != intervalo) _timer.Interval = intervalo;
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

    public void AlVolver() => _ = Intentar(() => { });

    /// <summary>
    /// Ejecuta una acción sobre el jardín y vuelve a pintar. Si la pantalla quedó vieja
    /// (por ejemplo, cambió el día), muestra el motivo en vez de cerrar la app.
    /// </summary>
    private async Task Intentar(Action accion)
    {
        try
        {
            accion();
            Refrescar();
        }
        catch (KeyNotFoundException)
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            _escena.Cancelar();
            await DisplayAlertAsync("No se pudo", ex.Message, "Entendido");
            Refrescar();
        }
    }

    private void Refrescar()
    {
        _vista = Servicios.Jardin.Arbol(_id);
        if (_hoyMostrado != Servicios.Jardin.Hoy) _escena.ReiniciarCosecha();
        _hoyMostrado = Servicios.Jardin.Hoy;
        var v = _vista;
        Title = "";
        _escena.Arbol = EstadoArbol.De(v);
        _escena.Debuffs = Servicios.Jardin.Debuffs();
        _escena.PuedeCosechar = v.Etapa >= GrowthStage.FrutaComestible && !v.EnCuidado
            && Preferences.Default.Get(ClaveCosecha, 0) != _hoyMostrado.DayNumber;
        PintarAnimo(v);

        bool jugando = _escena.Juego != MiniJuego.Ninguno;
        _ayuda.IsVisible = jugando;
        _cancelar.IsVisible = jugando;
        _checkIn.IsVisible = !jugando;
        _cuidado.IsVisible = !jugando;
        _extras.IsVisible = !jugando;

        _titulo.Text = v.Especie;
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
        if (v.DiasSinVisita > 0)
            partes.Add(v.DiasSinVisita == 1
                ? "Ayer no lo visitaste: hoy no creció."
                : $"Lleva {v.DiasSinVisita} días sin visita y está en pausa. A los {AvanceDiario.PrimerStrikeAbandono} le cae una plaga.");
        if (v.EnCuidado)
            partes.Add("Mientras está enfermo no crece.");
        if (v.Strikes > 0)
            partes.Add(v.Strikes == 1
                ? "Tiene 1 tropiezo: al segundo le salen raíces podridas."
                : $"Tiene {v.Strikes} tropiezos: al {CheckInProcessor.StrikesParaMorir}.º el árbol vuelve a ser semilla.");
        if (v.StrikesAbandono > 0)
            partes.Add(v.StrikesAbandono == 1 ? "Y 1 marca por abandono." : $"Y {v.StrikesAbandono} marcas por abandono.");
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
            AgregarAsistencia(v);
            return;
        }

        var cumpli = Tema.Boton("Lo cumplí", Tema.Hoja);
        var rompi = Tema.Boton("Lo rompí", Tema.Tierra, Tema.SobreTierra);
        cumpli.Clicked += async (_, _) => await HacerCheckIn(CheckInResult.Cumplido);
        rompi.Clicked += async (_, _) => await HacerCheckIn(CheckInResult.Roto);

        var botones = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
        };
        botones.Add(cumpli, 0);
        botones.Add(rompi, 1);

        _checkIn.Add(Tema.Texto("¿Cómo te fue hoy?", 17, Tema.FuenteNegrita));
        _checkIn.Add(botones);
        AgregarAsistencia(v);
    }

    private void AgregarAsistencia(VistaArbol v)
    {
        if (!v.PideAsistencia) return;
        if (v.AsistioHoy)
        {
            _checkIn.Add(Tema.Texto("Asistencia de hoy registrada. Pedir ayuda también es cuidar.", 14, color: Tema.HuesoTenue));
            return;
        }
        var fui = Tema.Boton("Hoy fui a mi grupo o sesión", Tema.MusgoClaro, Tema.Hueso);
        fui.Clicked += async (_, _) =>
        {
            await Intentar(() => Servicios.Jardin.RegistrarAsistencia(_id));
            await Avisos.AnunciarLogros(this);
        };
        _checkIn.Add(fui);
    }

    private void PintarCuidado(VistaArbol v)
    {
        _cuidado.Clear();
        _cuidado.Add(Tema.Texto("Cuidado de hoy", 17, Tema.FuenteNegrita));

        if (v.MiniEvento != MiniEvento.Ninguno)
        {
            var (texto, juego) = v.MiniEvento switch
            {
                MiniEvento.Caracol => ("¡Unos caracoles se colaron mientras no estabas!", MiniJuego.Plaga),
                MiniEvento.RamaSeca => ("Se secó una rama mientras no estabas.", MiniJuego.Podar),
                _ => ("La tierra se resecó mientras no estabas.", MiniJuego.Regar),
            };
            _cuidado.Add(Tema.Texto(texto, 15, Tema.FuenteNegrita, Tema.Miel));
            var rescatar = Tema.Boton("Rescatarlo", Tema.Miel);
            rescatar.Clicked += (_, _) => Jugar(juego, rescate: true);
            _cuidado.Add(rescatar);
        }

        if (v.EnCuidado && !v.TratadoHoy)
        {
            string que = v.Plaga && v.Raices ? "plaga y raíces podridas" : v.Plaga ? "plaga" : "raíces podridas";
            string hasta = v.FinCuidado is { } fin ? $" Cuidado diario hasta el {Tema.FechaCorta(fin.AddDays(-1))}." : "";
            _cuidado.Add(Tema.Texto($"Tiene {que}.{hasta}", 15, color: Tema.Miel));
            var curar = Tema.Boton("Curar el árbol", Tema.Miel);
            curar.Clicked += (_, _) => Jugar(v.Plaga ? MiniJuego.Plaga : MiniJuego.Raices);
            _cuidado.Add(curar);
        }
        else if (v.EnCuidado)
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

        if (v.PuedeBorron)
        {
            var borrar = Tema.Boton("Borrón y cuenta nueva", Tema.MusgoClaro, Tema.Hueso);
            borrar.Clicked += async (_, _) =>
            {
                bool si = await DisplayAlertAsync("Tres meses sin tropiezos",
                    $"Llevas {CheckInProcessor.DiasParaBorron} días sin strikes en este árbol. Puedes borrarlos o dejarlos como recuerdo. Es tu decisión.",
                    "Borrarlos", "Dejarlos");
                if (!si) return;
                await Intentar(() => Servicios.Jardin.Borron(_id));
                await Avisos.AnunciarLogros(this);
            };
            _extras.Add(Tema.Texto("Tres meses sin tropiezos: puedes empezar con la tierra limpia.", 15, Tema.FuenteNegrita));
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
                guardar.Clicked += async (_, _) =>
                    await Intentar(() => Servicios.Jardin.EditarDiasDeCheat(_id, chips.Elegidos));
                _extras.Add(Tema.Texto("Hoy es sábado: puedes cambiar tus días libres. El cambio empieza mañana.", 15));
                _extras.Add(chips);
                _extras.Add(Dias.AvisoPara(chips));
                _extras.Add(guardar);
            }
        }
    }

    private async Task HacerCheckIn(CheckInResult resultado)
    {
        // Un doble toque no debe mandar dos check-ins.
        if (_checkInEnCurso) return;
        _checkInEnCurso = true;
        try { await HacerCheckInUnaVez(resultado); }
        finally { _checkInEnCurso = false; }
    }

    private string ClaveCosecha => $"cosecha.{_id}";

    private async void Cosechado(int frutos)
    {
        Preferences.Default.Set(ClaveCosecha, _hoyMostrado.DayNumber);
        await DisplayAlertAsync("Cosecha del día", $"Cortaste {frutos} frutos. Mañana habrá más.", "¡Qué rico!");
        Refrescar();
    }

    private void PintarAnimo(VistaArbol v)
    {
        var (cara, texto) = v switch
        {
            { MiniEvento: not MiniEvento.Ninguno } => ("😟", "Necesita ayuda"),
            { Raices: true } => ("🤒", "Raíces dañadas"),
            { Plaga: true } => ("🤒", "Con plaga"),
            { EsCheatDayHoy: true } => ("😌", "Descansando"),
            { PideCheckInHoy: true } => ("🙂", "Te espera"),
            { Etapa: GrowthStage.FrutaLegendaria } => ("🤩", "Radiante"),
            { RegadoHoy: false, Etapa: >= GrowthStage.Arbusto } => ("💧", "Con sed"),
            _ => ("😊", "Contento"),
        };
        _animoCara.Text = cara;
        _animoTexto.Text = texto;
        SemanticProperties.SetDescription(_animo, $"Ánimo del árbol: {texto}");
    }

    /// <summary>Corre la animación y espera a que termine antes de seguir (diálogos, logros).</summary>
    private async Task Animar(Efecto efecto, GrowthStage? antes = null)
    {
        if (_escena.MovimientoReducido) return;
        _escena.Animar(efecto, antes);
        if (_timer is not null) _timer.Interval = Rapido;
        await Task.Delay(TimeSpan.FromSeconds(Escena.Duracion(efecto)));
    }

    private async Task HacerCheckInUnaVez(CheckInResult resultado)
    {
        var etapaAntes = _vista.Etapa;
        if (resultado == CheckInResult.Roto)
        {
            bool seguro = await DisplayAlertAsync("¿Lo rompiste hoy?",
                "Ser honesto es lo que hace crecer el jardín.", "Sí, lo rompí", "Cancelar");
            if (!seguro) return;
        }

        CheckInOutcome o;
        try
        {
            o = Servicios.Jardin.CheckIn(_id, resultado);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            await DisplayAlertAsync("No se pudo", ex.Message, "Entendido");
            Refrescar();
            return;
        }

        Refrescar();
        if (!o.ArbolMurio)
        {
            if (resultado == CheckInResult.Roto) await Animar(Efecto.Grieta);
            else if (_vista.Etapa != etapaAntes) await Animar(Efecto.Crecer, etapaAntes);
            else await Animar(Efecto.Alivio);
        }

        if (o.ArbolMurio)
        {
            await DisplayAlertAsync("El árbol volvió a ser semilla",
                "Fue el tercer tropiezo. Lo que aprendiste no se pierde: hoy queda plantada una semilla nueva y mañana haces su primer check-in.",
                "Plantar de nuevo");
        }
        else if (resultado == CheckInResult.Roto)
        {
            var v = Servicios.Jardin.Arbol(_id);
            string msg = v.Raices
                ? "Le salieron raíces podridas. Hay que cuidarlo cada día hasta que sane; mientras tanto no crece."
                : $"Le cayó una plaga. Defiéndelo durante {CheckInProcessor.DiasPlaga} días; mientras tanto no crece. Mañana es otro día.";
            // En un tropiezo, la ayuda queda a un toque.
            if (await DisplayAlertAsync("Gracias por decirlo", msg + "\n\nSi lo necesitas, pide ayuda: tus jardineros, la Línea de la Vida o el 911.", "Pedir ayuda", "Cuidarlo"))
                await this.UnaVez(() => new AyudaPage());
        }

        await Avisos.AnunciarLogros(this);
        Refrescar();
    }

    private void Jugar(MiniJuego juego, bool rescate = false)
    {
        _escena.Iniciar(juego, rescate);
        if (_timer is not null) _timer.Interval = Rapido;
        Refrescar();
    }

    private async void JuegoCompletado(MiniJuego juego)
    {
        var jardin = Servicios.Jardin;
        int piezas = _escena.Piezas;

        if (_escena.Rescate)
        {
            await Intentar(() => jardin.ResolverMiniEvento(_id, juego == MiniJuego.Plaga ? piezas : 0));
        }
        else if (juego == MiniJuego.Plaga && _vista.Raices)
        {
            // Falta sacar las raíces podridas.
            await Intentar(() => jardin.RegistrarJuego(_id, MiniJuegoTipo.Plaga, piezas));
            _escena.Iniciar(MiniJuego.Raices);
            Refrescar();
            return;
        }
        else
        {
            await Intentar(() =>
            {
                switch (juego)
                {
                    case MiniJuego.Regar: jardin.RegistrarJuego(_id, MiniJuegoTipo.Regar); break;
                    case MiniJuego.Podar: jardin.RegistrarJuego(_id, MiniJuegoTipo.Podar, piezas); break;
                    case MiniJuego.Plaga:
                        jardin.RegistrarJuego(_id, MiniJuegoTipo.Plaga, piezas);
                        jardin.RegistrarTratamiento(_id);
                        break;
                    case MiniJuego.Raices:
                        jardin.RegistrarJuego(_id, MiniJuegoTipo.Raices, piezas);
                        jardin.RegistrarTratamiento(_id);
                        break;
                }
            });
        }
        if (juego == MiniJuego.Podar) await Animar(Efecto.Sacudida);
        await Avisos.AnunciarLogros(this);
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
            $"Se quita el {_vista.Especie.ToLower(Tema.Cultura)} del jardín. Tus logros se quedan.", "Quitar", "Cancelar");
        if (!si) return;
        try
        {
            Servicios.Jardin.EliminarHabito(_id);
        }
        catch (KeyNotFoundException)
        {
            // Ya no existía: solo volver.
        }
        await Navigation.PopAsync();
    }
}
