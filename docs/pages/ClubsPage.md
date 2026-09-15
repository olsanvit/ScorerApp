# ClubsPage.razor
Route: `/clubs`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/ClubsPage.razor`
Popis: Seznam oddílů napříč organizacemi s filtrem organizace a hledáním.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Tabulka: název + zkratka, organizace, počet hráčů na soupisce, počet týmů
- Filtr organizace, hledání (UiSearchBar)
- Admin vidí i neaktivní oddíly
- Tlačítko Nový oddíl jen pro admina a správce organizace
- Plná lokalizace

## Chybí / Rozpracováno ⚠️
- Stránkování chybí — u stovek oddílů bude seznam dlouhý
- Nezobrazuje, ve kterých oddílech je přihlášený uživatel

## Návrhy na vylepšení 💡
- Filtr „moje oddíly“
- Řazení podle počtu hráčů

## Brainstorming poznámky
- Seznam není omezený na vlastní organizace — oddíly jsou ve ScorerAppu veřejné jako týmy a hráči; citlivé věci (chat, oběžníky, auta) hlídají služby

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
