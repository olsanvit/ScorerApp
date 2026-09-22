# SeasonCreate.razor
Route: `/admin/seasons/create`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/SeasonCreate.razor`
Popis: Formulář nové sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Stavitel modulárního formátu (2026-09-12): šablony + vlastní skládání modulů RoundRobin/Skupiny/Swiss/Playoff s náhledem
- Liga (předvyplnění `?leagueId`), název, rok, formát, začátek/konec, UseElo
- Zakládá ve stavu Návrh a přesměruje na registraci účastníků
- Lokalizace cs/en (klíče `SeasonForm_*`, sdílené se SeasonEdit)

## Chybí / Rozpracováno ⚠️
- Názvy formátů bez lokalizace
- Počet postupujících do playoff

## Návrhy na vylepšení 💡
- Šablony formátu jako tlačítka (spec 2026-09-09)
- Náhled textem: „round-robin → playoff top 8“
- Předvyplnění názvu a roku

## Brainstorming poznámky
- —

_Stav k 2026-09-22._
