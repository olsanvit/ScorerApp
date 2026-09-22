# SeasonEdit.razor
Route: `/admin/seasons/{Id:guid}/edit`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/SeasonEdit.razor`
Popis: Editace sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Stavitel modulárního formátu (2026-09-12); staré sezóny bez FormatJson se odvodí ze starého enumu
- Stejná pole jako SeasonCreate; liga je needitovatelná
- Lokalizace cs/en (klíče `SeasonForm_*`, sdílené se SeasonCreate)

## Chybí / Rozpracováno ⚠️
- Změna formátu po vygenerování rozpisu nic nepřegeneruje ani neupozorní
- Vypnutí UseElo nechá v SeasonParticipant staré hodnoty

## Návrhy na vylepšení 💡
- Upozornění, když změna ovlivní existující rozpis

## Brainstorming poznámky
- —

_Stav k 2026-09-22._
