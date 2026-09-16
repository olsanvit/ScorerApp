# ScorerApp — Co projekt umí (2026-09-08)

> Přehled implementovaných funkcí na základě analýzy kódu. Stav ke dni 2026-09-08.

---

## Databázový model (EF Core / PostgreSQL)

Všechny entity dědí z `BaseGuid` (SharedServices): GUID PK, `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy`, soft-delete (`IsDeleted/DeletedAt/DeletedBy`), `Emoji`, `Colors`.

4 skutečné migrace: Initial (2026-05-31), AddIsWhitelisted, AlignAppUserWithSharedServices, FixPendingModelChanges (2026-06-30).

| Entita | Obsah |
|---|---|
| `Sport` | Název, `SportType` (Football/IceHockey/Basketball/Tennis/Darts/Padel/Cards/Running/Other), `SportMatchType` (HeadToHead/MultiParticipant), `ParticipantKind` (Team/Individual), ikona Bootstrap, výchozí bodovací schéma jako JSON |
| `League` | Název, popis, vazba na Sport, volitelný JSON scoring override |
| `Season` | Vazba na League, název, rok, `SeasonFormat` (RoundRobin/DoubleRoundRobin/Playoff/GroupsAndKnockout/Custom), `SeasonStatus` (Planning/Active/Finished), UseElo flag, StartDate/EndDate |
| `SeasonParticipant` | Účastník sezóny — buď Player nebo Team (nullable FK), `EloRating` (decimal, výchozí 1000), computed `DisplayName` |
| `Match` | Kolo, HomeParticipant/AwayParticipant, datum, `MatchStatus` (Scheduled/Played/Cancelled/Postponed), Home/Away Score, ExtraTime/Penalties flag, penaltové skóre, poznámky |
| `MatchEvent` | Gól, asistence, karta, čisté konto; šipky (180, High Checkout, Leg); tenis (Ace, DoubleFault, Set); minuta, vazba na hráče, volitelná hodnota a poznámka |
| `MatchSet` | Set výsledek (číslo setu, home/away games, volitelný tiebreak) |
| `Player` | Jméno, přezdívka, datum narození |
| `Team` | Název, zkratka, barva (hex) |
| `TeamPlayer` | Vazba hráč–tým, pozice, datum příchodu/odchodu |
| `Race` | Závod v sezóně, vzdálenost + jednotka, datum, poznámky |
| `RaceResult` | Účastník, pozice, čas (TimeSpan), DNF flag |

Enumerace (`Enums.cs`): `SportType`, `SportMatchType`, `ParticipantKind`, `SeasonFormat`, `SeasonStatus`, `MatchStatus`, `MatchEventType` (16 typů)

**Globální soft-delete filtr** — automaticky vylučuje `IsDeleted = true` ze všech dotazů.

---

## Domain Services

### `EloService`

- `Calculate(homeElo, awayElo, homeResult, k=32)` — standardní Elo vzorec, vrací (newHome, newAway)
- `RecomputeSeason(participants, playedMatchesInOrder, k=32)` — deterministický přepočet celé sezóny od StartElo=1000

### `ScoringRulesService` + `ScoringRules`

- `Resolve(sport, league?)` — deserializuje JSON schéma ze Sportu nebo (pokud existuje) z přepsaného schématu Ligy
- `ScoringRules`: Win, Draw, Loss, WinOT, LossOT, HasDraw
- Odolné vůči poškozenému JSON (try-catch, fallback na výchozí pravidla)

### `StandingsService`

- `Calculate(season, playedMatches, rules)` — tabulka: Played/Won/Drawn/Lost/WonOT/LostOT/GoalsFor/GoalsAgainst/GoalDiff/Points/EloRating
- Řazení: Points → GoalDiff → GoalsFor
- Podporuje OT výhry/prohry (hokej) i sezóny bez remíz

### `MatchGeneratorService`

- `Generate(season, participants)` — generuje zápasy pro RoundRobin nebo DoubleRoundRobin
- DoubleRoundRobin: opakuje páry se prohozením home/away

---

## Blazor stránky — uživatelské

Všechny pod `[Authorize]`, `@rendermode InteractiveServer`:

