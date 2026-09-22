# TeamDetail.razor
Route: `/teams/{Id:guid}`
Soubor: `src/ScorerApp.Web/Components/Pages/TeamDetail.razor`
Popis: Detail týmu — sestava a sezóny.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Profil (barva, zkratka)
- Sestava s pozicí a datem příchodu; admin přidání/odebrání hráče
- Sezóny týmu se stavem (badge ze `SeasonStatusExtensions`) a ELO
- Lokalizace cs/en (klíče `TeamDetail_*`)

## Chybí / Rozpracováno ⚠️
- Historie soupisky — odebrání hráče záznam TeamPlayer smaže, místo aby nastavilo LeftAt
- Logo, výsledky napříč sezónami

## Návrhy na vylepšení 💡
- TeamSeasonStats jako záložka s výběrem sezóny
- All-time nejlepší střelci týmu

## Brainstorming poznámky
- Model TeamPlayer má JoinedAt/LeftAt, UI využívá jen JoinedAt

_Stav k 2026-09-22._
