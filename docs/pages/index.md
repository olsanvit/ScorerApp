# ScorerApp — Přehled stránek

Stav: ✅ funguje a otestováno | 🔄 rozděláno | ❌ nefunguje | ❓ neznámý stav

## Hlavní stránky

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Home | `/` | ❓ | Úvodní stránka |
| Profile | `/profile` | ❓ | Profil uživatele |

## Ligy a soutěže

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Leagues | `/leagues` | ❓ | Přehled lig |
| League Detail | `/leagues/{id}` | ❓ | Detail ligy |
| Seasons | `/seasons` | ❓ | Sezóny |
| Season Detail | `/seasons/{id}` | ❓ | Detail sezóny |
| Season Matches | `/seasons/{id}/matches` | ❓ | Zápasy sezóny |
| Races | `/races` | ❓ | Závody |
| Race Detail | `/races/{id}` | ❓ | Detail závodu |
| Rankings | `/rankings` | ❓ | Žebříčky |

## Zápasy a hráči

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Matches | `/matches` | ❓ | Přehled zápasů |
| Match Detail | `/matches/{id}` | ❓ | Detail zápasu |
| Players | `/players` | ❓ | Přehled hráčů |
| Player Detail | `/players/{id}` | ❓ | Detail hráče |
| Teams | `/teams` | ❓ | Přehled týmů |
| Team Detail | `/teams/{id}` | ❓ | Detail týmu |
| Team Season Stats | `/teams/{id}/stats` | ❓ | Statistiky týmu v sezóně |

## Club modul

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Clubs | `/clubs` | ❓ | Přehled klubů |
| Club Detail | `/clubs/{id}` | ❓ | Detail klubu |
| Club Edit | `/clubs/{id}/edit` | ❓ | Editace klubu |
| Join Club | `/clubs/join` | ❓ | Vstup do klubu |
| Accept Invite | `/clubs/accept` | ❓ | Přijetí pozvánky |
| Organizations | `/organizations` | ❓ | Organizace |
| Organization Detail | `/organizations/{id}` | ❓ | Detail organizace |
| Cars | `/cars` | ❓ | Vozidla |
| Car Reservations | `/cars/reservations` | ❓ | Rezervace vozidel |
| Circulars | `/circulars` | ❓ | Oběžníky |
| Circular Detail | `/circulars/{id}` | ❓ | Detail oběžníku |
| Circular Send | `/circulars/send` | ❓ | Odeslání oběžníku |
| Chat | `/chat` | ❓ | Chat |
| Debts | `/debts` | ❓ | Dluhy / platby |

## Admin

| Stránka | Route | Stav | Poznámky |
|---|---|---|---|
| Admin Dashboard | `/admin` | ❓ | Admin přehled |
| Sport Admin | `/admin/sports` | ❓ | Správa sportů |
| Users Admin | `/admin/users` | ❓ | Správa uživatelů |
| League Create/Edit | `/admin/leagues/*` | ❓ | CRUD lig |
| Season Create/Edit | `/admin/seasons/*` | ❓ | CRUD sezón |
| Team Create/Edit | `/admin/teams/*` | ❓ | CRUD týmů |
| Player Create/Edit | `/admin/players/*` | ❓ | CRUD hráčů |
| Race Create | `/admin/races/create` | ❓ | Vytvoření závodu |
| Generate Matches | `/admin/seasons/{id}/generate` | ❓ | Generování zápasů |
| Season Participants | `/admin/seasons/{id}/participants` | ❓ | Účastníci sezóny |

## TODO / chybí
- [ ] Doplnit skutečný stav každé stránky (session ScorerApp)
