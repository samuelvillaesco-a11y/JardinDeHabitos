using JardinDeHabitos.App.Paginas;

namespace JardinDeHabitos.App;

public class App : Application
{
    public App()
    {
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new JardinPage())
        {
            BarBackgroundColor = Tema.Musgo,
            BarTextColor = Tema.Hueso,
        });
}
