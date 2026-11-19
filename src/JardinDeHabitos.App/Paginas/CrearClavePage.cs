using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Crear (o cambiar) la clave de la app. Es opcional: la primera vez se puede saltar.
/// Al crearla se muestra UNA vez el código de recuperación para anotarlo.
/// </summary>
public sealed class CrearClavePage : ContentPage
{
    public enum Motivo { PrimeraVez, Ajustes, Recuperacion }

    private readonly Motivo _motivo;
    private readonly Func<Task> _alTerminar;
    private readonly Entry _clave = Campo("Nueva clave");
    private readonly Entry _repite = Campo("Repite la clave");
    private readonly Label _error = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private readonly Button _crear = Tema.Boton("Crear clave", Tema.Miel);
    private bool _creando;

    public CrearClavePage(Motivo motivo, Func<Task> alTerminar)
    {
        _motivo = motivo;
        _alTerminar = alTerminar;
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);
        _crear.Clicked += async (_, _) => await Crear();

        var pila = new VerticalStackLayout { Padding = new Thickness(20, 48, 20, 28), Spacing = 14 };
        pila.Add(Tema.Titulo(motivo == Motivo.PrimeraVez ? "Bienvenido a tu jardín" : "Crea tu clave", 30));
        pila.Add(Tema.Texto(motivo switch
        {
            Motivo.PrimeraVez => "Si quieres, crea una clave para la app. Con ella se abren tu estante, tu blog y, si lo activas, toda la app. Puedes hacerlo después en Seguridad.",
            Motivo.Recuperacion => "Tu código de recuperación funcionó. Crea una clave nueva.",
            _ => "Esta clave protege lo que pongas bajo llave.",
        }, 16));
        pila.Add(_clave);
        pila.Add(_repite);
        pila.Add(Tema.Texto($"Mínimo {ClaveLocal.LargoMinimo} caracteres. Puede tener números, letras y espacios.", 13, color: Tema.HuesoTenue));
        pila.Add(_error);
        pila.Add(_crear);
        if (motivo == Motivo.PrimeraVez)
        {
            var ahoraNo = Tema.Boton("Ahora no", Colors.Transparent, Tema.HuesoTenue);
            ahoraNo.Clicked += async (_, _) => await _alTerminar();
            pila.Add(ahoraNo);
        }
        pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Tema.Texto("Tus datos se quedan aquí", 16, Tema.FuenteNegrita, Tema.Hoja),
                Tema.Texto("• Todo lo que registres se guarda solo en este teléfono.", 14),
                Tema.Texto("• Sin cuentas de Google, Facebook ni de ninguna empresa, y sin correo.", 14),
                Tema.Texto("• La app no tiene servidores ni usa internet.", 14),
                Tema.Texto("• Tu clave no se guarda: solo una huella cifrada de ella.", 14),
            },
        }));
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
        MaxLength = ClaveLocal.LargoMaximo,
    };

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Pantalla.Privada(true);
    }

    // La primera vez, "atrás" no se salta la pantalla (para eso está "Ahora no"); al recuperar, tampoco.
    protected override bool OnBackButtonPressed() => _motivo != Motivo.Ajustes;

    private async Task Crear()
    {
        if (_creando) return;
        if (ClaveLocal.Problema(_clave.Text) is { } problema)
        {
            _error.Text = problema;
            return;
        }
        if (_clave.Text != _repite.Text)
        {
            _error.Text = "Las dos claves no coinciden.";
            return;
        }
        _creando = true;
        _crear.IsEnabled = false;
        _error.Text = "Guardando…";
        try
        {
            var codigo = await Seguridad.CrearClaveAsync(_clave.Text!);
            _clave.Text = _repite.Text = "";
            MostrarCodigo(codigo);
        }
        catch (Exception)
        {
            _error.Text = "No se pudo guardar en el almacenamiento seguro del teléfono. Intenta de nuevo.";
            _creando = false;
            _crear.IsEnabled = true;
        }
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
                    Tema.Texto("Anótalo en papel y guárdalo. Si olvidas tu clave, es la única forma de entrar. No se volverá a mostrar y no se puede recuperar de otra manera.", 16),
                    Tema.Parcela(Tema.Texto(codigo, 26, Tema.FuenteTitulo, Tema.Miel), Tema.TierraOscura, Tema.Miel),
                    Tema.Texto("No lo guardes en una captura de pantalla: esta pantalla no permite capturas.", 14, color: Tema.HuesoTenue),
                    listo,
                },
            },
        };
    }
}
