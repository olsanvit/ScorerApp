using System.Globalization;

namespace ScorerApp.Domain.Services;

/// <summary>
/// Dočasně přepne jazyk, ve kterém se sestavuje text pro někoho jiného než přihlášeného uživatele
/// (e-mail příjemci). IStringLocalizer čte CurrentUICulture až v okamžiku překladu, takže stačí
/// obalit sestavení textu — samotné odeslání (await) má být už mimo, ať se kultura nepřenese dál.
/// </summary>
public readonly struct CultureScope : IDisposable
{
    /// <summary>Jazyky, pro které existuje resx. Cokoli jiného (nebo nic) = výchozí čeština.</summary>
    public static readonly string[] Supported = ["cs", "en"];

    private readonly CultureInfo _previousCulture, _previousUiCulture;

    private CultureScope(CultureInfo culture)
    {
        _previousCulture = CultureInfo.CurrentCulture;
        _previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static CultureScope For(string? culture)
    {
        var code = culture?.Split('-')[0].ToLowerInvariant();
        return new CultureScope(CultureInfo.GetCultureInfo(Supported.Contains(code) ? code! : Supported[0]));
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}
