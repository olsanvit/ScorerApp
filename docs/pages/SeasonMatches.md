# SeasonMatches.razor
Route: `/seasons/{Id:guid}/matches`
Soubor: `src/ScorerApp.Web/Components/Pages/SeasonMatches.razor`
Popis: Rozpis zápasů sezóny po kolech s rychlým zadáním výsledku.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Seskupení podle fáze (2026-09-12): „Skupina A — kolo 1“, „Swiss — kolo 2“, „Semifinále“ — playoff už nesplývá s ligovými koly
- Po uložení výsledku se posune pavouk a přepočítá trvalý rating sportu
- Zápasy seskupené po kolech, počet odehraných v kole
- Skutečná kola od 2026-09-11 — okružní rozpis n-1 kol po n/2 zápasech (dříve měl každý zápas vlastní „kolo“)
- Admin: rychlé zadání skóre s kontrolou prázdných polí, po uložení přepočet ELO celé sezóny
- Odkaz na detail zápasu

## Chybí / Rozpracováno ⚠️
- Sport-specifický zápis (sety, body, multi-player) — jen na detailu a jen obecně
- Hromadné uložení více výsledků
- Klávesová obsluha Tab/Enter mezi poli
- Filtr „jen neodehrané“
- Při lichém počtu není vidět, kdo má v kole volno

## Návrhy na vylepšení 💡
- Velká dotyková čísla pro zadávání na mobilu
- „Live“ režim jednoho probíhajícího zápasu
- Sticky hlavička kola

## Brainstorming poznámky
- RecomputeSeasonEloAsync je zkopírovaný i v MatchDetail — přesunout do služby
- Řazení v kole podle MatchDate, které je většinou null

_Stav k 2026-09-12._
