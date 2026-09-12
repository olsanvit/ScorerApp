# MatchesPage.razor
Route: `/matches`
Soubor: `src/ScorerApp.Web/Components/Pages/MatchesPage.razor`
Popis: Přehled zápasů napříč všemi sezónami.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Posledních 200 zápasů (podle UpdatedAt)
- Filtr stavu: Vše / Naplánované / Odehrané / Zrušené
- Fulltext přes účastníky a sezónu

## Chybí / Rozpracováno ⚠️
- `Take(200)` bez stránkování a bez upozornění na oříznutí
- Filtr liga / sezóna / hráč (admin spec je vyžaduje)
- Stav Odložen chybí ve filtrech
- Řazení podle UpdatedAt míchá odehrané a editované zápasy

## Návrhy na vylepšení 💡
- Rozdělit na Nadcházející / Výsledky
- Filtr hráče = „moje zápasy“
- Paginator ze SharedServices

## Brainstorming poznámky
- Fulltext hledá jen v načtených 200 — starý zápas tiše nenajde

_Stav k 2026-09-11._
