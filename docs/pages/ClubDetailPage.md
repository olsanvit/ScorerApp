# ClubDetailPage.razor
Route: `/clubs/{id}`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/ClubDetailPage.razor`
Popis: Detail oddílu: soupiska, týmy, sezóny, kód skupiny a pozvánky.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Soupiska hráčů s pozicí a indikací spárovaného účtu
- Přidání existujícího hráče (hledání) i nového jménem (Enter), odebrání s potvrzením
- Týmy oddílu: přiřazení volného týmu, uvolnění
- Sezóny, kde oddíl nastoupil (přes ClubId účastníka nebo přes svůj tým)
- Kód skupiny + regenerace s potvrzením (správce)
- Pozvánka e-mailem s rolí, seznam nepřijatých pozvánek včetně prošlých (správce)
- Zrušení pozvánky (soft delete, odkaz přestane platit) a opětovné odeslání s NOVÝM tokenem a prodlouženou platností
- Rodiče hráčů (správce): sloupec Rodiče v soupisce se zrušením propojení, formulář hráč + e-mail účtu rodiče.
  Rodič se stane členem organizace a dostává oddílové oběžníky včetně nedoplatků; chat ne.

## Chybí / Rozpracováno ⚠️
- Kopírování kódu do schránky chybí (vyžaduje JS)
- Bez SMTP konfigurace se pozvánka založí, ale e-mail neodejde — varování jen v logu
- Rodič bez účtu se propojit nedá — musí se nejdřív zaregistrovat (pozvánka rodiče s vazbou na dítě chybí)
- Chat za dítě (přepínač „zobrazuji za“ ze specifikace ClubManageru) není

## Návrhy na vylepšení 💡
- Statistiky oddílu přes všechny sezóny
- Hromadný import soupisky

## Brainstorming poznámky
- Hráč bez účtu je na soupisce, ale nedostává chat ani oběžníky — ikona to ukazuje; oběžníky za něj může dostávat propojený rodič
- Notifikace: pozvánka generuje e-mail

_Stav k 2026-09-15 — modul Kluby převzatý z ClubManageru, doplněni rodiče._
