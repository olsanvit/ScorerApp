# TeamSeasonStats.razor
Route: `/teams/{TeamId:guid}/seasons/{SeasonId:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/TeamSeasonStats.razor`
Popis: Statistiky týmu v jedné sezóně.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Bilance Z/V/R/P a skóre
- Zápasy týmu (doma/venku, výsledek V/R/P)
- Aktuální sezónní ELO
- Top 10 střelců z MatchEvents
- Lokalizace cs/en (klíče `TeamStats_*`) (2026-09-22)

## Chybí / Rozpracováno ⚠️
- Panel „ELO rating“ ukazuje jen jedno číslo, ne vývoj
- Obdoba pro hráče (`/players/{id}/seasons/{seasonId}`) neexistuje — u individuálních sportů důležitější

## Návrhy na vylepšení 💡
- Graf ELO (ApexCharts)
- Forma a vzájemné zápasy se soupeři

## Brainstorming poznámky
- Samostatná URL přidává navigační krok — kandidát na záložku v TeamDetail

_Stav k 2026-09-22._
