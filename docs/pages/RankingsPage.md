# RankingsPage.razor
Route: `/rankings`
Soubor: `src/ScorerApp.Web/Components/Pages/RankingsPage.razor`
Popis: Žebříček trvalého ratingu podle sportu.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Chipy pro výběr sportu (ranking je vždy per sport)
- Tabulka: pořadí, hráč/tým s prokliknutím, rating, zápasy, V/R/P
- Lokalizováno přes IStringLocalizer<SharedResource> (CZ/EN)
- Rating plní SportRatingService — přepočet celé historie sportu po každém výsledku
- Lokalizace cs/en (sdílené klíče bez předpony `Rankings_*`) — ověřeno, žádné natvrdo psané texty nezbyly

## Chybí / Rozpracováno ⚠️
- Žádné hledání ani stránkování
- Nezobrazuje vývoj (trend nahoru/dolů) ani poslední zápas
- Závody (multi-participant) se do ratingu nepočítají

## Návrhy na vylepšení 💡
- Filtr „jen aktivní v posledních N měsících“
- Graf vývoje ratingu v čase (ApexCharts)
- Zvýraznit vlastního hráče v žebříčku

## Brainstorming poznámky
- Řadí se podle Rating, ne podle počtu her — nový hráč s jednou výhrou může být vysoko; zvážit minimum zápasů pro zařazení

_Stav k 2026-09-22._
