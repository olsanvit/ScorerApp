# SportAdmin.razor
Route: `/admin/sports`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/SportAdmin.razor`
Popis: Správa sportů — inline CRUD.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Název, typ sportu, typ zápasu, typ účastníků, ikona, bodovací JSON s validací
- Soft-delete blokovaný, pokud má sport navázané ligy

## Chybí / Rozpracováno ⚠️
- Pétanque a Ticket to Ride chybí v seedu, přestože patří k pěti hlavním sportům game designu
- `SportType` je uzavřený enum — nový sport skončí jako Other
- Bodování jen jako surový JSON

## Návrhy na vylepšení 💡
- Výběr ikony z palety místo psaní `bi-*` třídy
- Nastavení formuláře výsledku per sport (návaznost na ISportResultFormatter)

## Brainstorming poznámky
- —

_Stav k 2026-09-11._
