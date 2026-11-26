using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Blog botánico: tu diario y el registro diario de ánimo, ganas, emociones, detonantes y síntomas,
/// con gráficas de cómo vas. Va bajo llave.
/// </summary>
public sealed class BlogPage : ContentPage, IPrivada, IConLlave
{
    private const string ClaveRango = "blog.rango";
    private static readonly (int Dias, string Texto)[] Rangos = [(14, "2 semanas"), (30, "1 mes"), (90, "3 meses")];
    private const int EntradasVisibles = 30;

    private bool _abierto;

    // La pantalla del candado o el editor tapan esta página: eso no cuenta como salir.
    private bool _verificando;
    private bool _escribiendo;
    private readonly VerticalStackLayout _pila = new() { Padding = new Thickness(18, 28, 18, 28), Spacing = 14 };

    public BlogPage()
    {
        Title = "Blog botánico";
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);
        Content = new ScrollView { Content = _pila };
        PintarCerrado();
    }

    private static int Rango
    {
        get
        {
            try { return Preferences.Default.Get(ClaveRango, 14); }
            catch { return 14; }
        }
        set
        {
            try { Preferences.Default.Set(ClaveRango, value); }
            catch { /* Solo es una comodidad. */ }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
        Pantalla.Actualizar();
        _escribiendo = false;
        if (_abierto) Pintar();
        else if (!_verificando) await Abrir();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Al salir de la pestaña, se vuelve a cerrar con llave.
        if (_verificando || _escribiendo) return;
        Cerrar();
    }

    /// <summary>Vuelve a cerrar el blog con llave (al salir de la pestaña o al volver de segundo plano).</summary>
    public void Cerrar()
    {
        _abierto = false;
        _escribiendo = false;
        PintarCerrado();
    }

    private async Task Abrir()
    {
        if (_verificando) return;
        _verificando = true;
        bool ok;
        try { ok = await Seguridad.EntrarAsync(this, Seguro.Blog, "Tu blog botánico"); }
        finally { _verificando = false; }
        if (!ok) return;
        _abierto = true;
        Pintar();
    }

    private void PintarCerrado()
    {
        _pila.Clear();
        _pila.Add(Tema.Titulo("Blog botánico", 34));
        var abrir = Tema.Boton("Abrir mi blog", Tema.Miel);
        abrir.Clicked += async (_, _) => await Abrir();
        _pila.Add(Tema.Texto("Tu diario está bajo llave.", 16, color: Tema.HuesoTenue));
        _pila.Add(abrir);
    }

    // ---------- Contenido ----------

    private void Pintar()
    {
        var svc = Servicios.Jardin;
        var hoy = svc.Hoy;
        var entradas = svc.Diario();
        var r = svc.ResumenDiario(Rango);

        _pila.Clear();
        _pila.Add(Tema.Titulo("Blog botánico", 34));
        _pila.Add(Tema.Texto("Tu diario y cómo te has sentido. Solo tú lo ves.", 16, color: Tema.HuesoTenue));
        _pila.Add(TarjetaHoy(entradas.FirstOrDefault(e => e.Dia == hoy), hoy));

        if (entradas.Count == 0)
        {
            _pila.Add(Tema.Parcela(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Tema.Texto("Tu primera página", 17, Tema.FuenteNegrita, Tema.Miel),
                    Tema.Texto("Cuando escribas unos días, aquí verás gráficas de tu ánimo, tus ganas y tus síntomas, y qué suele detonarlas.", 15),
                },
            }));
            _pila.Add(Ciencia(svc.Plantados()));
            return;
        }

        var rango = new Chips<int>(Rangos, iniciales: [Rango]);
        rango.Cambio += () =>
        {
            Rango = rango.Elegido;
            Pintar();
        };
        _pila.Add(rango);
        _pila.Add(Fichas(r));
        _pila.Add(Linea("Ánimo", "De 😞 (muy mal) a 😄 (muy bien).", Tema.Hoja, 1, 5, CatalogoDiario.CarasAnimo, r.Animo,
            v => $"{CatalogoDiario.CarasAnimo[(int)v - 1]} {CatalogoDiario.NombresAnimo[(int)v - 1]}"));
        _pila.Add(Linea("Ganas", "De 0 (ninguna) a 4 (muy fuertes).", Tema.Miel, 0, 4,
            ["0", "1", "2", "3", "4"], r.Ganas, v => $"{v:0} · {CatalogoDiario.NombresGanas[(int)v]}"));
        _pila.Add(Mapa(r));
        if (Patrones(r) is { } patrones) _pila.Add(patrones);
        _pila.Add(Historial(entradas));
        _pila.Add(Ciencia(svc.Plantados()));
    }

    private View TarjetaHoy(EntradaDiario? hoyEntrada, DateOnly hoy)
    {
        var caja = new VerticalStackLayout { Spacing = 8 };
        caja.Add(Tema.Texto("Hoy", 18, Tema.FuenteTitulo, Tema.Hoja));
        if (hoyEntrada is null)
        {
            caja.Add(Tema.Texto("¿Cómo te sentiste hoy? Toma un minuto: anotar el día te ayuda a ver tus patrones.", 15));
            var escribir = Tema.Boton(Iconos.Pluma, "Escribir la entrada de hoy", Tema.Miel);
            escribir.Clicked += async (_, _) => await Escribir(hoy);
            caja.Add(escribir);
        }
        else
        {
            caja.Add(Tema.Texto(ResumenCorto(hoyEntrada), 15));
            var editar = Tema.Boton("Editar la entrada de hoy", Tema.MusgoClaro, Tema.Hueso);
            editar.Clicked += async (_, _) => await Escribir(hoy);
            caja.Add(editar);
        }
        return Tema.Parcela(caja, borde: hoyEntrada is null ? Tema.Miel : null);
    }

    private static View Fichas(ResumenDiario r)
    {
        string tendencia = (r.GanasSemana, r.GanasSemanaAnterior) switch
        {
            ({ } a, { } b) when a < b - 0.25 => "↓ menos que la semana pasada",
            ({ } a, { } b) when a > b + 0.25 => "↑ más que la semana pasada",
            ({ }, { }) => "igual que la semana pasada",
            ({ }, null) => "esta semana",
            _ => "sin datos esta semana",
        };
        var grid = new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)],
            ColumnSpacing = 8,
        };
        grid.Add(Ficha(r.DiasEscritos.ToString(), $"días escritos de {r.Animo.Count}"), 0);
        grid.Add(Ficha(r.RachaEscritura.ToString(), r.RachaEscritura == 1 ? "día seguido" : "días seguidos"), 1);
        grid.Add(Ficha(r.GanasSemana is { } g ? g.ToString("0.0", Tema.Cultura) : "—", $"ganas promedio, {tendencia}"), 2);
        return grid;
    }

    private static View Ficha(string valor, string texto) => Tema.Parcela(new VerticalStackLayout
    {
        Spacing = 2,
        Children = { Tema.Texto(valor, 26, Tema.FuenteTitulo), Tema.Texto(texto, 12, color: Tema.HuesoTenue) },
    });

    private static View Linea(string titulo, string ayuda, Color color, double min, double max, string[] etiquetas,
        IReadOnlyList<PuntoDia> puntos, Func<double, string> leer)
    {
        var dibujo = new GraficaLinea(color, min, max, etiquetas) { Puntos = puntos };
        var vista = new GraphicsView { Drawable = dibujo, HeightRequest = 170 };
        var lectura = Tema.Texto("Toca la gráfica para ver un día.", 13, color: Tema.HuesoTenue);
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, e) =>
        {
            if (e.GetPosition(vista) is not { } p) return;
            int? i = GraficaLinea.IndiceEn((float)p.X, (float)vista.Width, puntos.Count);
            if (i is null) return;
            dibujo.Seleccionado = i;
            var d = puntos[i.Value];
            lectura.Text = $"{Tema.FechaCorta(d.Dia)}: {(d.Valor is { } v ? leer(v) : "sin registro")}";
            lectura.TextColor = Tema.Hueso;
            vista.Invalidate();
        };
        vista.GestureRecognizers.Add(toque);
        SemanticProperties.SetDescription(vista, $"Gráfica de {titulo.ToLower(Tema.Cultura)} por día. Los valores de cada día están en Entradas anteriores.");
        return Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 6,
            Children = { Tema.Texto(titulo, 18, Tema.FuenteTitulo, Tema.Hoja), Tema.Texto(ayuda, 13, color: Tema.HuesoTenue), vista, lectura },
        });
    }

    private static View Mapa(ResumenDiario r)
    {
        var caja = new VerticalStackLayout { Spacing = 6 };
        caja.Add(Tema.Texto("Síntomas", 18, Tema.FuenteTitulo, Tema.Hoja));
        if (r.Mapa.Count == 0)
        {
            caja.Add(Tema.Texto("No anotaste síntomas en este periodo.", 15));
            return Tema.Parcela(caja);
        }
        caja.Add(Tema.Texto(r.PorSemana ? "Promedio por semana. Más brillante = más fuerte." : "Un cuadro por día. Más brillante = más fuerte; un punto = no anotaste ese día.",
            13, color: Tema.HuesoTenue));
        var dibujo = new MapaSintomas { Filas = r.Mapa, Columnas = r.Columnas };
        var vista = new GraphicsView { Drawable = dibujo, HeightRequest = MapaSintomas.AltoPara(r.Mapa.Count) };
        var lectura = Tema.Texto("Toca un cuadro para leerlo.", 13, color: Tema.HuesoTenue);
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, e) =>
        {
            if (e.GetPosition(vista) is not { } p || dibujo.CeldaEn(p, (float)vista.Width) is not { } celda) return;
            dibujo.Seleccionada = celda;
            var fila = r.Mapa[celda.Fila];
            var dia = r.Columnas[celda.Columna];
            string cuando = r.PorSemana ? $"semana del {Tema.FechaCorta(dia)}" : Tema.FechaCorta(dia);
            string valor = fila.Valores[celda.Columna] is { } v
                ? (r.PorSemana ? $"{v.ToString("0.0", Tema.Cultura)} de 4 · " : "") + CatalogoDiario.NombresIntensidad[(int)Math.Round(v)]
                : "sin registro";
            lectura.Text = $"{fila.Nombre}, {cuando}: {valor}";
            lectura.TextColor = Tema.Hueso;
            vista.Invalidate();
        };
        vista.GestureRecognizers.Add(toque);
        SemanticProperties.SetDescription(vista, "Mapa de síntomas por día. Los valores de cada día están en Entradas anteriores.");
        caja.Add(vista);
        caja.Add(Leyenda());
        caja.Add(lectura);
        return Tema.Parcela(caja);
    }

    /// <summary>Leyenda de la escala de intensidad: cuadros con su palabra (el color nunca va solo).</summary>
    private static View Leyenda()
    {
        var fila = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        for (int i = 0; i < 5; i++)
        {
            fila.Add(new HorizontalStackLayout
            {
                Spacing = 4,
                Margin = new Thickness(0, 0, 10, 4),
                Children =
                {
                    new BoxView { Color = ColoresGrafica.Intensidad[i], WidthRequest = 14, HeightRequest = 14, CornerRadius = 3, VerticalOptions = LayoutOptions.Center },
                    Tema.Texto(CatalogoDiario.NombresIntensidad[i], 12, color: Tema.HuesoTenue),
                },
            });
        }
        return fila;
    }

    private static View? Patrones(ResumenDiario r)
    {
        if (r.Emociones.Count == 0 && r.Detonantes.Count == 0) return null;
        var caja = new VerticalStackLayout { Spacing = 6 };
        caja.Add(Tema.Texto("Lo que más se repite", 18, Tema.FuenteTitulo, Tema.Hoja));
        if (r.Emociones.Count > 0)
        {
            caja.Add(Tema.Texto("Emociones", 15, Tema.FuenteNegrita));
            caja.Add(Tema.Texto(string.Join("\n", r.Emociones.Select(x =>
                $"{CatalogoDiario.Emociones.Single(e => e.Item1 == x.Emocion).Item2} · {Veces(x.Veces)}")), 15));
        }
        if (r.Detonantes.Count > 0)
        {
            caja.Add(Tema.Texto("Detonantes", 15, Tema.FuenteNegrita));
            caja.Add(Tema.Texto(string.Join("\n", r.Detonantes.Select(x =>
                $"• {CatalogoDiario.Detonantes.Single(d => d.Item1 == x.Detonante).Item2} · {Veces(x.Veces)}")), 15));
            caja.Add(Tema.Texto("Idea: piensa qué harás la próxima vez que aparezca el primero (a quién llamar, a dónde ir, qué hacer en su lugar).",
                13, color: Tema.HuesoTenue));
        }
        return Tema.Parcela(caja);
    }

    private static string Veces(int n) => n == 1 ? "1 vez" : $"{n} veces";

    private View Historial(IReadOnlyList<EntradaDiario> entradas)
    {
        var caja = new VerticalStackLayout { Spacing = 10 };
        caja.Add(Tema.Texto("Entradas anteriores", 18, Tema.FuenteTitulo, Tema.Hoja));
        foreach (var e in entradas.Take(EntradasVisibles))
        {
            var renglon = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    Tema.Texto(Mayuscula(Tema.Fecha(e.Dia)), 15, Tema.FuenteNegrita),
                    Tema.Texto(ResumenCorto(e), 14, color: Tema.HuesoTenue),
                },
            };
            if (e.Nota is { } nota)
            {
                var texto = Tema.Texto(nota.Length > 140 ? nota[..140].TrimEnd() + "…" : nota, 14);
                texto.FontAttributes = FontAttributes.Italic;
                renglon.Add(texto);
            }
            var toque = new TapGestureRecognizer();
            var dia = e.Dia;
            toque.Tapped += async (_, _) => await Escribir(dia);
            renglon.GestureRecognizers.Add(toque);
            SemanticProperties.SetHint(renglon, "Toca para leer o editar");
            caja.Add(renglon);
            caja.Add(new BoxView { HeightRequest = 1, Color = ColoresGrafica.Rejilla });
        }
        if (entradas.Count > EntradasVisibles)
            caja.Add(Tema.Texto($"Y {entradas.Count - EntradasVisibles} entradas más antiguas.", 13, color: Tema.HuesoTenue));
        return Tema.Parcela(caja);
    }

    private static string Mayuscula(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Tema.Cultura) + s[1..];

    private static string ResumenCorto(EntradaDiario e)
    {
        var partes = new List<string>();
        if (e.Animo is { } a) partes.Add($"{CatalogoDiario.CarasAnimo[a - 1]} {CatalogoDiario.NombresAnimo[a - 1]}");
        if (e.Ganas is { } g) partes.Add($"ganas {g}/4");
        if (e.EmocionesO.Count > 0)
            partes.Add(string.Join(", ", CatalogoDiario.Emociones.Where(x => e.EmocionesO.Contains(x.Item1)).Take(3).Select(x => x.Item2)));
        int sintomas = e.SintomasO.Count(kv => kv.Value > 0);
        if (sintomas > 0) partes.Add(sintomas == 1 ? "1 síntoma" : $"{sintomas} síntomas");
        return partes.Count > 0 ? string.Join(" · ", partes) : "Solo una nota";
    }

    private static View Ciencia(IReadOnlySet<MalHabito> plantados)
    {
        var caja = new VerticalStackLayout { Spacing = 8 };
        caja.Add(Tema.Texto("Lo que dice la ciencia", 18, Tema.FuenteTitulo, Tema.Hoja));
        caja.Add(Tema.Texto(CatalogoDiario.FundamentoRegistro, 14));
        foreach (var h in plantados.OrderBy(h => h))
            if (CatalogoDiario.Fundamento(h) is { } f)
                caja.Add(Tema.Texto("• " + f, 14));
        caja.Add(Tema.Texto("Este registro no es un diagnóstico ni sustituye a un profesional de salud.", 12, color: Tema.HuesoTenue));
        return Tema.Parcela(caja);
    }

    // ---------- Escribir ----------

    private async Task Escribir(DateOnly dia)
    {
        if (_escribiendo) return;
        _escribiendo = true;
        try { await Navigation.PushModalAsync(new EntradaPage(dia, alerta => _ = AlGuardar(alerta))); }
        catch
        {
            _escribiendo = false;
            throw;
        }
    }

    private async Task AlGuardar(Alerta alerta)
    {
        switch (alerta)
        {
            case Alerta.Crisis:
            {
                var r = await DisplayActionSheetAsync(
                    "Gracias por escribirlo. No tienes que pasar por esto sin compañía: habla ahora con alguien.",
                    "Ahora no", null, "📞 Línea de la Vida (24 h)", "📞 Llamar al 911", "🆘 Mis jardineros");
                await Responder(r);
                break;
            }
            case Alerta.Urgente:
            {
                var r = await DisplayActionSheetAsync(
                    "Ver u oír cosas que no están, la confusión o las convulsiones durante una abstinencia pueden ser una urgencia. Llama al 911 o pide que te lleven a urgencias.",
                    "Ahora no", null, "📞 Llamar al 911", "🆘 Mis jardineros");
                await Responder(r);
                break;
            }
            case Alerta.MedicoHoy:
                if (await DisplayAlertAsync("Cuida tu cuerpo",
                        "Temblor, sudor o corazón acelerado fuertes pueden ser una abstinencia que necesita atención. Habla hoy con un médico; si empeora, llama al 911.",
                        "Pedir ayuda", "Entendido"))
                    await this.UnaVez(() => new AyudaPage());
                break;
        }
        await Avisos.AnunciarLogros(this);
    }

    private async Task Responder(string? opcion)
    {
        string? numero = opcion switch
        {
            "📞 Línea de la Vida (24 h)" => Telefono.LineaDeLaVida,
            "📞 Llamar al 911" => Telefono.Emergencias,
            _ => null,
        };
        if (numero is not null)
        {
            if (!Telefono.Marcar(numero))
                await DisplayAlertAsync("No se pudo", $"Este teléfono no puede abrir el marcador. Marca {numero} a mano.", "Entendido");
        }
        else if (opcion == "🆘 Mis jardineros")
        {
            await this.UnaVez(() => new AyudaPage());
        }
    }
}
