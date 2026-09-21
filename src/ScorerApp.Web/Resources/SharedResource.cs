using Microsoft.Extensions.Localization;

// Lokalizátor skládá název zdroje z kořene sestavení + ResourcesPath („Resources“, nastavuje AddSimpleLocalization)
// + název třídy bez kořene. Bez atributu by kořenem byl název sestavení „ScorerApp.Web“ a hledal by se neexistující
// ScorerApp.Web.Resources.ScorerApp.SharedResource — stránky by místo textů ukazovaly klíče.
[assembly: RootNamespace("ScorerApp")]

namespace ScorerApp;

/// <summary>
/// Marker třída pro resource-based lokalizaci. Soubory SharedResource.resx (čeština jako
/// výchozí) a SharedResource.en.resx leží vedle ní a sdílí je všechny komponenty
/// přes IStringLocalizer&lt;SharedResource&gt;.
/// </summary>
public class SharedResource
{
}
