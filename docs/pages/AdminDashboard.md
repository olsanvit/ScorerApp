# AdminDashboard.razor
Route: `/admin`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/AdminDashboard.razor`
Popis: Admin rozcestník (role Admin).
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- 6 dlaždic: Nová liga, Nová sezóna, Nový hráč, Nový tým, Ligy, Sporty

## Chybí / Rozpracováno ⚠️
- Žádná čísla ani stav aplikace
- Chybí odkaz na nový závod
- Správa uživatelů / whitelistu (SharedServices umí, ScorerApp nemá UI — jen odkaz „Registrovat uživatele“ v menu)
- Ruční přepočet ELO / ratingu

## Návrhy na vylepšení 💡
- Rychlý přístup ke sezónám v Registraci
- Poslední aktivita z audit polí (CreatedBy/UpdatedBy)

## Brainstorming poznámky
- Stránka je čistě navigační — mohla by se nahradit sekcí v NavMenu

_Stav k 2026-09-11._
