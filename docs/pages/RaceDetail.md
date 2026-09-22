# RaceDetail.razor
Route: `/races/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/RaceDetail.razor`
Popis: Výsledky závodu — pořadí, časy, DNF.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Výsledky: pořadí, čas, DNF
- Admin: přidání výsledku s validací času hh:mm:ss, mazání s potvrzením
- Lokalizace cs/en (klíče `Race_*`)

## Chybí / Rozpracováno ⚠️
- Žádná kontrola duplicit — stejný účastník jde přidat vícekrát, pozice se mohou opakovat
- Výsledky se nepromítají do tabulky sezóny ani do ratingu
- Pořadí se nedopočítá automaticky z časů

## Návrhy na vylepšení 💡
- Body za umístění (konfigurovatelné schéma) → tabulka sezóny
- Osobní rekordy (PB) na vzdálenost
- Hromadné zadání výsledků

## Brainstorming poznámky
- Race je paralelní model k Match; deskovky podle spec půjdou přes Match.ResultJson — rozhodnout sjednocení

_Stav k 2026-09-22._
