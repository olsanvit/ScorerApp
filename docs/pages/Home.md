# Home.razor
Route: `/`
Soubor: `src/ScorerApp.Web/Components/Pages/Home.razor`
Popis: Dashboard po přihlášení — souhrnná čísla, přehled sportů a probíhající sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- 4 stat karty: ligy, probíhající sezóny, hráči, odehrané zápasy (UiStats)
- Dlaždice sportů s počtem lig, proklik na `/leagues?sport={guid}`
- Tabulka až 10 probíhajících sezón (liga, sport, počet zápasů, tlačítko Zápasy)
- Loading stav a prázdné stavy

## Chybí / Rozpracováno ⚠️
- „Dnešní zápasy“ z původní spec 2026-05-29 nejsou implementované
- Žádný osobní kontext (moje zápasy, můj rating) — chybí propojení uživatel ↔ hráč
- Prázdný stav sportů odkazuje na `/admin/sports` i neadminovi
- Sezóny ve stavu Registrace se nezobrazují (jen Probíhá)

## Návrhy na vylepšení 💡
- Primární CTA „Nový turnaj“ — průvodce liga + sezóna + registrace v jednom kroku
- Panel „Probíhá registrace“ se sezónami ve stavu Registration
- Posledních N výsledků napříč sezónami
- Graf aktivity přes ApexCharts (knihovna je registrovaná, ale nikde nepoužitá)

## Brainstorming poznámky
- V MAUI WebView je to vstupní obrazovka — na kurtu je důležitější akce než statistika
- 5 dotazů za sebou; při růstu dat zvážit MemoryCache (je registrovaná přes AddSharedUI)
- Kandidát na rozdělení do komponent HomeStats / ActiveSeasonsPanel

_Stav k 2026-09-11._
