#if DEBUG
using JardinDeHabitos.App.Dibujo;
using JardinDeHabitos.Core;

namespace JardinDeHabitos.App.Paginas;

/// <summary>Solo en Debug: muestra las 8 etapas y los estados especiales para revisar el dibujo.</summary>
public sealed class GaleriaPage : ContentPage
{
    public GaleriaPage()
    {
        Title = "Galería";
        BackgroundColor = Tema.Musgo;
        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 10,
            RowSpacing = 10,
            Padding = 14,
        };

        var estados = Enum.GetValues<GrowthStage>()
            .Select(e => (Tema.NombreEtapa(e), new EstadoArbol { Etapa = e, Especie = "Naranjo" }))
            .Concat(
            [
                ("Plaga", new EstadoArbol { Etapa = GrowthStage.Flor, Plaga = true, Cicatrices = 1 }),
                ("Raíces podridas", new EstadoArbol { Etapa = GrowthStage.ArbolJoven, Plaga = true, RaicesPodridas = true, Cicatrices = 2 }),
                ("Día libre", new EstadoArbol { Etapa = GrowthStage.FrutaComestible, Especie = "Cerezo", EnPaz = true }),
                ("Regado, 5 cicatrices", new EstadoArbol { Etapa = GrowthStage.FrutaInmadura, Regado = true, Cicatrices = 5 }),
            ])
            .ToList();

        for (int i = 0; i < estados.Count; i++)
        {
            if (i % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var (nombre, estado) = estados[i];
            grid.Add(Tema.Parcela(new VerticalStackLayout
            {
                Children =
                {
                    new GraphicsView { Drawable = new ArbolPequeno(estado), HeightRequest = 150 },
                    Tema.Texto(nombre, 14, Tema.FuenteNegrita),
                },
            }), i % 2, i / 2);
        }
        Content = new ScrollView { Content = grid };
    }
}
#endif
