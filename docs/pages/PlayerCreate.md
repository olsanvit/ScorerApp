# PlayerCreate.razor
Route: `/admin/players/create`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/PlayerCreate.razor`
Popis: Formulář nového hráče.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- E-mail hráče (2026-09-12) — podle něj se páruje přihlášený účet se stránkou /profile
- Jméno, přezdívka, datum narození
- Lokalizace cs/en (klíče `PlayerForm_*`, společné s PlayerEdit)

## Chybí / Rozpracováno ⚠️
- Kontrola duplicitního jména (registrace sezóny ji od 2026-09-11 dělá, tady ne)
- Foto a kontakt z game designu

## Návrhy na vylepšení 💡
- Create a Edit jsou dvě téměř totožné stránky — sloučit do jedné s volitelným `{Id:guid?}` (ubere ~100 řádků duplicity)

## Brainstorming poznámky
- —

_Stav k 2026-09-22._
