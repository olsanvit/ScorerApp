# UsersAdmin.razor
Route: `/admin/users`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/UsersAdmin.razor`
Popis: Účty a administrátoři — přidání a odebrání role Admin (sub-admini).
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Seznam účtů s hledáním podle e-mailu nebo jména (prvních 50)
- Přidání a odebrání role Admin; role i příznak `AppUser.IsAdmin` se drží v souladu
- Sobě ani poslednímu administrátorovi roli odebrat nelze (hlídá `AdminUserService`, test `AdminUsersTests`)
- Odznak blokovaného účtu, loading a prázdný stav, chyba se zobrazí v alertu
- UI je sdílená komponenta `AdminUsersPanel` ze SharedServices (texty cs/en), stránka je jen obal s nadpisem
- Odkaz v menu (sekce Administrace) i dlaždice na `/admin`

## Chybí / Rozpracováno ⚠️
- Blokování účtu (`IsBanned`) a vynucení změny hesla (`MustChangePassword`) se jen zobrazují, nepřepínají
- Role `Moderator` se nespravuje (v seedu existuje, ale nic ji nepoužívá)
- Bez stránkování — nad 50 účtů je nutné hledat

## Návrhy na vylepšení 💡
- Přepínání blokace a vynucení změny hesla
- Historie změn rolí (kdo komu kdy)

## Brainstorming poznámky
- Sdílená komponenta záměrně: admin sekci s podadminy má mít každá aplikace
- `UserAccessPage` ze SharedServices řeší jen whitelist a ScorerApp ji nepoužívá

_Stav k 2026-09-23._
