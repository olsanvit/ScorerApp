namespace ScorerApp.Mobile;

public static class MobileAuthHandoff
{
    public static event Action<string>? TokenReceived;

    public static void RaiseTokenReceived(string token) => TokenReceived?.Invoke(token);
}
