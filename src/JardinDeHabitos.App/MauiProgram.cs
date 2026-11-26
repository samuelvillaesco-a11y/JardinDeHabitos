#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace JardinDeHabitos.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Nunito-Regular.ttf", Tema.Fuente);
                fonts.AddFont("Nunito-Bold.ttf", Tema.FuenteNegrita);
                fonts.AddFont("Lora-SemiBold.ttf", Tema.FuenteTitulo);
                fonts.AddFont("Lucide.ttf", Tema.FuenteIconos);
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
