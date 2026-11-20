using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Panel de seguridad (se entra verificando al dueño). Cada seguro se enciende o apaga solo
/// después de verificar otra vez: clave de la app, bloqueo del teléfono, o ambos.
/// </summary>
public sealed class SeguridadPage : ContentPage, IPrivada, IConLlave
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
        Pantalla.Actualizar();
        await Pintar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Pantalla.Actualizar();
    }

    private async Task Pintar()
    {
        var tipo = await Seguridad.TipoAsync();
        bool propio = tipo != TipoCandado.Telefono;
        _pila.Clear();
        _pila.Add(Tema.Titulo("🔒 Seguridad", 30));

        _pila.Add(Tema.Texto("Tu candado", 19, Tema.FuenteTitulo, Tema.Hoja));
        _pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Tema.Texto(Seguridad.Nombre(tipo), 17, Tema.FuenteNegrita),
                Tema.Texto(propio
                    ? "Se guarda solo una huella cifrada (PBKDF2 con sal) en el almacenamiento seguro del teléfono."
                    : "Se abre con la misma huella, PIN o patrón de tu teléfono.", 14, color: Tema.HuesoTenue),
            },
        }));
        var cambiar = Tema.Boton("Cambiar candado", Tema.MusgoClaro, Tema.Hueso);
        cambiar.Clicked += async (_, _) => await CambiarCandado();
        _pila.Add(cambiar);

        _pila.Add(Tema.Texto("Seguros", 19, Tema.FuenteTitulo, Tema.Hoja));
        _pila.Add(Tema.Texto("Toca un seguro para encenderlo o apagarlo. Cada cambio pide confirmar que eres tú.", 15, color: Tema.HuesoTenue));
        _pila.Add(Fila(Seguro.Logros, "Seguro de logros", "Pide verificación para abrir tu estante de trofeos."));
        _pila.Add(Fila(Seguro.Blog, "Seguro de blog botánico", "Pide verificación para abrir tu diario y tu registro de emociones."));
        _pila.Add(Fila(Seguro.General, "Seguro general", "Pide verificación cada vez que abres la app o vuelves a ella, y la oculta en capturas y en apps recientes."));
        _pila.Add(Fila(Seguro.DobleVerificacion, "Doble verificación",
            propio
                ? "Pide tu PIN o patrón de la app y además la huella, PIN o patrón del teléfono."
                : "Solo con un PIN o patrón propio.",
            habilitado: propio));

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
            // Al cerrarse la verificación la página ya se repintó con el estado viejo: se repinta de nuevo.
            await Pintar();
        }
        finally { _ocupado = false; }
    }

    private async Task CambiarCandado()
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            // Cambiar el candado pide el actual.
            if (!await Seguridad.VerificarAsync(this, "Confirma que eres tú")) return;
            await Navigation.PushModalAsync(new CrearClavePage(CrearClavePage.Motivo.Ajustes, async () =>
            {
                await Navigation.PopModalAsync();
                await Pintar();
            }));
        }
        finally { _ocupado = false; }
    }
}
