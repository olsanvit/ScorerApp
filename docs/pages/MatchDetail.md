# MatchDetail.razor
Route: `/matches/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/MatchDetail.razor`
Popis: Detail zápasu — výsledek, oprava výsledku, události a sety.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Penaltové skóre (2026-09-12) — rozhoduje vítěze v playoff při remíze
- Po uložení i opravě výsledku navazuje postup v pavouku a přepočet ratingu sportu
- Score panel s označením prodloužení/penalt
- Zadání výsledku (skóre, datum, prodloužení, penalty) + přepočet ELO
- Oprava odehraného výsledku s potvrzením a přepočtem ELO (varovný text opraven 2026-09-11 — tvrdil, že se ELO nepřepočítá)
- Události zápasu (16 typů MatchEventType, hráči podle soupisky týmu)
- Sety s tie-breakem a tenisové shrnutí

## Chybí / Rozpracováno ⚠️
- Sport-specifické formuláře (`ISportResultFormatter`, spec 2026-09-09)
- Validace: pétanque do 13, tie-break jen při 6:6
- Multi-player zápis pro Prší a Ticket to Ride (`Match.ResultJson` neexistuje)
- Penaltové skóre: model má HomePenaltyScore/AwayPenaltyScore, UI jen checkbox
- Nadpis „Kolo X“ bez názvu sezóny a ligy

## Návrhy na vylepšení 💡
- Rozdělit 717 řádků do komponent MatchScoreEditor / MatchEventsPanel / MatchSetsPanel
- Timeline událostí místo tabulky, undo posledního eventu

## Brainstorming poznámky
- Ověřit, zda se skóre zápasu dopočítává ze setů, nebo se zadává zvlášť (riziko nesouladu)
- Opakované inline styly inputů → CSS třída

_Stav k 2026-09-12._
