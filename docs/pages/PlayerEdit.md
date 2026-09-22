# PlayerEdit.razor
Route: `/admin/players/{Id:guid}/edit`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/PlayerEdit.razor`
Popis: Editace hráče.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- E-mail hráče + informace o spárování s účtem (2026-09-12)
- Jméno, přezdívka, datum narození
- Lokalizace cs/en (klíče `PlayerForm_*`, společné s PlayerCreate)

## Chybí / Rozpracováno ⚠️
- Merge duplicitních hráčů (admin spec)
- Deaktivace hráče

## Návrhy na vylepšení 💡
- Create a Edit jsou dvě téměř totožné stránky — sloučit do jedné s volitelným `{Id:guid?}` (ubere ~100 řádků duplicity)
- Sekce „Nebezpečná zóna“ s merge a deaktivací

## Brainstorming poznámky
- —

_Stav k 2026-09-22._
