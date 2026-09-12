# RaceCreate.razor
Route: `/admin/races/create`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/RaceCreate.razor`
Popis: Formulář nového závodu.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Sezóna (jen probíhající sezóny multi-participant sportů), název, vzdálenost + jednotka, datum, poznámky
- Předvyplnění sezóny z `?seasonId`

## Chybí / Rozpracováno ⚠️
- Závod nejde založit v sezóně ve stavu Registrace

## Návrhy na vylepšení 💡
- Série závodů najednou (např. celý seriál na rok)

## Brainstorming poznámky
- Od 2026-09-11 se závodní sezóna do stavu Probíhá dostane uzavřením registrace bez generování zápasů

_Stav k 2026-09-11._
