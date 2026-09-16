# Home.razor
Route: `/`
Soubor: `src/ScorerApp.Web/Components/Pages/Home.razor`
Popis: Dashboard po přihlášení — souhrnná čísla, moje oddíly, přehled sportů a probíhající sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- 4 stat karty: ligy, probíhající sezóny, hráči, odehrané zápasy (UiStats)
- Panel „Moje oddíly“ (jen když nějaké jsou): oddíly, kde je uživatel hráčem nebo správcem, i oddíly jeho dětí
  (štítek rodič); max. 6 + odkaz na všechny. Tlačítka Chat a Oběžníky s počtem nepřečtených; Chat se nezobrazí
  rodiči, který v žádném oddílu sám není. Nahrazuje přehled z Home ClubManageru.
- Dlaždice sportů s počtem lig, proklik na `/leagues?sport={guid}`; tlačítko „Přidat sport“ jen pro admina
- Tabulka až 10 probíhajících sezón (liga, sport, počet zápasů, tlačítko Zápasy)
- Loading stav a prázdné stavy
- Lokalizace cs/en (klíče `Home_*`)

## Chybí / Rozpracováno ⚠️
- „Dnešní zápasy“ z původní spec 2026-05-29 nejsou implementované
- Žádný osobní sportovní kontext (moje zápasy, můj rating) — je na `/profile`
- Sezóny ve stavu Registrace se nezobrazují (jen Probíhá)
- Admin aplikace vidí v „Moje oddíly“ všechny aktivní oddíly (má přístup všude)

## Návrhy na vylepšení 💡
- Primární CTA „Nový turnaj“ — průvodce liga + sezóna + registrace v jednom kroku
- Panel „Probíhá registrace“ se sezónami ve stavu Registration
- Posledních N výsledků napříč sezónami
- Nadcházející rezervace aut v panelu Moje oddíly
- Graf aktivity přes ApexCharts (knihovna je registrovaná, ale nikde nepoužitá)

## Brainstorming poznámky
- V MAUI WebView je to vstupní obrazovka — na kurtu je důležitější akce než statistika
- Dotazy běží za sebou (sdílený DbContext nesnese paralelní dotazy); při růstu dat zvážit MemoryCache
- Kandidát na rozdělení do komponent HomeStats / MyClubsPanel / ActiveSeasonsPanel

_Stav k 2026-09-15._
