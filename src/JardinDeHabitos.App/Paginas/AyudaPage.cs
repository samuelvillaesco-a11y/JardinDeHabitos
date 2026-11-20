using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>
/// Pedir ayuda: 911, tus jardineros y la Línea de la Vida. Nunca va bajo llave: en un momento
/// difícil no debe haber obstáculos. Las llamadas abren el marcador para que tú confirmes.
/// </summary>
public sealed class AyudaPage : ContentPage
{
    // Mensaje discreto: no menciona el hábito ni la app.
    private const string MensajeJardinero = "Hola, ¿tienes un momento? Estoy pasando un rato difícil y me ayudaría hablar contigo.";

    public AyudaPage()
    {
        Title = "Pedir ayuda";
        BackgroundColor = Tema.Musgo;
        var pila = new VerticalStackLayout { Padding = new Thickness(18, 20, 18, 28), Spacing = 14 };
        pila.Add(Tema.Titulo("🆘 Pedir ayuda", 30));
        pila.Add(Tema.Texto("Pedir ayuda es parte de crecer. Nada de esto se registra en la app.", 15, color: Tema.HuesoTenue));

        var emergencia = Tema.Boton("📞 Llamar al 911", Color.FromArgb("#C8553D"), Tema.Hueso);
        emergencia.FontSize = 20;
        emergencia.Clicked += async (_, _) => await Marcar(Telefono.Emergencias);
        pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Tema.Texto("Emergencia", 17, Tema.FuenteNegrita, Tema.Miel),
                Tema.Texto("Si tú o alguien corre peligro, o tienes convulsiones, confusión, alucinaciones, dolor en el pecho o no puedes dejar de vomitar.", 14),
                emergencia,
            },
        }));

        var jardineros = Servicios.Jardin.Jardineros();
        pila.Add(Tema.Texto("Tus jardineros", 19, Tema.FuenteTitulo, Tema.Hoja));
        if (jardineros.Count == 0)
        {
            pila.Add(Tema.Texto("Aún no tienes jardineros: personas de confianza a quien llamar o escribir. Agrégalos en Ajustes › Perfil y jardineros.", 14, color: Tema.HuesoTenue));
        }
        foreach (var j in jardineros)
        {
            var llamar = Tema.Boton("Llamar", Tema.Hoja);
            llamar.Clicked += async (_, _) => await Marcar(j.Telefono);
            var escribir = Tema.Boton("Mensaje", Tema.MusgoClaro, Tema.Hueso);
            escribir.Clicked += async (_, _) =>
            {
                if (!Telefono.Mensaje(j.Telefono, MensajeJardinero))
                    await DisplayAlertAsync("No se pudo", "Este teléfono no tiene app de mensajes.", "Entendido");
            };
            var botones = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 10 };
            botones.Add(llamar, 0);
            botones.Add(escribir, 1);
            pila.Add(Tema.Parcela(new VerticalStackLayout
            {
                Spacing = 8,
                Children = { Tema.Texto($"🌿 {j.Nombre}", 17, Tema.FuenteNegrita), botones },
            }));
        }

        var linea = Tema.Boton("Llamar a la Línea de la Vida", Tema.MusgoClaro, Tema.Hueso);
        linea.Clicked += async (_, _) => await Marcar(Telefono.LineaDeLaVida);
        pila.Add(Tema.Parcela(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Tema.Texto("Línea de la Vida", 17, Tema.FuenteNegrita, Tema.Hoja),
                Tema.Texto("800 911 2000 · gratis, 24 horas. Orientación en salud mental y adicciones (México).", 14),
                linea,
            },
        }));
        Content = new ScrollView { Content = pila };
    }

    private async Task Marcar(string numero)
    {
        if (!Telefono.Marcar(numero))
            await DisplayAlertAsync("No se pudo", $"Este teléfono no puede abrir el marcador. Marca {numero} a mano.", "Entendido");
    }
}
