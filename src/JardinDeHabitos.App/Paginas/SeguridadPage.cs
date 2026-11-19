using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Panel de seguridad (se entra verificando al dueño). Cada seguro se enciende o apaga solo
/// después de verificar otra vez: clave de la app, bloqueo del teléfono, o ambos.
/// </summary>
public sealed class SeguridadPage : ContentPage
{
    private readonly VerticalStackLayout _pila = new() { Padding = new Thickness(18, 20), Spacing = 14 };
    private bool _ocupado;

    public SeguridadPage()
    {
        Title = "Seguridad";
        BackgroundColor = Tema.Musgo;
        Content = new ScrollView { Content = _pila };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Privada(true);
        await Pintar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Pantalla.Privada(Seguridad.PantallaDebeSerPrivada);
    }

    private async Task Pintar()
    {
        bool tieneClave = await Seguridad.TieneClaveAsync();
        _pila.Clear();
        _pila.Add(Tema.Titulo("🔒 Seguridad", 30));
        _pila.Add(Tema.Texto("Toca un seguro para encenderlo o apagarlo. Cada cambio pide confirmar que eres tú.", 15, color: Tema.HuesoTenue));
        _pila.Add(Fila(Seguro.Logros, "Seguro de logros", "Pide verificación para abrir tu estante de trofeos."));
        _pila.Add(Fila(Seguro.Blog, "Seguro de blog botánico", "Pide verificación para abrir tu diario y tu registro de emociones."));
        _pila.Add(Fila(Seguro.General, "Seguro general", "Pide verificación cada vez que abres la app o vuelves a ella, y la oculta en capturas y en apps recientes."));
        _pila.Add(Fila(Seguro.DobleVerificacion, "Doble verificación",
            tieneClave
                ? "Pide tu clave de la app y además la huella, PIN o patrón del teléfono."
                : "Primero crea una clave de la app.",
            habilitado: tieneClave));

        _pila.Add(Tema.Texto("Clave de la app", 19, Tema.FuenteTitulo, Tema.Hoja));
        if (tieneClave)
        {
            _pila.Add(Tema.Texto("Tienes una clave. Solo se guarda una huella cifrada de ella (PBKDF2 con sal).", 14, color: Tema.HuesoTenue));
            var cambiar = Tema.Boton("Cambiar clave", Tema.MusgoClaro, Tema.Hueso);
            cambiar.Clicked += async (_, _) => await CrearClave();
            var quitar = Tema.Boton("Quitar clave", Colors.Transparent, Tema.HuesoTenue);
            quitar.Clicked += async (_, _) => await QuitarClave();
            _pila.Add(cambiar);
            _pila.Add(quitar);
        }
        else
        {
            _pila.Add(Tema.Texto("Sin clave, se usa el bloqueo del teléfono (huella, PIN o patrón).", 14, color: Tema.HuesoTenue));
            var crear = Tema.Boton("Crear clave", Tema.Miel);
            crear.Clicked += async (_, _) => await CrearClave();
            _pila.Add(crear);
        }

        _pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Tema.Texto("Cómo cuidamos tus datos", 16, Tema.FuenteNegrita, Tema.Hoja),
                Tema.Texto("• Todo se guarda solo en este teléfono. La app no tiene permiso de internet.", 14),
                Tema.Texto("• No hay cuentas, correo ni inicio de sesión de ninguna empresa.", 14),
                Tema.Texto("• Los respaldos en la nube y el paso a otro teléfono están desactivados para esta app.", 14),
                Tema.Texto("• Si borras la app, se borra todo.", 14),
            },
        }));
    }

    private View Fila(Seguro seguro, string titulo, string descripcion, bool habilitado = true)
    {
        // El switch solo muestra el estado: el cambio se hace al tocar la fila, después de verificar.
        var sw = new Switch { IsToggled = Seguridad.Activo(seguro), OnColor = Tema.Hoja, ThumbColor = Tema.Hueso, InputTransparent = true };
        var fila = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            ColumnSpacing = 12,
        };
        fila.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { Tema.Texto(titulo, 17, Tema.FuenteNegrita), Tema.Texto(descripcion, 14, color: Tema.HuesoTenue) },
        }, 0);
        sw.VerticalOptions = LayoutOptions.Center;
        fila.Add(sw, 1);

        var parcela = Tema.Parcela(fila);
        parcela.Opacity = habilitado ? 1 : 0.5;
        SemanticProperties.SetDescription(parcela, $"{titulo}: {(sw.IsToggled ? "encendido" : "apagado")}");
        if (habilitado)
        {
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await Alternar(seguro, titulo, sw);
            parcela.GestureRecognizers.Add(toque);
        }
        return parcela;
    }

    private async Task Alternar(Seguro seguro, string titulo, Switch sw)
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            bool nuevo = !Seguridad.Activo(seguro);
            string accion = nuevo ? "Encender" : "Apagar";
            if (!await Seguridad.VerificarAsync(this, $"{accion} {titulo.ToLower(Tema.Cultura)}")) return;

            if (seguro == Seguro.DobleVerificacion && nuevo && await Bloqueo.PedirAsync("Doble verificación", "Comprueba el bloqueo del teléfono") != Bloqueo.Resultado.Abierto)
            {
                await DisplayAlertAsync("Falta el bloqueo del teléfono",
                    "Para la doble verificación, el teléfono necesita un PIN, patrón o huella y que lo confirmes.", "Entendido");
                return;
            }
            Seguridad.Cambiar(seguro, nuevo);
            sw.IsToggled = nuevo;
        }
        finally { _ocupado = false; }
    }

    private async Task CrearClave()
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            // Cambiar una clave existente pide la actual (o el bloqueo, si no hay).
            if (!await Seguridad.VerificarAsync(this, "Confirma que eres tú")) return;
            await Navigation.PushModalAsync(new CrearClavePage(CrearClavePage.Motivo.Ajustes, async () =>
            {
                await Navigation.PopModalAsync();
                await Pintar();
            }));
        }
        finally { _ocupado = false; }
    }

    private async Task QuitarClave()
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            if (!await Seguridad.VerificarAsync(this, "Quitar la clave de la app")) return;
            if (!await DisplayAlertAsync("¿Quitar la clave?", "Se usará solo el bloqueo del teléfono. La doble verificación se apaga.", "Quitar", "Cancelar")) return;
            await Seguridad.QuitarClaveAsync();
            await Pintar();
        }
        finally { _ocupado = false; }
    }
}