| Stránka | Route | Co dělá |
|---|---|---|
| `Home.razor` | `/` | Stat karty (ligy, aktivní sezóny, hráči, odehrané zápasy), přehled sportů s počtem lig, posledních 5 zápasů |
| `LeaguesPage.razor` | `/leagues` | Výpis lig, chip filtry dle sportu, fulltextové vyhledávání; Admin: tlačítko Nová liga |
| `LeagueDetail.razor` | `/leagues/{id}` | Detail ligy, výpis sezón se statusem a formátem; Admin: Upravit, Nová sezóna, soft-delete |
| `SeasonsPage.razor` | `/seasons` | Výpis sezón, filtr dle statusu (Planning/Active/Finished), vyhledávání; Admin: Nová sezóna |
| `SeasonDetail.razor` | `/seasons/{id}` | Ligová tabulka (StandingsService), posledních 5 zápasů, seznam závodů; Admin: Aktivovat/Dokončit, Spravovat účastníky, Upravit, Smazat |
| `SeasonMatches.razor` | `/seasons/{id}/matches` | Výpis zápasů po kolech s výsledkem a statusem; Admin: inline zadání výsledku (okamžitý přepočet ELO), generovat zápasy |
| `MatchesPage.razor` | `/matches` | Přehled zápasů přes všechny sezóny, filtr dle statusu, fulltextové vyhledávání |
| `MatchDetail.razor` | `/matches/{id}` | Výsledek, status, oba účastníci; Admin: zadání/oprava výsledku (skóre, datum, OT, PK) s přepočtem Elo; Admin: přidávání/mazání match eventů (gól, asistence, karta, …) a setů (pro tenis/padel) |
| `PlayersPage.razor` | `/players` | Výpis hráčů, vyhledávání, počet sezón; Admin: Nový hráč |
| `PlayerDetail.razor` | `/players/{id}` | Profil (jméno, přezdívka, věk), týmy, přehled sezón s ELO, statistiky eventů (góly, karty, …); Admin: Upravit |
| `TeamsPage.razor` | `/teams` | Výpis týmů s barvou, zkratkou, počtem hráčů; Admin: Nový tým |
| `TeamDetail.razor` | `/teams/{id}` | Profil týmu (barva, zkratka), sestava hráčů s pozicí a datem příchodu; Admin: přidání/odebrání hráče, Upravit |
| `TeamSeasonStats.razor` | `/teams/{teamId}/seasons/{seasonId}` | Played/Won/Drawn/Lost/GoalsFor/GoalsAgainst, výpis zápasů týmu v sezóně, top 10 střelců (z MatchEvents) |
| `RacesPage.razor` | `/races` | Přehled závodů s počtem výsledků |
| `RaceDetail.razor` | `/races/{id}` | Výsledky závodu (pořadí, čas, DNF); Admin: přidávání a mazání výsledků |

---

## Admin stránky (`/admin/...`) — pouze role Admin

| Stránka | Route | Co dělá |
|---|---|---|
| `AdminDashboard.razor` | `/admin` | Rozcestník (Nová liga, Nová sezóna, Nový hráč, Nový tým, Správa sportů) |
| `LeagueCreate.razor` | `/admin/leagues/create` | Formulář: název, sport, popis, JSON scoring override |
| `LeagueEdit.razor` | `/admin/leagues/{id}/edit` | Editace stejných polí |
| `SeasonCreate.razor` | `/admin/seasons/create` | Liga, název, rok, formát, UseElo, StartDate/EndDate |
| `SeasonEdit.razor` | `/admin/seasons/{id}/edit` | Editace stejných polí |
| `SeasonParticipants.razor` | `/admin/seasons/{id}/participants` | Správa účastníků: přidání hráče nebo týmu (nevypíše již přidané), odebrání (blokováno pokud má zápasy) |
| `SeasonGenerateMatches.razor` | `/admin/seasons/{id}/generate` | Preview počtu zápasů, generuje RR/DRR (starý rozpis bez výsledků se smaže; s výsledky zablokováno) |
| `PlayerCreate.razor` | `/admin/players/create` | Jméno, přezdívka, datum narození |
| `PlayerEdit.razor` | `/admin/players/{id}/edit` | Editace stejných polí |
| `TeamCreate.razor` | `/admin/teams/create` | Název, zkratka, color picker |
| `TeamEdit.razor` | `/admin/teams/{id}/edit` | Editace stejných polí |
| `RaceCreate.razor` | `/admin/races/create` | Sezóna, název, vzdálenost + jednotka, datum, poznámky |
| `SportAdmin.razor` | `/admin/sports` | Inline přidání/editace sportů (název, typ, bodovací JSON), soft-delete sportu |

