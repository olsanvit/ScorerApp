# ScorerApp — CLAUDE.md

## Co projekt dělá

Multi-sport liga a turnajový manažer. Uživatel vytváří ligy, sezóny, přidává hráče/týmy, generuje zápasy a zadává výsledky. Výstupem jsou tabulky pořadí, výsledky a ELO rating.

## Stack

- **.NET 10 Blazor Server** — `@rendermode InteractiveServer`
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1`
- **ASP.NET Core Identity** — login/registrace, role `Admin`
- **SharedServices** git submodule (`src/SharedServices`) — `BaseGuid`, `UiSearchBar`, `Paginator`, `ToastService`, `ThemePicker`, `UiLibraryService`
- **Bootstrap 5 + Bootstrap Icons** — glassmorphism CSS ze SharedServices
- **Blazor-ApexCharts** — grafy
- **Serilog** → Console + File

## Struktura

```
src/
  ScorerApp.Web/
    Components/
      Pages/
        Admin/          # Admin CRUD stránky (role Admin)
        Home.razor, LeaguesPage, LeagueDetail, SeasonsPage
        SeasonDetail, SeasonMatches, MatchDetail, MatchesPage
        PlayersPage, PlayerDetail, TeamsPage, TeamDetail
      Account/          # Login, Register, Logout
      Layout/           # MainLayout, NavMenu
    Domain/
      Models/           # Enums.cs + entity třídy
      Services/         # ScoringRulesService, StandingsService, EloService, MatchGeneratorService
    Data/               # AppDbContext, AppUser, SeedData, AppDbContextFactory
    Migrations/
    wwwroot/app.css
  ScorerApp.Tests/
  SharedServices/       # git submodule
```

## Klíčové modely (ScorerApp.Domain.Models)

| Model | Popis |
|---|---|
| `Sport` | Sport s ScoringRulesJson, SportMatchType (HeadToHead / MultiParticipant) |
| `League` | Liga → Sport, volitelné ScoringRulesOverrideJson |
| `Season` | Sezóna → Liga, Format, Status, UseElo |
| `Player` | Hráč (individuální sport nebo člen týmu) |
| `Team` | Tým → TeamPlayers |
| `SeasonParticipant` | Účastník sezóny (PlayerId XOR TeamId), EloRating |
| `Match` | Zápas mezi dvěma SeasonParticipanty, HomeScore/AwayScore |
| `MatchEvent` | Událost zápasu (Goal, Assist, YellowCard, …) |
| `MatchSet` | Set/hra (tenis, darts) |
| `Race` | Závod s více účastníky (běh, cyklistika) |
| `RaceResult` | Výsledek závodu: pozice, čas |

## Konvence

- `IDbContextFactory<AppDbContext>` — `await using var db = await DbFactory.CreateDbContextAsync()`
- Všechny stránky: `@rendermode InteractiveServer` + `@attribute [Authorize]`
- Admin stránky: `@attribute [Authorize(Roles = "Admin")]`
- Glassmorphism CSS: `ui-panel`, `ui-page-header`, `ui-panel-header`, `ui-panel-title`, `ui-anim-fade-up`, `text-accent`
- Name-as-link pattern: `<a href="/leagues/@l.Guid" class="text-decoration-none">@l.Name</a>`

## Domain Services

- **ScoringRulesService** — parsuje `ScoringRulesJson` z Sport nebo League override
- **StandingsService** — výpočet tabulky z odehraných zápasů
- **EloService** — ELO rating update po zápase (K=32)
- **MatchGeneratorService** — Round Robin / Double Round Robin generátor

## Workflow

1. Admin → Nová liga → vybere sport
2. Admin → Nová sezóna → vybere ligu, formát, ELO ano/ne
3. Admin → Přidat účastníky (hráče nebo týmy)
4. Admin → Generovat zápasy (automaticky aktivuje sezónu)
5. Uživatel → SeasonMatches → rychlé zadání výsledků
6. Uživatel → MatchDetail → detailní statistiky (góly, karty, …)
7. Tabulka pořadí se zobrazuje na SeasonDetail

## Seed data

Při startu se automaticky vytvoří 8 sportů (Football, Ice Hockey, Basketball, Tennis, Darts, Padel, Cards, Running).
Admin účet: `admin@local` / `Admin123.`
