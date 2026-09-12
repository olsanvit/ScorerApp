# SeasonParticipants.razor
Route: `/admin/seasons/{Id:guid}/participants`
Soubor: `src/ScorerApp.Web/Components/Pages/Admin/SeasonParticipants.razor`
Popis: Registrace účastníků sezóny včetně přidávání na místě (on-site).
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem — vlastní mobilní UI neexistuje)

## Hotovo ✅
- Přepsáno 2026-09-11 podle spec on-site registrace
- Vyhledání existujících hráčů/týmů s živým filtrem (max 8 návrhů)
- Přidání nového hráče/týmu jménem (Enter), existující záznam se stejným jménem se použije znovu
- Hromadné vložení — jedno jméno na řádek
- První přidání přepne sezónu Návrh → Registrace
- „Uzavřít registraci a generovat zápasy“ s potvrzením (vyžaduje ≥ 2 účastníky) → Probíhá
- Po uzavření jen pro čtení; odebrání blokované, pokud má účastník zápasy

## Chybí / Rozpracováno ⚠️
- Check-in stránka `/tournament/{id}/checkin` (hráči se přihlásí sami, třeba přes QR)
- Nasazení podle ratingu při generování
- Dva různí lidé se stejným jménem splynou do jednoho hráče

## Návrhy na vylepšení 💡
- Při shodě jména nabídnout „použít existujícího / založit nového“ s přezdívkou
- Pořadí přihlášení jako fallback pro nasazení

## Brainstorming poznámky
- Porovnání jmen ignoruje velikost písmen (`ToLower`), diakritiku ale bere přesně — „Jiri“ a „Jiří“ vzniknou jako dva hráči
- Blazored.Typeahead ze spec není v projektu nainstalovaný; vyhledávání je řešené vlastním filtrem bez nové závislosti

_Stav k 2026-09-11._