---

## Autentizace a autorizace

Poskytováno ze SharedServices:

- ASP.NET Core Identity + Google OAuth (+ Facebook/Microsoft/GitHub/Apple podmíněně)
- `AppUser` rozšiřuje `IdentityUser` o `IsAdmin`, `IsWhitelisted`, `MustChangePassword`
- Role: Admin, Moderator, LoginUser
- `AccessGate` — Dev mode (jen owner email) nebo Whitelist mode (`Authentication:AccessMode = "Whitelist"`)
- **MustChangePassword flow** — po prvním přihlášení redirect na změnu hesla
- Vlastní Google OAuth minimal API endpointy (obchází výchozí Blazor Identity UI)
- Kompletní Identity stránky ze SharedServices: Login, Register, Logout, ForgotPassword, ResetPassword, ChangePassword, AccessPending

---

## Mobilní aplikace (MAUI)

Projekt: `ScorerApp.Mobile`

- MAUI WebView wrapper — zobrazuje `https://scorerapp.vo2info.cz` jako embedded WebView
- Google OAuth handoff přes systémový prohlížeč (Chrome Custom Tabs)
- Deep link auth: `scorerapp://auth?token=...` → server endpoint `/mobile-token-login`
- Tokeny v `ConcurrentDictionary`, TTL 2 minuty, jednorázové

---

## Testy

`ScorerApp.Tests` (xUnit):

- `EloServiceTests` — 5 testů (HomeWin, AwayWin, Draw, Conservation of sum, Upset)
- `ScoringRulesServiceTests` — Football výchozí, Hockey s OT, League JSON override, fallback
- `MatchGeneratorServiceTests` — generování zápasů (RR, DRR)
- `IntegrationTests` — integrační testy s DB

---

## Seed dat

`SeedData.cs` — při startu automaticky vloží 8 sportů s bodovacími schématy v JSON:
Football, Ice Hockey, Basketball, Tennis, Darts, Padel, Cards, Running.

---

## SharedServices — co poskytuje ScorerApp

- **Auth**: `AddMabAuth<T>()`, AccessGate, OAuth providers, Admin User Seeder
- **UI komponenty**: Toast, ConfirmDialog, UiSearchBar, UiStats, ThemePicker, ConnectionBanner, ReconnectModal, GlobalAlert
- **Services**: ToastService, ConfirmService, ThemeService, UiLibraryService, AuditInterceptor
- **Base entity**: `BaseGuid` (GUID, audit, soft-delete)
- **Lokalizace**: `AddSimpleLocalization()`
- **`AddSharedUI()`**: Blazored.Modal, LocalStorage, SessionStorage, ApexCharts, MemoryCache

---

## Technická infrastruktura

- Blazor Server, .NET 10, `@rendermode InteractiveServer`
- PostgreSQL (Npgsql) s `EnableDynamicJson()`
- Serilog — konzole + rolling file (30 dní) + Serilog.Exceptions
- `IDbContextFactory` + scoped DbContext (dual registrace pro Blazor)
- `/health` HealthChecks endpoint
- PathBase podpora (provoz pod `/scorer` subpathí)
- ForwardedHeaders (Nginx reverse proxy)
- UseRequestLocalization + MapMabCultureEndpoint
- ApexCharts registrováno (zatím nevyužito na stránkách)
- MudBlazor registrováno (zatím UI využívá Bootstrap)
- Auto-migrate + auto-seed při startu

---

## Co chybí (dle spec)

| Funkce | Status |
|---|---|
| Ranking/ELO zobrazení na profilu hráče (`/profile`) | ❌ stránka `/profile` (celkový avg rating) chybí — jen `PlayerDetail` |
| On-site signup (`/tournament/{id}/checkin`) | ❌ chybí |
| Flexibilní přidání hráčů na místě | ❌ pouze předregistrovaní účastníci |
| Turnajový pavouk (single elimination) | ❌ formát Playoff existuje v enumu, ale generátor řeší jen RR/DRR |
| Odměny (daily/weekly/season missions) | ❌ chybí |
| Přátelé a sociální funkce | ❌ chybí |
| Sdílení výsledků | ❌ chybí |
| Porovnání statistik s přáteli | ❌ chybí |
