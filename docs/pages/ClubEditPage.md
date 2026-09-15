# ClubEditPage.razor
Route: `/clubs/create, /clubs/{id}/edit`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/ClubEditPage.razor`
Popis: Založení a úprava oddílu (jedna komponenta, dvě routy).
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Založení: výběr organizace z těch, které uživatel spravuje; ?organizationId= předvyplní
- Úprava: název, zkratka, popis, aktivní
- Kontrola oprávnění stránkou i ClubService
- Unikátní název v organizaci s čitelnou chybou

## Chybí / Rozpracováno ⚠️
- Mazání oddílu záměrně chybí — jen deaktivace (soft delete by nechal aktivní členy a vlákna)

## Návrhy na vylepšení 💡
- Logo/barva oddílu

## Brainstorming poznámky
- Po uložení přesměruje na detail oddílu

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
