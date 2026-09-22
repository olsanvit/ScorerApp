# PlayerDetail.razor
Route: `/players/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/PlayerDetail.razor`
Popis: Profil hráče — informace, týmy, kariérní statistiky a historie sezón.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Informace (jméno, přezdívka, narození) a týmy s pozicí
- Kariérní statistiky z MatchEvents (góly, asistence, karty, esa, legy…)
- Historie sezón se sezónním ELO
- Oprava 2026-09-11: dva souběžné EF dotazy nad jedním DbContextem (`Task.WhenAll`) → sekvenčně
- Lokalizace cs/en (klíče `PlayerDetail_*`)

## Chybí / Rozpracováno ⚠️
- Celkový rating napříč sporty (vážený průměr z game designu)
- Head-to-head bilance proti konkrétnímu soupeři
- Forma, graf vývoje ELO, výsledky závodů

## Návrhy na vylepšení 💡
- „vs“ widget — výběr soupeře a vzájemná bilance
- Série výher, nejlepší sport
- Odkaz na statistiky hráče v sezóně (stránka zatím neexistuje)

## Brainstorming poznámky
- Statistiky míchají sporty — góly z fotbalu a esa z tenisu v jednom seznamu; seskupit podle sportu

_Stav k 2026-09-22._
