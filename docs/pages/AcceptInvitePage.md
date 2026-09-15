# AcceptInvitePage.razor
Route: `/accept-invite?token=`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/AcceptInvitePage.razor`
Popis: Přijetí pozvánky do oddílu.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Vyžaduje přihlášení; pozvánku přijme jen účet se stejným e-mailem
- Role z pozvánky nikomu nesníží stávající roli

## Chybí / Rozpracováno ⚠️
- Nový uživatel se musí nejdřív zaregistrovat — v režimu Whitelist ho musí admin povolit

## Návrhy na vylepšení 💡
- Registrace přímo s tokenem (musela by běžet mimo interaktivní circuit kvůli cookie)

## Brainstorming poznámky
- ClubManager zde přihlašoval uvnitř circuitu — cookie se tam zapsat nedá, proto nový postup

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
