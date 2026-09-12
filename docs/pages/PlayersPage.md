# PlayersPage.razor
Route: `/players`
Soubor: `src/ScorerApp.Web/Components/Pages/PlayersPage.razor`
Popis: Výpis hráčů s fulltextem.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Tabulka: jméno, přezdívka, datum narození, počet týmů
- Fulltext přes jméno a přezdívku
- Admin: Nový hráč

## Chybí / Rozpracováno ⚠️
- Rating ve výpisu — v aplikaci postavené na ELO nejdůležitější sloupec
- Řazení, filtr podle sportu, deaktivace hráče, foto

## Návrhy na vylepšení 💡
- Přeměnit na žebříček: rating per sport + vážený průměr
- Počet odehraných zápasů a poslední aktivita

## Brainstorming poznámky
- On-site registrace (2026-09-11) zakládá hráče jen jménem → brzy bude potřeba merge duplicit

_Stav k 2026-09-11._
