using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Escribir (o editar) el día en el blog botánico: ánimo, ganas, emociones, detonantes, síntomas y una nota.
/// Todo es opcional. Se abre como modal encima del blog; al volver de segundo plano se cierra con él.
/// </summary>
public sealed class EntradaPage : ContentPage, IPrivada, IConLlave
{
    private readonly DateOnly _dia;
    private readonly Action<Alerta> _alGuardar;
    private readonly Escala _animo;
    private readonly Escala _ganas;
    private readonly Chips<Emocion> _emociones;
    private readonly Chips<Detonante> _detonantes;
    private readonly List<(Sintoma Sintoma, Escala Escala)> _sintomas = [];
    private readonly Chips<Sintoma> _alarmas;
    private readonly Editor _nota;
    private readonly Label _error = Tema.Texto("", 14, Tema.FuenteNegrita, Tema.Miel);
    private bool _guardando;

    public EntradaPage(DateOnly dia, Action<Alerta> alGuardar)
    {
        _dia = dia;
        _alGuardar = alGuardar;
        BackgroundColor = Tema.Musgo;
        var svc = Servicios.Jardin;
        var e = svc.Entrada(dia);
        var plantados = svc.Plantados();

        _animo = new Escala("Ánimo", CatalogoDiario.CarasAnimo, CatalogoDiario.NombresAnimo, 1, e?.Animo, 48);
        _ganas = new Escala("Ganas", ["0", "1", "2", "3", "4"], CatalogoDiario.NombresGanas, 0, e?.Ganas);
        _emociones = new Chips<Emocion>(CatalogoDiario.Emociones, maximo: CatalogoDiario.Emociones.Count, iniciales: e?.EmocionesO);
        _detonantes = new Chips<Detonante>(CatalogoDiario.Detonantes, maximo: CatalogoDiario.Detonantes.Count, iniciales: e?.DetonantesO);

        var sintomas = new VerticalStackLayout { Spacing = 14 };
        var aplicables = CatalogoDiario.Para(plantados).ToList();
        foreach (var info in aplicables.Where(s => !s.Alarma))
        {
            var escala = new Escala(info.Nombre, ["0", "1", "2", "3", "4"], CatalogoDiario.NombresIntensidad, 0,
                e?.SintomasO.TryGetValue(info.Sintoma, out int v) == true ? v : null, 40);
            _sintomas.Add((info.Sintoma, escala));
            sintomas.Add(new VerticalStackLayout { Spacing = 6, Children = { Tema.Texto(info.Nombre, 15, Tema.FuenteNegrita), escala } });
        }
        var alarmas = aplicables.Where(s => s.Alarma).ToList();
        _alarmas = new Chips<Sintoma>(alarmas.Select(s => (s.Sintoma, s.Nombre)), maximo: alarmas.Count,
            iniciales: alarmas.Where(s => e?.SintomasO.GetValueOrDefault(s.Sintoma) > 0).Select(s => s.Sintoma));

        _nota = new Editor
        {
            Text = e?.Nota ?? "",
            Placeholder = "¿Qué pasó hoy? ¿Qué te ayudó? ¿Qué harías distinto?",
            PlaceholderColor = Tema.HuesoTenue,
            TextColor = Tema.Hueso,
            FontFamily = Tema.Fuente,
            FontSize = 16,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 120,
            MaxLength = EntradaDiario.LargoMaximoNota,
            Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeSentence | KeyboardFlags.Spellcheck),
        };

        var guardar = Tema.Boton("Guardar", Tema.Hoja);
        guardar.Clicked += async (_, _) => await Guardar();
        var cancelar = Tema.Boton("Cancelar", Tema.MusgoClaro, Tema.Hueso);
        cancelar.Clicked += async (_, _) => await Salir();

        var pila = new VerticalStackLayout
        {
            Padding = new Thickness(18, 24, 18, 32),
            Spacing = 14,
            Children =
            {
                Tema.Titulo(e is null ? "Nueva entrada" : "Editar entrada", 28),
                Tema.Texto(Primera(Tema.Fecha(dia)), 16, color: Tema.HuesoTenue),
                Tema.Texto("Todo es opcional: responde solo lo que quieras.", 14, color: Tema.HuesoTenue),
                Seccion("¿Cómo estuvo tu ánimo?", null, _animo),
                Seccion("¿Qué tantas ganas tuviste?", "Las ganas de caer en el hábito, de 0 (ninguna) a 4 (muy fuertes).", _ganas),
                Seccion("¿Qué sentiste?", "Elige todas las que quieras.", _emociones),
                Seccion("¿Algo te detonó las ganas?", "Reconocer los detonantes ayuda a prepararte para la próxima vez.", _detonantes),
                Seccion("¿Cómo se sintió tu cuerpo?", "De 0 (nada) a 4 (muy fuerte).", sintomas),
            },
        };
        if (alarmas.Count > 0)
            pila.Add(Seccion("Señales de alarma", "Si te pasó alguna, márcala: te diremos a dónde pedir ayuda de inmediato.", _alarmas));
        pila.Add(Seccion("Tu nota", null, _nota));
        pila.Add(_error);
        pila.Add(guardar);
        pila.Add(cancelar);
        if (e is not null)
        {
            var borrar = Tema.Boton("Borrar esta entrada", Colors.Transparent, Tema.HuesoTenue);
            borrar.Clicked += async (_, _) => await Borrar();
            pila.Add(borrar);
        }
        Content = new ScrollView { Content = pila };
    }

    private static string Primera(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Tema.Cultura) + s[1..];

    private static View Seccion(string titulo, string? ayuda, View contenido)
    {
        var caja = new VerticalStackLayout { Spacing = 8 };
        caja.Add(Tema.Texto(titulo, 18, Tema.FuenteTitulo, Tema.Hoja));
        if (ayuda is not null) caja.Add(Tema.Texto(ayuda, 14, color: Tema.HuesoTenue));
        caja.Add(contenido);
        return Tema.Parcela(caja);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Actualizar();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Salir();
        return true;
    }

    private async Task Guardar()
    {
        if (_guardando) return;
        _guardando = true;
        try
        {
            var sintomas = _sintomas.Where(s => s.Escala.Valor > 0).ToDictionary(s => s.Sintoma, s => s.Escala.Valor!.Value);
            foreach (var a in _alarmas.Elegidos) sintomas[a] = 1;
            var entrada = new EntradaDiario(_dia, _animo.Valor, _ganas.Valor,
                _emociones.Elegidos.ToHashSet(), _detonantes.Elegidos.ToHashSet(), sintomas, _nota.Text);
            Alerta alerta;
            try { alerta = Servicios.Jardin.GuardarEntrada(entrada); }
            catch (ArgumentException ex)
            {
                _error.Text = ex.Message;
                return;
            }
            await Navigation.PopModalAsync();
            _alGuardar(alerta);
        }
        finally { _guardando = false; }
    }

    private async Task Borrar()
    {
        if (!await DisplayAlertAsync("¿Borrar esta entrada?", "Se borra del teléfono y no se puede recuperar.", "Borrar", "Conservar")) return;
        Servicios.Jardin.EliminarEntrada(_dia);
        await Navigation.PopModalAsync();
        _alGuardar(Alerta.Ninguna);
    }

    private async Task Salir()
    {
        if (Navigation.ModalStack.LastOrDefault() == this) await Navigation.PopModalAsync();
    }
}
