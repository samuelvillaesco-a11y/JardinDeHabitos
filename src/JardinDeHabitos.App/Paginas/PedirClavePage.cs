using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Pide la clave de la app (modal). Si se olvidó, se entra con el código de recuperación y se
/// crea una clave nueva en el momento. Tras varios fallos, hay que esperar.
/// </summary>
public sealed class PedirClavePage : ContentPage, IPrivada
{
    private readonly TaskCompletionSource<bool> _resultado = new();
    private readonly TipoCandado _tipo;
    private readonly ContentView _hueco = new();
    private readonly Label _indicacion = Tema.Texto("", 16);
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private readonly Button _olvide = Tema.Boton("", Colors.Transparent, Tema.HuesoTenue);
    private EntradaSecreto _entrada = null!;
    private readonly VerticalStackLayout _pila;
    private bool _modoRecuperacion;
    private bool _probando;
    private bool _cerrando;

    private PedirClavePage(string titulo, TipoCandado tipo)
    {
        _tipo = tipo;
        BackgroundColor = Tema.Musgo;
        _olvide.Clicked += (_, _) => CambiarModo(!_modoRecuperacion);
        var cancelar = Tema.Boton("Cancelar", Colors.Transparent, Tema.HuesoTenue);
        cancelar.Clicked += async (_, _) => await Cerrar(false);

        _pila = new VerticalStackLayout
        {
            Padding = new Thickness(24, 48, 24, 24),
            Spacing = 14,
            Children = { Tema.Texto("🔒", 44, Tema.FuenteTitulo), Tema.Titulo(titulo, 26), _indicacion, _hueco, _error, _olvide, cancelar },
        };
        CambiarModo(false);
    }

    /// <summary>Muestra la pantalla y espera: true si el PIN, patrón o clave (o la recuperación) fue correcto.</summary>
    public static async Task<bool> MostrarAsync(INavigation nav, string titulo, TipoCandado tipo)
    {
        var p = new PedirClavePage(titulo, tipo);
        await nav.PushModalAsync(p);
        return await p._resultado.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Actualizar();
        _entrada.Enfocar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Si la cerraron por fuera (p. ej. al volver de segundo plano), quien espera recibe "no".
        if (_cerrando) return;
        Dispatcher.Dispatch(() =>
        {
            if (!_cerrando && !Navigation.ModalStack.Contains(this)) _resultado.TrySetResult(false);
        });
        Pantalla.Actualizar();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Cerrar(false);
        return true;
    }

    private void CambiarModo(bool recuperacion)
    {
        _modoRecuperacion = recuperacion;
        _error.Text = "";
        _entrada = recuperacion ? new EntradaSecreto(TipoCandado.Clave, textoVisible: true) : new EntradaSecreto(_tipo);
        _entrada.Listo += async s => await Probar(s);
        _hueco.Content = _entrada;
        // Con patrón no hay ScrollView: en Android se roba el arrastre del dedo aunque no se desplace.
        bool patron = !recuperacion && _tipo == TipoCandado.Patron;
        if (patron && Content != _pila)
        {
            if (Content is ScrollView s) s.Content = null;
            Content = _pila;
        }
        else if (!patron && Content is not ScrollView) { Content = null; Content = new ScrollView { Content = _pila }; }
        string que = _tipo switch { TipoCandado.Pin => "PIN", TipoCandado.Patron => "patrón", _ => "clave" };
        _indicacion.Text = recuperacion
            ? "Escribe el código de recuperación que anotaste."
            : _tipo switch
            {
                TipoCandado.Pin => "Escribe tu PIN.",
                TipoCandado.Patron => "Dibuja tu patrón.",
                _ => "Escribe tu clave.",
            };
        _olvide.Text = recuperacion ? $"Volver a mi {que}" : $"Olvidé mi {que}";
        _entrada.Enfocar();
    }

    private async Task Probar(string texto)
    {
        if (_probando) return;
        _probando = true;
        _error.Text = "Revisando…";
        try
        {
            var r = _modoRecuperacion
                ? await Seguridad.ProbarRecuperacionAsync(texto)
                : await Seguridad.ProbarClaveAsync(texto);
            _entrada.Limpiar();
            switch (r)
            {
                case ResultadoClave.Correcta when _modoRecuperacion:
                    // El código se gasta: hay que elegir candado nuevo (y sale un código nuevo).
                    await Navigation.PushModalAsync(new CrearClavePage(CrearClavePage.Motivo.Recuperacion, async () =>
                    {
                        await Navigation.PopModalAsync();
                        await Cerrar(true);
                    }));
                    break;
                case ResultadoClave.Correcta:
                    await Cerrar(true);
                    break;
                case ResultadoClave.SoloRecuperacion:
                    CambiarModo(true);
                    _error.Text = "Demasiados intentos fallidos. Para entrar, usa tu código de recuperación.";
                    break;
                case ResultadoClave.Esperar:
                    var espera = await Seguridad.EsperaRestanteAsync();
                    _error.Text = $"Demasiados intentos. Espera {Math.Ceiling(espera.TotalMinutes)} min y vuelve a intentar.";
                    break;
                default:
                    _error.Text = _modoRecuperacion ? "Ese código no es correcto." : _tipo switch
                    {
                        TipoCandado.Pin => "Ese PIN no es correcto.",
                        TipoCandado.Patron => "Ese patrón no es correcto.",
                        _ => "Esa clave no es correcta.",
                    };
                    break;
            }
        }
        finally { _probando = false; }
    }

    private async Task Cerrar(bool ok)
    {
        if (_resultado.Task.IsCompleted || _cerrando) return;
        _cerrando = true;
        if (Navigation.ModalStack.LastOrDefault() == this) await Navigation.PopModalAsync();
        _resultado.TrySetResult(ok);
    }
}
