# ScorerApp — Design Spec
**Datum:** 2026-05-29  
**Stav:** Schváleno

---

## Co aplikace dělá

ScorerApp je multi-sport liga a turnajový manažer. Uživatel vytvoří ligu pro zvolený sport, přidá sezónu, zaregistruje hráče nebo týmy, nechá vygenerovat zápasy a průběžně zadává výsledky. Výstupem jsou tabulky pořadí, výsledky, statistiky hráčů a ELO rating.

---

## Tech stack

- **.NET 10 Blazor Server** — `@rendermode InteractiveServer`
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1`
- **ASP.NET Core Identity** — login/registrace, role `Admin`
- **SharedServices** git submodule (`src/SharedServices`) — `BaseGuid`, `UiSearchBar`, `Paginator`, `ConfirmDialog`, `PageLoadingSpinner`, `ToastService`, `ThemePicker`
- **Bootstrap 5 + Bootstrap Icons** — glassmorphism CSS (stejný styl jako ostatní projekty v mono-repo)
- **Blazor-ApexCharts** — ELO graf, standings bar chart
- **Serilog** → Console + File + PostgreSQL sink

---

## Datový model

### Enumerace

```csharp
enum SportType       { Football, IceHockey, Basketball, Tennis, Darts, Padel, Cards, Running, Other }
enum MatchType       { HeadToHead, MultiParticipant }   // HeadToHead = 1v1/team, MultiParticipant = závody
enum ParticipantKind { Team, Individual }
enum SeasonFormat    { RoundRobin, DoubleRoundRobin, Playoff, GroupsAndKnockout, Custom }
enum SeasonStatus    { Planning, Active, Finished }
enum MatchStatus     { Scheduled, Played, Cancelled, Postponed }
enum MatchEventType  { Goal, Assist, YellowCard, RedCard, PenaltyGoal, OwnGoal,
                       CleanSheet, PlusMinus, PenaltyMissed, Checkout180,
                       HighCheckout, LegWon, Ace, DoubleFault, SetResult, Other }
```

### Modely

```
Sport : BaseGuid
  Name, Type (SportType), MatchType (MatchType), ParticipantKind
  Icon (Bootstrap icon name, e.g. "bi-dribbble")
  ScoringRulesJson (string)   ← default bodovací schéma jako JSON

League : BaseGuid
  Name, Description?
  SportId → Sport
  ScoringRulesOverrideJson?   ← přepíše Sport.ScoringRulesJson pokud vyplněno
  CreatedUtc

Season : BaseGuid
  LeagueId → League
  Name (např. "2024/25"), Year (int)
  Format (SeasonFormat), Status (SeasonStatus)
  UseElo (bool)
  StartDate? (DateOnly), EndDate? (DateOnly)

Player : BaseGuid
  Name, Nickname?, DateOfBirth?

Team : BaseGuid
  Name, ShortName?, Color?
  TeamPlayers → List<TeamPlayer>

TeamPlayer : BaseGuid
  TeamId → Team, PlayerId → Player
  Position?, JoinedDate?, LeftDate?

SeasonParticipant : BaseGuid
  SeasonId → Season
  PlayerId?  → Player    ← jedno z těchto dvou je vyplněno
  TeamId?    → Team
  EloRating (decimal, default 1000)
  DisplayName (computed: Player.Name ?? Team.Name)

Match : BaseGuid
  SeasonId → Season
  Round (int)
  HomeParticipantId → SeasonParticipant
  AwayParticipantId → SeasonParticipant
  MatchDate? (DateOnly)
  Status (MatchStatus)
  HomeScore?, AwayScore? (int?)
  ExtraTime? (bool), Penalties? (bool)
  HomePenaltyScore?, AwayPenaltyScore? (int?)
  Notes?
  MatchEvents → List<MatchEvent>
  MatchSets   → List<MatchSet>

MatchEvent : BaseGuid
  MatchId → Match
  Minute? (int)
  Type (MatchEventType)
  PlayerId? → Player    ← hráč, který event udělal (nullable pro team eventy)
  Value? (int)          ← např. +1/-1 pro PlusMinus, číslo checkoutu pro darts
  Notes?

MatchSet : BaseGuid                  ← pro tenis, squash, darts sety
  MatchId → Match
  SetNumber (int)
  HomeGames, AwayGames (int)
  TiebreakHome?, TiebreakAway? (int?)

Race : BaseGuid                      ← pro běh, cyklistiku a jiné časové sporty
  SeasonId → Season
  Name, Distance?, DistanceUnit?
  RaceDate? (DateOnly)
  Notes?
  RaceResults → List<RaceResult>

RaceResult : BaseGuid
  RaceId → Race
  ParticipantId → SeasonParticipant
  Position? (int)
  FinishTime? (TimeSpan)
  Dnf (bool, Did Not Finish)
  Notes?
```

### Bodovací schéma (JSON)

Uloženo v `Sport.ScoringRulesJson`, přepsat lze v `League.ScoringRulesOverrideJson`.

```json
// Fotbal
{ "win": 3, "draw": 1, "loss": 0, "winOT": null, "lossOT": null, "hasDraw": true }

// Lední hokej
{ "win": 3, "draw": null, "loss": 0, "winOT": 2, "lossOT": 1, "hasDraw": false }

