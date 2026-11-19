using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Pide la clave de la app (modal). Si se olvidó, se entra con el código de recuperación y se
/// crea una clave nueva en el momento. Tras varios fallos, hay que esperar.
/// </summary>
public sealed class PedirClavePage : ContentPage
{
    private readonly TaskCompletionSource<bool> _resultado = new();
    private readonly Entry _entrada = new()
    {
        IsPassword = true,
        IsSpellCheckEnabled = false,
        IsTextPredictionEnabled = false,
        FontFamily = Tema.Fuente,
        FontSize = 20,
        TextColor = Tema.Hueso,
        PlaceholderColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
        MaxLength = ClaveLocal.LargoMaximo,
    };
    private readonly Label _indicacion = Tema.Texto("", 16);
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private readonly Button _entrar = Tema.Boton("Entrar", Tema.Miel);
    private readonly Button _olvide = Tema.Boton("Olvidé mi clave", Colors.Transparent, Tema.HuesoTenue);
    private bool _modoRecuperacion;
    private bool _probando;

    private PedirClavePage(string titulo)
    {
        BackgroundColor = Tema.Musgo;
        _entrar.Clicked += async (_, _) => await Probar();
        _entrada.Completed += async (_, _) => await Probar();
        _olvide.Clicked += (_, _) => CambiarModo(!_modoRecuperacion);
        var cancelar = Tema.Boton("Cancelar", Colors.Transparent, Tema.HuesoTenue);
        cancelar.Clicked += async (_, _) => await Cerrar(false);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 64, 24, 24),
                Spacing = 14,
                Children = { Tema.Texto("🔒", 48, Tema.FuenteTitulo), Tema.Titulo(titulo, 26), _indicacion, _entrada, _error, _entrar, _olvide, cancelar },
            },
        };
        CambiarModo(false);
    }

    /// <summary>Muestra la pantalla y espera: true si la clave (o la recuperación) fue correcta.</summary>
    public static async Task<bool> MostrarAsync(INavigation nav, string titulo)
    {
        var p = new PedirClavePage(titulo);
        await nav.PushModalAsync(p);
        return await p._resultado.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Privada(true);
        _entrada.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Pantalla.Privada(Seguridad.PantallaDebeSerPrivada);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Cerrar(false);
        return true;
    }

    private void CambiarModo(bool recuperacion)
    {
        _modoRecuperacion = recuperacion;
        _entrada.Text = "";
        _error.Text = "";
        _entrada.IsPassword = !recuperacion;
        _entrada.Keyboard = recuperacion ? Keyboard.Plain : Keyboard.Default;
        _entrada.Placeholder = recuperacion ? "XXXX-XXXX-XXXX-XXXX" : "Tu clave";
        _indicacion.Text = recuperacion
            ? "Escribe el código de recuperación que anotaste al crear tu clave."
            : "Escribe la clave de la app.";
        _olvide.Text = recuperacion ? "Volver a la clave" : "Olvidé mi clave";
    }

    private async Task Probar()
    {
        if (_probando) return;
        _probando = true;
        _entrar.IsEnabled = false;
        _error.Text = "Revisando…";
        try
        {
            var r = _modoRecuperacion
                ? await Seguridad.ProbarRecuperacionAsync(_entrada.Text)
                : await Seguridad.ProbarClaveAsync(_entrada.Text);
            _entrada.Text = "";
            switch (r)
            {
                case ResultadoClave.Correcta when _modoRecuperacion:
                    // El código se gasta: hay que crear clave nueva (y sale un código nuevo).
                    await Navigation.PushModalAsync(new CrearClavePage(CrearClavePage.Motivo.Recuperacion, async () =>
                    {
                        await Navigation.PopModalAsync();
                        await Cerrar(true);
                    }));
                    break;
                case ResultadoClave.Correcta:
                    await Cerrar(true);
                    break;
                case ResultadoClave.Esperar:
                    var espera = await Seguridad.EsperaRestanteAsync();
                    _error.Text = $"Demasiados intentos. Espera {Math.Ceiling(espera.TotalMinutes)} min y vuelve a intentar.";
                    break;
                default:
                    _error.Text = _modoRecuperacion ? "Ese código no es correcto." : "Esa clave no es correcta.";
                    break;
            }
        }
        finally
        {
            _probando = false;
            _entrar.IsEnabled = true;
        }
    }

    private async Task Cerrar(bool ok)
    {
        if (_resultado.Task.IsCompleted) return;
        if (Navigation.ModalStack.LastOrDefault() == this) await Navigation.PopModalAsync();
        _resultado.TrySetResult(ok);
    }
}
