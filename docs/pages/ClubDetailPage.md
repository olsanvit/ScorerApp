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
- Pozvánka e-mailem s rolí, seznam čekajících pozvánek (správce)

## Chybí / Rozpracováno ⚠️
- Pozvánku nejde zrušit ani poslat znovu
- Kopírování kódu do schránky chybí (vyžaduje JS)
- Bez SMTP konfigurace se pozvánka založí, ale e-mail neodejde — varování jen v logu

## Návrhy na vylepšení 💡
- Statistiky oddílu přes všechny sezóny
- Hromadný import soupisky

## Brainstorming poznámky
- Hráč bez účtu je na soupisce, ale nedostává chat ani oběžníky — ikona to ukazuje
- Notifikace: pozvánka generuje e-mail

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
