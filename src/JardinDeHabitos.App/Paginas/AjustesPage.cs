namespace JardinDeHabitos.App.Paginas;

/// <summary>Ajustes: música, seguridad, privacidad y "acerca de".</summary>
public sealed class AjustesPage : ContentPage
{
    private bool _navegando;

    public AjustesPage()
    {
        Title = "Ajustes";
        BackgroundColor = Tema.Musgo;
        NavigationPage.SetHasNavigationBar(this, false);

        var musica = new Switch { IsToggled = Musica.Encendida, OnColor = Tema.Hoja, ThumbColor = Tema.Hueso, VerticalOptions = LayoutOptions.Center };
        musica.Toggled += (_, e) => Musica.Encendida = e.Value;
        SemanticProperties.SetDescription(musica, "Música del jardín");
        var filaMusica = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        filaMusica.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Tema.Texto("🎵 Música del jardín", 17, Tema.FuenteNegrita),
                Tema.Texto("Una melodía tranquila de bosque, en bucle. Se pausa cuando sales de la app.", 14, color: Tema.HuesoTenue),
            },
        }, 0);
        filaMusica.Add(musica, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 28, 18, 28),
                Spacing = 14,
                Children =
                {
                    Tema.Titulo("Ajustes", 34),
                    Tema.Parcela(filaMusica),
                    Entrada("🆘 Pedir ayuda", "911, tus jardineros y la Línea de la Vida. Siempre a la mano, sin candado.", () => Abrir(() => new AyudaPage())),
                    Entrada("👤 Perfil y jardineros", "Tus datos (opcionales) y tus personas de confianza.", AbrirPerfil),
                    Entrada("🎨 Personalización", "Colores, degradados y escenas para el fondo.", () => Abrir(() => new PersonalizacionPage())),
                    Entrada("🔒 Seguridad", "Tu candado y qué partes van bajo llave.", AbrirSeguridad),
                    Entrada("🛡️ Privacidad", "Qué pasa con tus datos (spoiler: nada sale de tu teléfono).", () => Abrir(() => new PrivacidadPage())),
                    Entrada("🌱 Acerca de", "Versión, licencia y créditos.", () => Abrir(() => new AcercaDePage())),
                },
            },
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
    }

    private static View Entrada(string titulo, string detalle, Func<Task> accion)
    {
        var parcela = Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { Tema.Texto(titulo, 17, Tema.FuenteNegrita), Tema.Texto(detalle, 14, color: Tema.HuesoTenue) },
        });
        SemanticProperties.SetDescription(parcela, $"{titulo}. {detalle}");
        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) => await accion();
        parcela.GestureRecognizers.Add(toque);
        return parcela;
    }

    private async Task Abrir(Func<Page> crear)
    {
        if (_navegando || Navigation.NavigationStack.LastOrDefault() != this) return;
        _navegando = true;
        try { await Navigation.PushAsync(crear()); }
        finally { _navegando = false; }
    }

    /// <summary>El perfil tiene datos personales: pide verificación para entrar.</summary>
    private async Task AbrirPerfil()
    {
        if (_navegando) return;
        if (!await Seguridad.VerificarAsync(this, "Tu perfil")) return;
        await Abrir(() => new PerfilPage());
    }

    /// <summary>El panel de seguridad siempre pide verificación, aunque los seguros estén apagados.</summary>
    private async Task AbrirSeguridad()
    {
        if (_navegando) return;
        if (!await Seguridad.VerificarAsync(this, "Ajustes de seguridad")) return;
        await Abrir(() => new SeguridadPage());
    }
}

/// <summary>Política de privacidad: corta, honesta y verificable.</summary>
public sealed class PrivacidadPage : ContentPage
{
    public PrivacidadPage()
    {
        Title = "Privacidad";
        BackgroundColor = Tema.Musgo;
        var pila = new VerticalStackLayout { Padding = new Thickness(18, 20, 18, 28), Spacing = 12 };
        pila.Add(Tema.Titulo("🛡️ Tu privacidad", 30));
        pila.Add(Tema.Texto("Jardín de Hábitos está hecho para que lo que vives aquí sea solo tuyo.", 16));
        foreach (var (titulo, texto) in Puntos) pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { Tema.Texto(titulo, 16, Tema.FuenteNegrita, Tema.Hoja), Tema.Texto(texto, 14) },
        }));
        Content = new ScrollView { Content = pila };
    }

    private static readonly (string, string)[] Puntos =
    [
        ("No recopilamos nada", "No hay cuentas, registros, analíticas, anuncios ni rastreadores. Nadie más que tú ve lo que registras."),
        ("Todo se queda en tu teléfono", "Tus árboles, logros y diario se guardan solo en este dispositivo, en una base de datos cifrada (SQLCipher) cuya llave vive en el almacenamiento seguro de Android."),
        ("Sin internet", "La app no tiene permiso de internet: no puede enviar ni recibir nada. Los avisos se generan dentro del teléfono."),
        ("Sin empresas de por medio", "No usamos inicio de sesión de Google, Facebook ni de ninguna otra compañía, ni correo electrónico."),
        ("Sin copias en la nube", "Los respaldos automáticos y el paso de datos a otro teléfono están desactivados para esta app. Si la desinstalas, se borra todo."),
        ("Tu candado es tuyo", "Puedes usar el bloqueo del teléfono o un PIN o patrón propio. De tu PIN o patrón solo se guarda una huella cifrada (PBKDF2 con sal), nunca el original."),
        ("Código abierto", "El código es abierto (licencia MIT): cualquiera puede revisar que todo esto es verdad."),
    ];
}

/// <summary>Versión, licencia y créditos.</summary>
public sealed class AcercaDePage : ContentPage
{
    public AcercaDePage()
    {
        Title = "Acerca de";
        BackgroundColor = Tema.Musgo;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 20, 18, 28),
                Spacing = 12,
                Children =
                {
                    Tema.Titulo("🌱 Jardín de Hábitos", 30),
                    Tema.Texto($"Versión {AppInfo.Current.VersionString}", 15, color: Tema.HuesoTenue),
                    Tema.Texto("Cada hábito que dejas es un árbol que crece. Un compañero tranquilo y privado para dejar malos hábitos, con tiempos basados en evidencia médica.", 16),
                    Tema.Parcela(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            Tema.Texto("Licencia", 16, Tema.FuenteNegrita, Tema.Hoja),
                            Tema.Texto("Software libre bajo la licencia MIT.", 14),
                        },
                    }),
                    Tema.Parcela(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            Tema.Texto("Música", 16, Tema.FuenteNegrita, Tema.Hoja),
                            Tema.Texto("\"Forest Ambience\" de TinyWorlds, en dominio público (CC0), vía OpenGameArt.", 14),
                        },
                    }),
                    Tema.Parcela(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            Tema.Texto("Escenas de fondo", 16, Tema.FuenteNegrita, Tema.Hoja),
                            Tema.Texto("Bosque, jardín, casa de campo y suburbio: dibujos vectoriales propios del proyecto (licencia MIT).", 14),
                        },
                    }),
                    Tema.Parcela(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            Tema.Texto("No sustituye ayuda profesional", 16, Tema.FuenteNegrita, Tema.Miel),
                            Tema.Texto("Si tu hábito pone en riesgo tu salud, busca apoyo médico. En México: Línea de la Vida 800 911 2000 (gratis, 24 h). Urgencias: 911.", 14),
                        },
                    }),
                },
            },
        };
    }
}
