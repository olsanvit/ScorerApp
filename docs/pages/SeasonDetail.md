# SeasonDetail.razor
Route: `/seasons/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/SeasonDetail.razor`
Popis: Detail sezóny — tabulka pořadí, závody, poslední výsledky a řízení životního cyklu.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Vizuální playoff pavouk (2026-09-12) — kola vedle sebe, nasazení, skóre, proklik na zápas
- Skupinové tabulky (jedna na skupinu) u formátu se skupinovou fází
- Výzva „generovat další fázi“, jakmile je aktuální fáze dohraná
- Do tabulky pořadí se nepočítají playoff zápasy
- Stat řádek: účastníci, zápasy, odehráno, zbývá
- Tabulka pořadí (StandingsService) — respektuje HasDraw a UseElo
- Panel závodů (Race) s přidáním závodu pro probíhající sezónu
- Posledních 5 výsledků
- Admin akce podle stavu (2026-09-11): Návrh → Otevřít registraci; Registrace → Registrace hráčů; Probíhá → Účastníci + Ukončit; Dokončena → Znovu otevřít; vždy Upravit a Smazat

## Chybí / Rozpracováno ⚠️
- Třetí místo v pavouku se nehraje (není zápas o 3. místo)
- Forma (posledních 5 zápasů V/R/P) v tabulce
- Ukončení sezóny nic nepřepočítá ani nevyhlásí vítěze
- Formát zobrazen jako enum

## Návrhy na vylepšení 💡
- Záložky Tabulka | Pavouk | Zápasy | Statistiky
- ELO sparkline u účastníků (ApexCharts)
- Export tabulky do PNG/CSV pro sdílení do skupinového chatu

## Brainstorming poznámky
- Velký Include graf (všechny zápasy s oběma účastníky) — v pořádku pro desítky zápasů
- Sloupec ELO je sezónní rating, ne celkový rating hráče — uživatelé to mohou plést

_Stav k 2026-09-12._
