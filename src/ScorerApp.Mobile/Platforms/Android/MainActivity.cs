using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace ScorerApp.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "scorerapp", DataHost = "auth")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleAuthIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleAuthIntent(intent);
    }

    private static void HandleAuthIntent(Intent? intent)
    {
        var token = intent?.Data?.GetQueryParameter("token");
        if (!string.IsNullOrEmpty(token))
        {
            MobileAuthHandoff.RaiseTokenReceived(token);
        }
    }
}
