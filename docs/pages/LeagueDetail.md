# LeagueDetail.razor
Route: `/leagues/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/LeagueDetail.razor`
Popis: Detail ligy — popis a seznam sezón; admin akce nad ligou.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Hlavička se sportem a popisem
- Tabulka sezón: formát (popis z `FormatJson`), stav (badge ze `SeasonStatusExtensions`), účastníci, zápasy
- Admin: Nová sezóna (předvyplněná liga), Upravit, Smazat s potvrzením
- Lokalizace cs/en (klíče `League_*`) (2026-09-22)

## Chybí / Rozpracováno ⚠️
- Síň slávy — kdo vyhrál kterou sezónu
- Čitelné zobrazení bodovacího schématu (JSON override ligy)
- All-time tabulka ligy přes všechny sezóny
- Ověřit chování soft-delete: smaže se jen liga, sezóny zůstanou v DB bez viditelného rodiče?

## Návrhy na vylepšení 💡
- „Klonovat sezónu“ — nová sezóna se stejnými účastníky (roční ligy)
- Statistiky ligy (nejvíc výher, nejdelší série)

## Brainstorming poznámky
- Include Seasons → Participants + Matches načte všechny zápasy jen kvůli počtu — stačila by projekce Count

_Stav k 2026-09-22._
