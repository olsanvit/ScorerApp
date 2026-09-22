# LeagueCreate.razor
Route: `/admin/leagues/create`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/LeagueCreate.razor`
Popis: Formulář nové ligy.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Název, sport, popis
- Volitelný JSON override bodování s validací
- Lokalizace cs/en (klíče `LeagueForm_*`, sdílené s LeagueEdit)

## Chybí / Rozpracováno ⚠️
- Bodování jen jako surový JSON
- Typ soutěže (roční liga / turnaj) z game designu

## Návrhy na vylepšení 💡
- Formulář Win/Draw/Loss/OT s náhledem JSON
- Create a Edit jsou dvě téměř totožné stránky — sloučit do jedné s volitelným `{Id:guid?}` (ubere ~100 řádků duplicity)

## Brainstorming poznámky
- Po vytvoření rovnou nabídnout založení první sezóny

_Stav k 2026-09-22._
