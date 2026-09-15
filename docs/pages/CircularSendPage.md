# CircularSendPage.razor
Route: `/circulars/send`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/CircularSendPage.razor`
Popis: Odeslání oběžníku oddílu nebo celé organizaci.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Správce organizace smí psát celé organizaci, správce oddílu jen svým oddílům
- Náhled příjemců s účtem
- Typ, předmět, text, e-mail/ntfy
- Uložení v transakci, doručení mimo ni — selhání doručení oběžník nezruší

## Chybí / Rozpracováno ⚠️
- Koncepty (stav Draft) nejdou uložit bez odeslání
- Opakované doručení selhaných příjemců chybí

## Návrhy na vylepšení 💡
- Plánované odeslání
- Příloha

## Brainstorming poznámky
- Text se v e-mailu HTML-escapuje

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
