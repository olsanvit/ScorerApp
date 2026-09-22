# SeasonGenerateMatches.razor
Route: `/admin/seasons/{Id:guid}/generate`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/SeasonGenerateMatches.razor`
Popis: Generování rozpisu zápasů sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Generování navazujících fází (2026-09-12): playoff ze zápasů předchozí fáze, další kolo Swissu
- Náhled aktuální fáze a jejího dohrání
- Náhled počtu zápasů
- Generování přes `SeasonScheduleService` (od 2026-09-11 sdílené s registrací)
- Ochrana proti přepsání rozpisu s odehranými zápasy
- Přepnutí sezóny do stavu Probíhá
- Závodní sporty: jen přepnutí stavu bez zápasů
- Lokalizace cs/en (klíče `Generate_*`, sdílené `GenerateNextPhase`, `PhaseNotFinished`); text chyby ze SeasonScheduleService zůstává česky

## Chybí / Rozpracováno ⚠️
- Nasazení podle ratingu
- Náhled rozpisu po kolech před potvrzením

## Návrhy na vylepšení 💡
- Počet vzájemných zápasů jako parametr (1×, 2×, 3×)

## Brainstorming poznámky
- Po zavedení registrace je hlavní cesta „Uzavřít registraci“ — tahle stránka zůstává pro přegenerování

_Stav k 2026-09-22._
