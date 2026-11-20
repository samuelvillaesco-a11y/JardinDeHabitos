using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace JardinDeHabitos.App;

[Activity(Theme = "@style/Jardin.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Con el seguro general, ni capturas ni miniatura en "apps recientes" desde el primer cuadro.
        if (Seguridad.PantallaDebeSerPrivada)
            Window?.SetFlags(Android.Views.WindowManagerFlags.Secure, Android.Views.WindowManagerFlags.Secure);

        if (OperatingSystem.IsAndroidVersionAtLeast(33)
            && CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            RequestPermissions([Manifest.Permission.PostNotifications], 1);
        }
        Notificaciones.Programar(this);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        Telefono.AlVolver(requestCode, resultCode, data);
    }
}
