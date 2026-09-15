# TeamCreate.razor
Route: `/admin/teams/create`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/TeamCreate.razor`
Popis: Formulář nového týmu.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Název, zkratka, barva (color picker)

## Chybí / Rozpracováno ⚠️
- Logo
- Kontrola duplicitního názvu

## Návrhy na vylepšení 💡
- Create a Edit jsou dvě téměř totožné stránky — sloučit do jedné s volitelným `{Id:guid?}` (ubere ~100 řádků duplicity)
- Rovnou přidat hráče do soupisky

## Brainstorming poznámky
- —

_Stav k 2026-09-11._

## Kluby (2026-09-14)
- Výběr oddílu (Team.ClubId); stránka plně lokalizovaná
