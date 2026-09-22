# LeaguesPage.razor
Route: `/leagues`
Soubor: `src/ScorerApp.Web/Components/Pages/LeaguesPage.razor`
Popis: Výpis všech lig s filtrem podle sportu a fulltextem.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Tabulka lig: název, sport s ikonou, počet sezón, badge probíhajících sezón
- Chip filtr podle sportu + předvyplnění z `?sport={guid}`
- Fulltext přes UiSearchBar
- Admin: tlačítko Nová liga
- Lokalizace cs/en (klíče `Leagues_*`) (2026-09-22)

## Chybí / Rozpracováno ⚠️
- Načítá všechny ligy včetně sezón do paměti a filtruje v C#
- Bez stránkování (Paginator ze SharedServices není v projektu použitý)
- Nerozlišuje roční ligu a jednodenní/vícedenní turnaj (game design to vyžaduje)
- Žádné archivování ukončených lig

## Návrhy na vylepšení 💡
- Filtr typu (liga / turnaj) a přepínač „jen aktivní“
- Řazení podle poslední aktivity místo abecedy
- Karty místo tabulky na mobilu, počet hráčů v lize

## Brainstorming poznámky
- `?sport` se čte jen v OnInitializedAsync — změna query při stejné komponentě se neprojeví
- Klik na chip nemění URL → filtr nejde sdílet odkazem

_Stav k 2026-09-22._
