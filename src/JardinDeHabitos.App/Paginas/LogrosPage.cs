using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>Logros del usuario. Los secretos solo aparecen una vez desbloqueados.</summary>
public sealed class LogrosPage : ContentPage
{
    public LogrosPage()
    {
        Title = "Logros";
        BackgroundColor = Tema.Musgo;

        var jardin = Servicios.Jardin;
        var lista = new VerticalStackLayout { Padding = new Thickness(18, 20), Spacing = 12 };

        var tenidos = jardin.LogrosDesbloqueados();
        foreach (var l in tenidos)
        {
            var info = LogroInfo.De(l.Logro);
            bool dorada = l.Logro == Logro.LeyendaCaida;
            lista.Add(Tema.Parcela(
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        Tema.Texto(info.Nombre, 20, Tema.FuenteTitulo, dorada ? Tema.Musgo : Tema.Miel),
                        Tema.Texto(info.Descripcion, 15, color: dorada ? Tema.Musgo : Tema.Hueso),
                        Tema.Texto($"{l.Habito}, {Tema.FechaCorta(l.Fecha)}", 13, color: dorada ? Tema.Corteza : Tema.HuesoTenue),
                    },
                },
                dorada ? Tema.Miel : Tema.MusgoClaro));
        }

        foreach (var pendiente in jardin.LogrosPorDesbloquear())
        {
            var info = LogroInfo.De(pendiente);
            lista.Add(Tema.Parcela(
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        Tema.Texto(info.Nombre, 20, Tema.FuenteTitulo, Tema.HuesoTenue),
                        Tema.Texto(info.Descripcion, 15, color: Tema.HuesoTenue),
                    },
                },
                Colors.Transparent, Tema.MusgoClaro));
        }

        if (tenidos.Count == 0)
            lista.Insert(0, Tema.Texto("Aún no hay logros. Los árboles tardan en dar fruto.", 15, color: Tema.HuesoTenue));

        Content = new ScrollView { Content = lista };
    }
}
