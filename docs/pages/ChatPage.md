# ChatPage.razor
Route: `/chat`
Soubor: `src/ScorerApp.Web/Components/Pages/ClubModule/ChatPage.razor`
Popis: Chat oddílů ve vláknech s doručením v reálném čase.
Platforma: Obojí (ScorerApp.Mobile je MAUI WebView nad webem)

## Hotovo ✅
- Vlákna seskupená podle oddílu, počet nepřečtených
- Nové vlákno s typem (jen správce oddílu), archivace
- Real-time přes in-process ClubChatBroadcaster (bez SignalR hubu)
- Označení přečtení při otevření a při příchodu zprávy
- Notifikace e-mail/ntfy na pozadí podle priority vlákna a preferencí

## Chybí / Rozpracováno ⚠️
- Přímé zprávy (DM) a managed child ze spec zatím chybí
- UI pro NotificationPreference chybí — platí výchozí chování
- Stránkování starší historie (načte posledních 100 zpráv)
- Broadcaster funguje jen pro jednu instanci aplikace

## Návrhy na vylepšení 💡
- Editace a mazání vlastní zprávy
- Zmínky @uživatel

## Brainstorming poznámky
- Nejnovější zpráva viditelná přes flex column-reverse — bez JS scrollování
- Debt vlákno posílá e-mail vždy, i proti preferencím

_Stav k 2026-09-14 — modul Kluby převzatý z ClubManageru._
