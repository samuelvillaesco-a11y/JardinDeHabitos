using System.Globalization;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Perfil opcional y jardineros. Todo es opcional y se guarda cifrado solo en el teléfono.
/// Se abre desde Ajustes, previa verificación (son datos personales).
/// </summary>
public sealed class PerfilPage : ContentPage, IPrivada, IConLlave
{
    private readonly Entry _nombre = Campo("Tu nombre o apodo", Keyboard.Text);
    private readonly Entry _correo = Campo("Tu correo", Keyboard.Email);
    private readonly Entry _telefono = Campo("Tu teléfono", Keyboard.Telephone);
    private readonly Entry _peso = Campo("Peso (kg)", Keyboard.Numeric);
    private readonly Entry _altura = Campo("Altura (cm)", Keyboard.Numeric);
    private readonly Chips<ActividadFisica> _actividad;
    private readonly Label _aviso = Tema.Texto("", 15, Tema.FuenteNegrita, Tema.Miel);
    private readonly VerticalStackLayout _listaJardineros = new() { Spacing = 10 };
    private bool _ocupado;

    public PerfilPage()
    {
        Title = "Perfil";
        BackgroundColor = Tema.Musgo;

        var p = Servicios.Jardin.Perfil();
        _nombre.Text = p.Nombre;
        _correo.Text = p.Correo;
        _telefono.Text = p.Telefono;
        _peso.Text = p.PesoKg?.ToString(CultureInfo.InvariantCulture);
        _altura.Text = p.AlturaCm?.ToString(CultureInfo.InvariantCulture);
        _actividad = new Chips<ActividadFisica>(
            [
                (ActividadFisica.SinDecir, "Prefiero no decir"),
                (ActividadFisica.Ninguna, "Casi nada"),
                (ActividadFisica.Ligera, "Ligera"),
                (ActividadFisica.Moderada, "Moderada"),
                (ActividadFisica.Intensa, "Intensa"),
            ], iniciales: [p.Actividad]);

        var guardar = Tema.Boton("Guardar perfil", Tema.Miel);
        guardar.Clicked += (_, _) => Guardar();
        var desdeContactos = Tema.Boton("➕ Elegir de mis contactos", Tema.Hoja);
        desdeContactos.Clicked += async (_, _) => await AgregarDesdeContactos();
        var aMano = Tema.Boton("Escribir un número", Tema.MusgoClaro, Tema.Hueso);
        aMano.Clicked += async (_, _) => await AgregarAMano();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 20, 18, 28),
                Spacing = 12,
                Children =
                {
                    Tema.Titulo("👤 Tu perfil", 30),
                    Tema.Texto("Todo es opcional. Se guarda cifrado y solo en este teléfono; nadie más lo ve.", 14, color: Tema.HuesoTenue),
                    _nombre, _correo, _telefono,
                    Tema.Texto("Salud (opcional)", 17, Tema.FuenteNegrita, Tema.Hoja),
                    Fila(_peso, _altura),
                    Tema.Texto("¿Cuánta actividad física haces?", 15),
                    _actividad,
                    Tema.Texto("Moderada = 150 a 300 minutos a la semana de caminar rápido, bici o similar (recomendación de la OMS).", 13, color: Tema.HuesoTenue),
                    _aviso,
                    guardar,
                    Tema.Texto("🌿 Tus jardineros", 22, Tema.FuenteTitulo, Tema.Hoja),
                    Tema.Texto($"Personas de confianza a quien llamar o escribir en un momento difícil (hasta {Jardinero.Maximo}). Aparecen en Ajustes › Pedir ayuda.", 14, color: Tema.HuesoTenue),
                    _listaJardineros,
                    desdeContactos,
                    aMano,
                    Tema.Texto("Al elegir de tus contactos, Android solo le pasa a la app el contacto que tocas: no ve el resto de tu agenda.", 13, color: Tema.HuesoTenue),
                },
            },
        };
        PintarJardineros();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Apariencia.Aplicar(this);
        Pantalla.Actualizar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Pantalla.Actualizar();
    }

    private static Entry Campo(string placeholder, Keyboard teclado) => new()
    {
        Placeholder = placeholder,
        Keyboard = teclado,
        FontFamily = Tema.Fuente,
        FontSize = 17,
        TextColor = Tema.Hueso,
        PlaceholderColor = Tema.HuesoTenue,
        BackgroundColor = Tema.MusgoClaro,
        MaxLength = Perfil.LargoMaximoTexto,
    };

    private static Grid Fila(View a, View b)
    {
        var g = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 10 };
        g.Add(a, 0);
        g.Add(b, 1);
        return g;
    }

    private static double? Numero(string? texto) =>
        double.TryParse((texto ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;

    private void Guardar()
    {
        if (!string.IsNullOrWhiteSpace(_peso.Text) && Numero(_peso.Text) is null) { _aviso.Text = "Escribe el peso como número, por ejemplo 70.5."; return; }
        if (!string.IsNullOrWhiteSpace(_altura.Text) && Numero(_altura.Text) is null) { _aviso.Text = "Escribe la altura en centímetros, por ejemplo 170."; return; }
        try
        {
            Servicios.Jardin.GuardarPerfil(new Perfil(_nombre.Text, _correo.Text, _telefono.Text,
                Numero(_peso.Text), Numero(_altura.Text), _actividad.Elegido));
            _aviso.TextColor = Tema.Hoja;
            _aviso.Text = "Perfil guardado.";
        }
        catch (ArgumentException ex)
        {
            _aviso.TextColor = Tema.Miel;
            _aviso.Text = ex.Message;
        }
    }

    private void PintarJardineros()
    {
        _listaJardineros.Clear();
        var lista = Servicios.Jardin.Jardineros();
        if (lista.Count == 0)
            _listaJardineros.Add(Tema.Texto("Aún no tienes jardineros.", 14, color: Tema.HuesoTenue));
        foreach (var j in lista)
        {
            var quitar = Tema.Boton("Quitar", Colors.Transparent, Tema.HuesoTenue);
            quitar.Clicked += async (_, _) =>
            {
                if (!await DisplayAlertAsync("¿Quitar jardinero?", $"{j.Nombre} deja de aparecer en Pedir ayuda.", "Quitar", "Cancelar")) return;
                Servicios.Jardin.EliminarJardinero(j.Id);
                PintarJardineros();
            };
            var fila = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 8 };
            fila.Add(new VerticalStackLayout
            {
                Spacing = 2,
                Children = { Tema.Texto(j.Nombre, 16, Tema.FuenteNegrita), Tema.Texto(j.Telefono, 14, color: Tema.HuesoTenue) },
            }, 0);
            fila.Add(quitar, 1);
            _listaJardineros.Add(Tema.Parcela(fila));
        }
    }

    private async Task AgregarDesdeContactos()
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            var elegido = await Telefono.ElegirContactoAsync();
            if (elegido is not { } c) return;
            Agregar(c.Nombre, c.Numero);
        }
        finally { _ocupado = false; }
    }

    private async Task AgregarAMano()
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            var nombre = await DisplayPromptAsync("Nuevo jardinero", "¿Cómo se llama?", "Siguiente", "Cancelar", maxLength: Perfil.LargoMaximoTexto);
            if (string.IsNullOrWhiteSpace(nombre)) return;
            var numero = await DisplayPromptAsync("Nuevo jardinero", $"Número de {nombre.Trim()}", "Agregar", "Cancelar", keyboard: Keyboard.Telephone, maxLength: 20);
            if (string.IsNullOrWhiteSpace(numero)) return;
            Agregar(nombre, numero);
        }
        finally { _ocupado = false; }
    }

    private async void Agregar(string nombre, string numero)
    {
        try
        {
            Servicios.Jardin.AgregarJardinero(nombre, numero);
            PintarJardineros();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await DisplayAlertAsync("No se pudo agregar", ex.Message, "Entendido");
        }
    }
}
