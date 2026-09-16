namespace ScorerApp.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        webView.Navigating += OnWebViewNavigating;
        webView.Navigated += (_, _) => loadingIndicator.IsVisible = loadingIndicator.IsRunning = false;

        MobileAuthHandoff.TokenReceived += OnAuthTokenReceived;
        Unloaded += (_, _) => MobileAuthHandoff.TokenReceived -= OnAuthTokenReceived;
    }

    private void OnAuthTokenReceived(string token)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            webView.Source = $"https://scorerapp.vo2info.cz/mobile-token-login?token={Uri.EscapeDataString(token)}");
    }

    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Google blokuje OAuth přihlášení uvnitř embedded WebView ("disallowed_useragent").
        // Otevřeme tuto navigaci v systémovém prohlížeči (Chrome Custom Tabs na Androidu).
        if (Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) &&
            uri.Host.Equals("accounts.google.com", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            return;
        }

        loadingIndicator.IsVisible = loadingIndicator.IsRunning = true;
    }

    protected override bool OnBackButtonPressed()
    {
        if (webView.CanGoBack)
        {
            webView.GoBack();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
