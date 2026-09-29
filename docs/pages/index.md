# ScorerApp — Přehled stránek

Stav: ✅ funguje a otestováno | 🔄 rozděláno | ❌ nefunguje | ❓ neznámý stav

Doplněno 2026-09-29. „Otestováno“ znamená buď render test v `ScorerApp.Tests/Database`,
nebo proklikání v prohlížeči; u responzivity se testuje 375 px i desktop, světlý i tmavý motiv.
Sloupec Responzivní se týká úkolu 2 (redesign pro mobil) — ostatní stránky zatím používají
`table-responsive`, tedy vodorovné rolování.

## Hlavní stránky

| Stránka | Route | Stav | Responzivní | Poznámky |
|---|---|---|---|---|
| Home | `/` | ✅ | ✅ | Dashboard. Karty sezón na mobilu, statistiky zmenšené |
| Profile | `/profile` | ✅ | ❌ | Render test; redesign zbývá |
| Privacy | `/privacy` | ✅ | — | Statický text |
| Error | `/Error` | ✅ | — | Výchozí stránka Blazoru, **nelokalizovaná** (jediné místo v projektu) |
| Not found | `/not-found` | ✅ | — | |

## Ligy a soutěže

| Stránka | Route | Stav | Responzivní | Poznámky |
|---|---|---|---|---|
| Leagues | `/leagues` | ✅ | ❌ | Render test |
| League Detail | `/leagues/{id}` | ✅ | ❌ | Render test |
| Seasons | `/seasons` | ✅ | ✅ | Karty na mobilu, filtry stavu |
| Season Detail | `/seasons/{id}` | ✅ | ✅ | Tabulka pořadí skrývá vedlejší sloupce pod 576 px; závody a poslední výsledky jako karty |
| Season Matches | `/seasons/{id}/matches` | ✅ | ✅ | Rychlé zadání výsledku, karty na mobilu |
| Races | `/races` | ✅ | ❌ | Render test |
| Race Detail | `/races/{id}` | 🔄 | ❌ | Bez render testu. Bílé inline styly odstraněny, jinak neprověřeno |
| Rankings | `/rankings` | ✅ | ❌ | Render test |

## Zápasy a hráči

| Stránka | Route | Stav | Responzivní | Poznámky |
|---|---|---|---|---|
| Matches | `/matches` | ✅ | ✅ | Karty na mobilu |
| Match Detail | `/matches/{id}` | ✅ | ✅ | Oprava výsledku ověřena včetně zápisu do DB; sety jako karty |
| Players | `/players` | ✅ | ✅ | Karty na mobilu |
| Player Detail | `/players/{id}` | ✅ | ❌ | Render test |
| Teams | `/teams` | ✅ | ✅ | Karty na mobilu, prázdný stav ověřen |
| Team Detail | `/teams/{id}` | 🔄 | ❌ | Bez render testu. Bílé inline styly odstraněny, jinak neprověřeno |
| Team Season Stats | `/teams/{teamId}/seasons/{seasonId}` | ❓ | ❌ | **Jiná routa, než měl skeleton** (`/teams/{id}/stats` neexistuje). Bez testu |

## Club modul

| Stránka | Route | Stav | Responzivní | Poznámky |
|---|---|---|---|---|
| Clubs | `/clubs` | ✅ | ❌ | Render test |
| Club Detail | `/clubs/{id}` | ✅ | ❌ | Render test vč. oprávnění (kód pro správce, pozvánky) |
| Club Create | `/clubs/create` | ✅ | ❌ | Ve skeletonu chyběla |
| Club Edit | `/clubs/{id}/edit` | ✅ | ❌ | Render test vč. odepření pro člena |
| Join Club | `/join` | ✅ | ❌ | **Ne `/clubs/join`** |
| Accept Invite | `/accept-invite?token=…` | ✅ | ❌ | **Ne `/clubs/accept`** |
| Organizations | `/organizations` | ✅ | ❌ | Render test |
| Organization Detail | `/organizations/{id}` | ✅ | ❌ | Render test vč. odepření pro cizího |
| Cars | `/cars?organizationId=…` | ✅ | ❌ | Render test |
| Car Reservations | `/cars/reservations` | ✅ | ❌ | Render test |
| Circulars | `/circulars` | ✅ | ❌ | Render test |
| Circular Detail | `/circulars/{id}` | ✅ | ❌ | Render test |
| Circular Send | `/circulars/send` | ✅ | ❌ | Render test |
| Chat | `/chat` | ✅ | ❌ | Render test. Real-time přes in-process broadcaster, jen pro jednu instanci |
| Debts | `/circulars/debts` | ✅ | ❌ | **Ne `/debts`**. Render test vč. odepření pro člena |

## Admin

| Stránka | Route | Stav | Responzivní | Poznámky |
|---|---|---|---|---|
| Admin Dashboard | `/admin` | ✅ | ❌ | Render test |
| Sport Admin | `/admin/sports` | ✅ | ❌ | Render test |
| Users Admin | `/admin/users` | ✅ | ❌ | Render test; komponenta ze SharedServices |
| League Create | `/admin/leagues/create` | ✅ | ❌ | Ověřeno proklikáním |
| League Edit | `/admin/leagues/{id}/edit` | ❓ | ❌ | Bez testu |
| Season Create | `/admin/seasons/create` | ✅ | ❌ | Ověřeno proklikáním vč. výběru formátu |
| Season Edit | `/admin/seasons/{id}/edit` | ❓ | ❌ | Bez testu |
| Team Create | `/admin/teams/create` | ✅ | ❌ | Render test |
| Team Edit | `/admin/teams/{id}/edit` | ❓ | ❌ | Bez testu |
| Player Create | `/admin/players/create` | ❓ | ❌ | Bez testu |
| Player Edit | `/admin/players/{id}/edit` | ❓ | ❌ | Bez testu |
| Race Create | `/admin/races/create` | ❓ | ❌ | Bez testu |
| Generate Matches | `/admin/seasons/{id}/generate` | ✅ | ❌ | Ověřeno proklikáním (6 zápasů) |
| Season Participants | `/admin/seasons/{id}/participants` | ✅ | ❌ | Ověřeno proklikáním vč. hromadného přidání |

## Účty (SharedServices)

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Login | `/login` | ✅ | Ověřeno proklikáním |
| Logout | `/logout` | ✅ | |
| Register | `/register` | ✅ | Jen pro roli Admin |
| Change Password | `/change-password` | ✅ | Ověřeno v obou jazycích po lokalizaci (SharedServices f7d5877) |
| Forgot / Reset Password | `/forgot-password`, `/reset-password` | 🔄 | Bez SMTP nelze dokončit — e-mail se neodešle |
| Access Pending | `/access-pending` | ✅ | |

## Co chybí nebo je rozděláno

- **Responzivní redesign** hotový u 8 stránek; zbývají detaily hráče/týmu/ligy, závody,
  rankings, profil, klubové a admin stránky.
- **SMTP není nakonfigurované** → pozvánky, oběžníky a reset hesla se neodešlou.
- **Google login** zapojený v kódu, ale v produkční konfiguraci chybí klíče.
- **`Error.razor`** jako jediná stránka není lokalizovaná.
- **Přímé zprávy (DM)** v chatu: model `ThreadParticipant` existuje, `ChatService` ale nemá
  cestu, jak DM vlákno založit (ve specifikaci ClubManageru bylo až ve V2).
- Stránky se stavem ❓ nemají render test ani proklikání — u CRUD formulářů admina
  to je největší nepokryté místo.
