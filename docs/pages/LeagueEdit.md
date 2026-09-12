# LeagueEdit.razor
Route: `/admin/leagues/{Id:guid}/edit`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/LeagueEdit.razor`
Popis: Editace ligy.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Stejná pole jako LeagueCreate včetně validace JSON

## Chybí / Rozpracováno ⚠️
- Změna sportu u ligy s odehranými sezónami není ošetřená (bodování i typ zápasu se změní zpětně)

## Návrhy na vylepšení 💡
- Create a Edit jsou dvě téměř totožné stránky — sloučit do jedné s volitelným `{Id:guid?}` (ubere ~100 řádků duplicity)

## Brainstorming poznámky
- —

_Stav k 2026-09-11._
