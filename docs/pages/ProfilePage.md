# ProfilePage.razor
Route: `/profile`
Soubor: `src/ScorerApp.Web/Components/Pages/ProfilePage.razor`
Popis: Profil přihlášeného uživatele — celkový rating a rating podle sportů.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Párování účtu s hráčem podle e-mailu; první shoda se uloží do Player.UserId
- Celkový rating = vážený průměr přes sporty (rating × počet her / celkem her)
- Tabulka ratingu podle sportů, proklik na detail hráče a na žebříčky
- Lokalizováno (CZ/EN)

## Chybí / Rozpracováno ⚠️
- Když účet nemá odpovídajícího hráče, stránka jen oznámí, že spárování chybí — nenabídne řešení
- Chybí moje nadcházející zápasy a moje sezóny
- Hráč se založeným e-mailem se spáruje, i když jde o jmenovce se stejnou adresou (teoretické)

## Návrhy na vylepšení 💡
- Tlačítko „požádat admina o spárování“ nebo výběr ze seznamu nespárovaných hráčů
- Sekce Moje zápasy / Moje sezóny
- Sdílení profilu veřejným odkazem

## Brainstorming poznámky
- Volba padla na párování podle e-mailu (varianta C) — admin e-mail vyplňuje v PlayerCreate/PlayerEdit
- Rating se bere ze SportRatings; dokud se nepřepočítá (uložení výsledku nebo ruční přepočet), je prázdný

_Stav k 2026-09-12._
