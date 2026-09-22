# SeasonsPage.razor
Route: `/seasons`
Soubor: `src/ScorerApp.Web/Components/Pages/SeasonsPage.razor`
Popis: Výpis sezón napříč ligami s filtrem stavu a fulltextem.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Tabulka: sezóna, liga, sport, stav, formát, účastníci, zápasy
- Filtr stavu podle nového životního cyklu: Probíhá / Registrace / Návrh / Dokončené (2026-09-11)
- Fulltext přes název sezóny i ligy
- Admin: Nová sezóna
- Lokalizace cs/en (klíče `Seasons_*`) (2026-09-22)

## Chybí / Rozpracováno ⚠️
- Filtr podle sportu
- Načítá všechny zápasy kvůli počtu
- Formát jako surový enum bez lokalizace

## Návrhy na vylepšení 💡
- Přepínač „Moje sezóny“ (po propojení uživatel ↔ hráč)
- Seskupení podle ligy

## Brainstorming poznámky
- Obsahově se překrývá s LeagueDetail — rozhodnout, jestli z ní udělat hub s filtry, nebo ji zrušit

_Stav k 2026-09-22._
