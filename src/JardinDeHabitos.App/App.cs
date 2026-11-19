using JardinDeHabitos.App.Paginas;

namespace JardinDeHabitos.App;

public class App : Application
{
    public App()
    {
        UserAppTheme = AppTheme.Dark;
    }

    /// <summary>La app suele quedar en segundo plano de un día a otro: al volver, se relee todo.</summary>
    protected override void OnResume()
    {
        base.OnResume();
        if (Windows.FirstOrDefault()?.Page is NavigationPage nav && nav.CurrentPage is IRefrescable pagina)
            pagina.AlVolver();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new JardinPage())
        {
            BarBackgroundColor = Tema.Musgo,
            BarTextColor = Tema.Hueso,
        });
}
