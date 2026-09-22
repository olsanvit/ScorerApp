# RacesPage.razor
Route: `/races`
Soubor: `src/ScorerApp.Web/Components/Pages/RacesPage.razor`
Popis: Přehled závodů (multi-participant sporty).
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Tabulka: závod, sezóna, liga, datum, počet výsledků
- Lokalizace cs/en (klíče `Races_*`)

## Chybí / Rozpracováno ⚠️
- Filtr a hledání
- Tlačítko nového závodu (jde jen přes SeasonDetail)
- Stránkování

## Návrhy na vylepšení 💡
- Filtr podle sezóny
- Kalendářní pohled

## Brainstorming poznámky
- Nejmenší stránka — zvážit sloučení do SeasonDetail / LeagueDetail

_Stav k 2026-09-22._