// Basketbal / Darts
{ "win": 2, "draw": null, "loss": 0, "winOT": null, "lossOT": null, "hasDraw": false }
```

---

## Architektura — přístup A (unified model + EventType enum)

Jeden `Match` model pokrývá všechny head-to-head sporty. Sport-specific pole jsou nullable. Detailní statistiky se ukládají jako `MatchEvent` záznamy s enumem `MatchEventType`. Set-based sporty (tenis, darts) používají `MatchSet`. Časové sporty (běh) používají `Race` + `RaceResult` místo `Match`.

---

## Domain services

### MatchGeneratorService
- Vstup: `SeasonId`, `SeasonFormat`, seznam `SeasonParticipant`
- Round Robin: generuje `n*(n-1)/2` zápasů, přiřazuje kola
- Double Round Robin: každý pár 2×, swap home/away
- Playoff: generuje pavouk dle počtu účastníků (8, 16…)
- Výstup: `List<Match>` v stavu `Scheduled`

### StandingsService
- Vstup: `SeasonId`
- Načte odehrané `Match` záznamy, aplikuje bodovací schéma
- Výstup: `List<StandingsRow>` (účastník, body, zápasy, skóre, ELO)
- ELO update po každém zadaném výsledku (K-faktor konfigurovatelný, default 32)

### ScoringRulesService
- Načte `Sport.ScoringRulesJson`, přepíše z `League.ScoringRulesOverrideJson` pokud existuje
- Vrátí typovaný `ScoringRules` objekt

---

## Stránky

### Veřejné (po přihlášení)

| Route | Komponenta | Popis |
|---|---|---|
| `/` | `Home.razor` | Dashboard: sport dlaždice, aktivní sezóny, dnešní zápasy |
| `/leagues` | `LeaguesPage.razor` | Všechny ligy, filtr dle sportu (`?sport=football`) |
| `/leagues/{id}` | `LeagueDetail.razor` | Detail ligy, seznam sezón |
| `/seasons/{id}` | `SeasonDetail.razor` | Tabulka pořadí, ELO chart, účastníci |
| `/seasons/{id}/matches` | `SeasonMatches.razor` | Zápasy per kolo, rychlé zadání výsledku |
| `/matches/{id}` | `MatchDetail.razor` | Detail zápasu, zadávání MatchEvent (góly, karty…) |
| `/races/{id}` | `RaceDetail.razor` | Výsledky závodu, pořadí, časy |
| `/players` | `PlayersPage.razor` | Pool hráčů, filtr, search |
| `/players/{id}` | `PlayerDetail.razor` | Statistiky hráče across sezón |
| `/teams` | `TeamsPage.razor` | Všechny týmy, filtr dle sportu |
| `/teams/{id}` | `TeamDetail.razor` | Sestava + výsledky |

### Admin (role Admin)

| Route | Komponenta | Popis |
|---|---|---|
| `/admin` | `AdminDashboard.razor` | Přehled |
| `/admin/leagues/create` | `LeagueCreate.razor` | Nová liga (sport, název, scoring) |
| `/admin/seasons/create` | `SeasonCreate.razor` | Nová sezóna (liga, formát, ELO) |
| `/admin/seasons/{id}/participants` | `SeasonParticipants.razor` | Přidat/odebrat účastníky |
| `/admin/seasons/{id}/generate` | `SeasonGenerateMatches.razor` | Generovat zápasy |
| `/admin/players/create` | `PlayerCreate.razor` | Nový hráč |
| `/admin/teams/create` | `TeamCreate.razor` | Nový tým + sestava |

---

## Navigace (NavMenu)

```
🏠 Home
⚽ Leagues        (filtr per sport na stránce)
📅 Seasons        (poslední aktivní)
🎮 Matches        (dnešní / nadcházející)
🏃 Players
🛡️ Teams
─────────────────
⚙️ Admin          [Authorize(Roles="Admin")]
```

---

## Struktura projektu

```
ScorerApp/
├── src/
│   ├── ScorerApp.Web/
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   └── NavMenu.razor
│   │   │   ├── Pages/
│   │   │   │   ├── Admin/
│   │   │   │   └── [viz tabulka výše]
│   │   │   └── Account/          # Identity scaffolded pages
│   │   ├── Domain/
│   │   │   ├── Models/           # Všechny entity
│   │   │   └── Services/         # MatchGenerator, Standings, ScoringRules
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── SeedData.cs       # Football, Hockey, Basketball sporty
│   │   └── wwwroot/
│   │       └── app.css           # Glassmorphism UI (ui-panel, ui-page-header…)
│   ├── ScorerApp.Tests/
│   └── SharedServices/           # git submodule → github.com/olsanvit/SharedServices
├── docs/superpowers/specs/
└── ScorerApp.sln
```

---

## Seed data (sporty při startu)

| Sport | Type | MatchType | ParticipantKind | ScoringRules |
|---|---|---|---|---|
| Football | Football | HeadToHead | Team | win=3, draw=1, loss=0 |
| Ice Hockey | IceHockey | HeadToHead | Team | win=3, winOT=2, lossOT=1, loss=0 |
| Basketball | Basketball | HeadToHead | Team | win=2, loss=0 |
| Tennis | Tennis | HeadToHead | Individual | win=2, loss=0 |
| Darts | Darts | HeadToHead | Individual | win=2, loss=0 |
| Padel | Padel | HeadToHead | Team | win=2, loss=0 |
| Cards (Prší) | Cards | HeadToHead | Individual | win=2, loss=0 |
| Running | Running | MultiParticipant | Individual | position-based |

---

## CSS / UI konvence

Stejný glassmorphism styl jako ostatní projekty v mono-repo:
- `ui-page-header`, `ui-panel`, `ui-panel-header`, `ui-panel-title`
- `ui-anim-fade-up`, `text-accent`
- Bootstrap 5 tabulky: `table table-hover mb-0`
- Badges: `badge bg-secondary/success/danger`
