# OrganizationDetailPage.razor
Route: `/organizations/{id}`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/OrganizationDetailPage.razor`
Popis: Detail organizace: oddíly, členové s rolemi, nastavení.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Oddíly organizace
- Rychlé odkazy: oběžníky, auta, rezervace, nedoplatky (správci)
- Správce: změna role členů, aktivace/deaktivace, přidání existujícího účtu e-mailem, úprava názvu/popisu/aktivity
- Ochrana: organizace nesmí přijít o posledního aktivního správce

## Chybí / Rozpracováno ⚠️
- Nový účet nejde založit odsud — pozvánka se posílá z detailu oddílu
- FamilyLink (rodič–dítě) nemá UI

## Návrhy na vylepšení 💡
- Správa FamilyLink
- Historie změn rolí

## Brainstorming poznámky
- Členové organizace = OPRÁVNĚNÍ (účty); soupiska oddílu = hráči

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
