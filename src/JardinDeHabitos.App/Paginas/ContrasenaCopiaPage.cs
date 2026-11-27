using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Pide la contraseña de una copia del jardín (dos veces si es nueva). Modal y privada: nada de lo
/// que se escribe aparece en capturas. La contraseña no se guarda en ningún lado.
/// </summary>
public sealed class ContrasenaCopiaPage : ContentPage, IPrivada
{
    private readonly TaskCompletionSource<string?> _resultado = new();
    private readonly Entry _primera;
    private readonly Entry? _segunda;
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);

    public static async Task<string?> PedirAsync(INavigation navegacion, bool nueva)
    {
        var pagina = new ContrasenaCopiaPage(nueva);
        await navegacion.PushModalAsync(pagina);
        var contrasena = await pagina._resultado.Task;
        // El cierre por ausencia larga ya pudo haber quitado la página.
        if (navegacion.ModalStack.Contains(pagina)) await navegacion.PopModalAsync();
        return contrasena;
    }

    private ContrasenaCopiaPage(bool nueva)
    {
        BackgroundColor = Tema.Musgo;
        _primera = Campo(nueva ? "Contraseña nueva" : "Contraseña de la copia");
        if (nueva) _segunda = Campo("Escríbela otra vez");
        _error.IsVisible = false;

        var continuar = Tema.Boton(nueva ? "Guardar copia" : "Abrir copia", Tema.Hoja, Tema.Musgo);
        continuar.Clicked += (_, _) => Continuar(nueva);
        var cancelar = Tema.Boton("Cancelar", Colors.Transparent, Tema.Hueso);
        cancelar.Clicked += (_, _) => _resultado.TrySetResult(null);

        var pila = new VerticalStackLayout
        {
            Padding = new Thickness(18, 28),
            Spacing = 14,
            Children =
            {
                Tema.Titulo(nueva ? "Contraseña de la copia" : "Abrir una copia", 28),
                Tema.Texto(nueva
                    ? $"Elige una contraseña de al menos {Respaldo.MinimoContrasena} caracteres. Una frase corta funciona bien, como \"girasol de mayo 7\"."
                    : "Escribe la contraseña que elegiste al guardar la copia.", 15, color: Tema.HuesoTenue),
                _primera,
            },
        };
        if (_segunda is not null)
        {
            pila.Add(_segunda);
            pila.Add(Tema.Texto("Anótala en un lugar seguro. Si la olvidas, nadie puede abrir la copia, ni tú.", 15, Tema.FuenteNegrita));
        }
        pila.Add(_error);
        pila.Add(continuar);
        pila.Add(cancelar);
        Content = new ScrollView { Content = pila };
    }

    private static Entry Campo(string placeholder) => new()
    {
        Placeholder = placeholder,
        IsPassword = true,
        IsSpellCheckEnabled = false,
        IsTextPredictionEnabled = false,
        FontFamily = Tema.Fuente,
        FontSize = 18,
        TextColor = Tema.Hueso,
        PlaceholderColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
        MaxLength = 200,
    };

    private void Continuar(bool nueva)
    {
        string texto = _primera.Text ?? "";
        string? problema = nueva ? Respaldo.ProblemaContrasena(texto) : string.IsNullOrEmpty(texto) ? "Escribe la contraseña." : null;
        if (problema is null && nueva && texto != _segunda!.Text) problema = "Las dos contraseñas no coinciden.";
        if (problema is not null)
        {
            _error.Text = problema;
            _error.IsVisible = true;
            SemanticScreenReader.Announce(problema);
            return;
        }
        _resultado.TrySetResult(texto);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Actualizar();
        _primera.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _resultado.TrySetResult(null);
        Pantalla.Actualizar();
    }

    protected override bool OnBackButtonPressed()
    {
        _resultado.TrySetResult(null);
        return true;
    }
}
