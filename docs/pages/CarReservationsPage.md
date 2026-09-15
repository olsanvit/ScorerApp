# CarReservationsPage.razor
Route: `/cars/reservations`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/CarReservationsPage.razor`
Popis: Rezervace aut: seznam, kalendář, schvalování, uzavření jízdy.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Rezervace na celé dny, překryv včetně krajních dnů
- Souběh hlídá Serializable transakce
- Správce schvaluje/zamítá; řidič nebo správce ruší a uzavírá jízdu se stavem tachometru
- Obnovení zrušené rezervace znovu kontroluje volný termín
- Měsíční kalendář a filtr auta

## Chybí / Rozpracováno ⚠️
- Notifikace o schválení/zamítnutí chybí
- Vyúčtování km chybí

## Návrhy na vylepšení 💡
- Export jízd pro účetnictví

## Brainstorming poznámky
- —

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
