using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Elegir el candado: el mismo bloqueo del teléfono (por defecto), un PIN propio o un patrón propio.
/// Al crear un PIN o patrón se pide dos veces y se muestra UNA vez el código de recuperación.
/// </summary>
public sealed class CrearClavePage : ContentPage, IPrivada
{
    public enum Motivo { PrimeraVez, Ajustes, Recuperacion }

    private readonly Motivo _motivo;
    private readonly Func<Task> _alTerminar;
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private bool _guardando;

    public CrearClavePage(Motivo motivo, Func<Task> alTerminar)
    {
        _motivo = motivo;
        _alTerminar = alTerminar;
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);
        MostrarOpciones();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Actualizar();
    }

    // La primera vez y al recuperar, "atrás" no se salta la pantalla.
    protected override bool OnBackButtonPressed() => _motivo != Motivo.Ajustes;

    private void MostrarOpciones()
    {
        var pila = new VerticalStackLayout { Padding = new Thickness(20, 48, 20, 28), Spacing = 14 };
        pila.Add(Tema.Titulo(_motivo == Motivo.PrimeraVez ? "Bienvenido a tu jardín" : "Elige tu candado", 30));
        pila.Add(Tema.Texto(_motivo switch
        {
            Motivo.PrimeraVez => "Tu estante de logros y tu blog botánico quedan bajo llave. ¿Con qué quieres abrirlos? Puedes cambiarlo cuando quieras en Ajustes › Seguridad.",
            Motivo.Recuperacion => "Tu código de recuperación funcionó. Elige un candado nuevo.",
            _ => "Elige con qué se abre lo que pongas bajo llave.",
        }, 16));
        pila.Add(Opcion("📱", "Usar el bloqueo de mi teléfono", "La misma huella, PIN o patrón con que desbloqueas el celular. Recomendado.", TipoCandado.Telefono));
        pila.Add(Opcion("🔢", "Crear un PIN propio", $"De {ClaveLocal.PinMinimo} a {ClaveLocal.PinMaximo} números, distinto al del teléfono.", TipoCandado.Pin));
        pila.Add(Opcion("✳️", "Dibujar un patrón propio", $"Une al menos {ClaveLocal.PatronMinimo} de los 9 puntos.", TipoCandado.Patron));
        pila.Add(_error);
        if (_motivo == Motivo.PrimeraVez)
        {
            var despues = Tema.Boton("Decidir después", Colors.Transparent, Tema.HuesoTenue);
            despues.Clicked += async (_, _) => await _alTerminar();
            pila.Add(despues);
        }
        if (_motivo == Motivo.Ajustes)
        {
            var cancelar = Tema.Boton("Cancelar", Colors.Transparent, Tema.HuesoTenue);
            cancelar.Clicked += async (_, _) => await Navigation.PopModalAsync();
            pila.Add(cancelar);
        }
        pila.Add(Privacidad());
        Content = new ScrollView { Content = pila };
    }

    private View Opcion(string icono, string titulo, string detalle, TipoCandado tipo)
    {
        var fila = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 14 };
        fila.Add(Tema.Texto(icono, 30), 0);
        fila.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children = { Tema.Texto(titulo, 17, Tema.FuenteNegrita), Tema.Texto(detalle, 14, color: Tema.HuesoTenue) },
        }, 1);
        var parcela = Tema.Parcela(fila);
        SemanticProperties.SetDescription(parcela, $"{titulo}. {detalle}");
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await Elegir(tipo);
        parcela.GestureRecognizers.Add(toque);
        return parcela;
    }

    private async Task Elegir(TipoCandado tipo)
    {
        if (_guardando) return;
        _error.Text = "";
        if (tipo != TipoCandado.Telefono)
        {
            PedirSecreto(tipo, primero: null);
            return;
        }
        if (!Bloqueo.TelefonoTieneBloqueo)
        {
            _error.Text = "Tu teléfono no tiene huella, PIN ni patrón. Actívalo en los ajustes del teléfono o crea un PIN o patrón propio aquí.";
            return;
        }
        _guardando = true;
        try
        {
            await Seguridad.UsarBloqueoDelTelefonoAsync();
            await _alTerminar();
        }
        finally { _guardando = false; }
    }

    /// <summary>Pide el PIN o patrón; la segunda vez compara con el primero.</summary>
    private void PedirSecreto(TipoCandado tipo, string? primero)
    {
        var entrada = new EntradaSecreto(tipo);
        var aviso = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
        string que = tipo == TipoCandado.Pin ? "PIN" : "patrón";
        entrada.Listo += async secreto =>
        {
            if (primero is null)
            {
                if (ClaveLocal.Problema(tipo, secreto) is { } p)
                {
                    aviso.Text = p;
                    entrada.Limpiar();
                    return;
                }
                PedirSecreto(tipo, secreto);
                return;
            }
            if (secreto != primero)
            {
                aviso.Text = tipo == TipoCandado.Pin ? "Los PIN no coinciden. Empieza de nuevo." : "Los patrones no coinciden. Empieza de nuevo.";
                await Task.Delay(1200);
                PedirSecreto(tipo, null);
                return;
            }
            await Guardar(tipo, secreto);
        };
        var volver = Tema.Boton("Elegir otro candado", Colors.Transparent, Tema.HuesoTenue);
        volver.Clicked += (_, _) => MostrarOpciones();

        var pila = new VerticalStackLayout
            {
                Padding = new Thickness(20, 48, 20, 28),
                Spacing = 16,
                Children =
                {
                    Tema.Titulo(primero is null ? $"Crea tu {que}" : $"Repite tu {que}", 28),
                    Tema.Texto(tipo == TipoCandado.Pin
                        ? $"De {ClaveLocal.PinMinimo} a {ClaveLocal.PinMaximo} números. Evita fechas obvias y secuencias como 1234."
                        : $"Une al menos {ClaveLocal.PatronMinimo} puntos sin levantar el dedo.", 15, color: Tema.HuesoTenue),
                    entrada,
                    aviso,
                    volver,
                },
            };
        // Con patrón no hay ScrollView: en Android se roba el arrastre del dedo aunque no se desplace.
        Content = tipo == TipoCandado.Patron ? pila : new ScrollView { Content = pila };
    }

    private async Task Guardar(TipoCandado tipo, string secreto)
    {
        if (_guardando) return;
        _guardando = true;
        try
        {
            var codigo = await Seguridad.CrearSecretoAsync(tipo, secreto);
            MostrarCodigo(codigo);
        }
        catch (Exception)
        {
            MostrarOpciones();
            _error.Text = "No se pudo guardar en el almacenamiento seguro del teléfono. Intenta de nuevo.";
        }
        finally { _guardando = false; }
    }

    private void MostrarCodigo(string codigo)
    {
        var listo = Tema.Boton("Ya lo anoté", Tema.Miel);
        listo.Clicked += async (_, _) =>
        {
            listo.IsEnabled = false;
            await _alTerminar();
        };
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 48, 20, 28),
                Spacing = 16,
                Children =
                {
                    Tema.Titulo("Tu código de recuperación", 28),
                    Tema.Texto("Anótalo en papel y guárdalo. Si olvidas tu PIN o patrón, es la única forma de entrar. No se volverá a mostrar.", 16),
                    Tema.Parcela(Tema.Texto(codigo, 26, Tema.FuenteTitulo, Tema.Miel), Tema.TierraOscura, Tema.Miel),
                    Tema.Texto("No lo guardes en una captura de pantalla: esta pantalla no permite capturas.", 14, color: Tema.HuesoTenue),
                    listo,
                },
            },
        };
    }

    private static View Privacidad() => Tema.Parcela(new VerticalStackLayout
    {
        Spacing = 6,
        Children =
        {
            Tema.Texto("Tus datos se quedan aquí", 16, Tema.FuenteNegrita, Tema.Hoja),
            Tema.Texto("• Todo lo que registres se guarda cifrado y solo en este teléfono.", 14),
            Tema.Texto("• Sin cuentas de Google, Facebook ni de ninguna empresa, y sin correo.", 14),
            Tema.Texto("• La app no tiene servidores ni usa internet.", 14),
            Tema.Texto("• Tu PIN o patrón no se guarda: solo una huella cifrada de él.", 14),
        },
    });
}
