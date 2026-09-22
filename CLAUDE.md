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
        RacesPage, RaceDetail, TeamSeasonStats
        RankingsPage    # /rankings — žebříček per sport
        ProfilePage     # /profile — můj rating, moje zápasy a sezóny
      Account/          # Login, Register, Logout
      Layout/           # MainLayout, NavMenu
      Shared/           # SeasonFormatBuilder, PlayoffBracket, ClubPageBase
      Pages/ClubModule/ # Kluby: /clubs, /organizations, /chat, /circulars, /cars, /join, /accept-invite
    Domain/
      Models/           # Enums.cs + entity třídy
      Services/         # viz Domain Services níže
      Models/Clubs/     # klubové entity (namespace ScorerApp.Domain.Models.Clubs)
      Services/Clubs/   # klubové služby (namespace ScorerApp.Domain.Services.Clubs)
    Data/               # AppDbContext, AppUser, SeedData, AppDbContextFactory
    Resources/          # SharedResource.cs (namespace ScorerApp!) + .resx / .en.resx
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
| `PlayoffMatch` | Pozice v pavouku: kolo, dvojice, nasazení, vítěz, vazba na `Match` |
| `SportRating` | Trvalý rating hráče v jednom sportu (Games/Wins/Draws/Losses) |
| `SeasonFormatDefinition` | POCO pro `Season.FormatJson` — seznam modulů soutěže |

## Konvence

- `IDbContextFactory<AppDbContext>` — `await using var db = await DbFactory.CreateDbContextAsync()`
- Všechny stránky: `@rendermode InteractiveServer` + `@attribute [Authorize]`
- Admin stránky: `@attribute [Authorize(Roles = "Admin")]`
- Glassmorphism CSS: `ui-panel`, `ui-page-header`, `ui-panel-header`, `ui-panel-title`, `ui-anim-fade-up`, `text-accent`
- Name-as-link pattern: `<a href="/leagues/@l.Guid" class="text-decoration-none">@l.Name</a>`
- `SeasonStatus` má **explicitní číselné hodnoty** (Draft=0, InProgress=1, Completed=2, Registration=3) — sedí na starý Planning/Active/Finished, takže se hodnoty nesmí přečíslovat. Popisky přes extension metody `Label()` / `Badge()` / `AllowsParticipantChanges()` v `Enums.cs`.
- `dotnet ef migrations add` nikdy s `--no-build` — vygeneruje prázdnou migraci ze zastaralé sestavy
- Layout (`wwwroot/app.css`): okno se neposouvá, roluje jen `main` (menu a hlavička stojí). Proto: `main` má `min-width: 0` (široká tabulka jinak roztáhne stránku), tabulky vždy v `table-responsive`, záhlaví `.ui-page-header > .d-flex` se zalamuje. Po navigaci vrací `main` nahoru skript v `App.razor` (Blazor posouvá jen okno). Na mobilu (< 641 px) je menu sbalené pod tlačítkem.
- `ui-2026.css` ze SharedServices ScorerApp NEnačítá — pravidla `body[data-ui-lib=…]` tu neplatí.
- Data Protection klíče (přihlašovací a antiforgery cookie) jsou v DB — `AddDataProtection().SetApplicationName("ScorerApp").PersistKeysToDbContext<AppDbContext>()`, tabulka `DataProtectionKeys`. Neměnit ApplicationName, jinak se všichni odhlásí.
- `Program.cs` musí volat `AddRadzenComponents()` — `<UiProviders/>` ze SharedServices vykresluje `<RadzenComponents/>` a bez registrace padá každá stránka na 500.

## Domain Services

- **ScoringRulesService** — parsuje `ScoringRulesJson` z Sport nebo League override
- **StandingsService** — výpočet tabulky z odehraných zápasů
- **EloService** — ELO rating update po zápase (K=32)
- **MatchGeneratorService** — okružní (Berger) rozpis, skupiny se „hadím“ nasazením, švýcarský systém
- **SeasonFormatService** — čte/zapisuje `Season.FormatJson`; při chybějícím nebo rozbitém JSON spadne zpět na starý enum `SeasonFormat`
- **SeasonScheduleService** — generuje fáze sezóny podle formátu, po uložení výsledku posouvá pavouka a přepočítá rating
- **PlayoffService** — nasazení zrcadlením (1,8,4,5,2,7,3,6), volné losy, postup vítězů
- **SportRatingService** — trvalý rating per sport; počítá se **vždy přehráním celé historie**, ne inkrementálně

## Modulární formát sezóny

`Season.FormatJson` je pole modulů (`RoundRobin` / `GroupStage` / `Swiss` / `Playoff`), které se odehrají po sobě.
Sezóny bez JSON používají starý enum — nový kód proto nikdy nečte `Season.Format` přímo, ale přes `SeasonFormatService.Resolve()`.

`Match.Stage` + `ModuleIndex` + `GroupIndex` říkají, ke které fázi zápas patří:
- do tabulky se počítá jen `Stage != Playoff`
- seznam zápasů se seskupuje podle `(Stage, GroupIndex, Round)` — samotné `Round` nestačí, protože playoff začíná znovu od kola 1

## Lokalizace

`@inject IStringLocalizer<SharedResource> S` + `@S["klic"]`, klíče v `Resources/SharedResource.resx` (cs) a `.en.resx` (en).

Nenalezený resx nehází chybu — lokalizátor vrátí klíč a stránka ukáže např. `Clubs_Clubs`. Aby se našel, musí platit všechno najednou:
- `SharedResource.cs` v namespace **`ScorerApp`** a v něm `[assembly: RootNamespace("ScorerApp")]` — bez atributu bere lokalizátor jako kořen název sestavení `ScorerApp.Web`.
- csproj `<EmbeddedResourceUseDependentUponConvention>false`, jinak se resx vedle `SharedResource.cs` zabalí jako `ScorerApp.SharedResource`, ale `AddSimpleLocalization` (SharedServices) nastavuje `ResourcesPath = "Resources"` a hledá `ScorerApp.Resources.SharedResource`.
- Hlídá to test `Pages_ShowTranslatedTexts_NotResourceKeys` (do 2026-09-21 se texty z resx nezobrazovaly vůbec).

Všechno UI je lokalizované (2026-09-22) — nový text nikdy natvrdo, klíč s předponou stránky (`Match_`, `Season_`, `Clubs_`…). Enumy přes `S[$"NázevEnumu_{hodnota}"]` (klíč pro KAŽDOU hodnotu). Služby, jejichž texty vidí uživatel (`PlayoffService`, `SeasonFormatService`, `SeasonScheduleService`, klubové služby — hlášky výjimek `ClubErr_*`), berou `IStringLocalizer<SharedResource>` v konstruktoru. E-maily se skládají v jazyce PŘÍJEMCE: `using (CultureScope.For(user.PreferredCulture))` kolem sestavení textu (`ClubMails`), odeslání až mimo scope. `PreferredCulture` plní middleware `PreferredCultureSync` z culture cookie (přepínač jazyka ukládá jen cookie); pozvánka bez účtu jde v jazyce zvoucího. Logy zůstávají česky.

## Modul Kluby

Převzato z projektu ClubManager (2026-09-14), který jako samostatná appka zaniká. Repo ClubManager je jen archiv.

**Model:** `Organization` → `Club` → `ClubMember` (soupiska = `Player`, účet nemusí mít). Oprávnění nese `OrganizationMember` (účet + `OrgRole` Member < ClubManager < OrgAdmin — porovnává se přes `>=`, pořadí hodnot nesmí změnit). `Team.ClubId` a `SeasonParticipant.ClubId` jsou volitelné; účastník sezóny zůstává hráč XOR tým, takže tabulky/ELO/playoff kluby neřeší.

Dál: `Invitation` + `Club.JoinCode`, chat (`ClubThread`, `ChatMessage`, `ChatMessageRead`), oběžníky (`Circular`, `CircularRecipient` — typ Debt = nedoplatky), `NotificationPreference`, `Car` + `CarReservation`, `FamilyLink`.

**Pravidla:**
- Oprávnění ověřují SLUŽBY přes `ClubAccessService`, ne jen stránky. Stránka předává `UserId` a `IsSiteAdmin` (z `ClubPageBase`).
- Chat, oběžníky a auta fungují jen pro hráče spárované s účtem (`Player.UserId`).
- `FamilyLink` = účet rodiče ↔ HRÁČ soupisky (`ChildPlayerId`, ne účet dítěte — děti účet většinou nemají), omezený na organizaci. Rodič dostává oddílové oběžníky (`ClubAccessService.ClubParentIds`, přidané jen v `CircularService`), do chatu ani `ClubAccountIds` NEPATŘÍ. Propojení z rodiče udělá člena organizace (Member).
- Kluby, organizace, auta ani členy soupisky NEMAZAT — `IsActive = false`. `AuditInterceptor` převádí Remove na soft delete a DB kaskáda se pak nespustí.
- Chat real-time přes singleton `ClubChatBroadcaster` (in-process), NE SignalR hub — server-side HubConnection nemá auth cookie. Funguje pro jednu instanci aplikace.
- `Program.cs` registruje `AddDbContextFactory` i `AddDbContext` — `AddDbContext` MUSÍ mít `optionsLifetime: ServiceLifetime.Singleton`, jinak singleton `IDbContextFactory` sahá na scoped konfiguraci z root provideru a v Development (validace scope) nejde získat vůbec. Hosted service si scoped služby bere z `IServiceScopeFactory`.
- Rezervace aut: `DateOnly`, oba krajní dny jsou obsazené, kontrola + zápis v Serializable transakci.
- Uživatelský text do e-mailu vždy `WebUtility.HtmlEncode`.
- Přijetí pozvánky vyžaduje přihlášení — `SignInManager` uvnitř InteractiveServer circuitu cookie nezapíše.
- Opětovné odeslání pozvánky vydá NOVÝ token (starý odkaz přestane platit); zrušení = soft delete. `GetPendingForClubAsync` vrací i prošlé, aby šly poslat znovu.
- `NotificationPreference` bez uloženého záznamu = výchozí hodnoty třídy, které MUSÍ odpovídat `ChatNotificationDispatcher.Channels(type, null)` (hlídá test).

**Konfigurace** (hodnoty jen v `appsettings.Production.json` / env, nikdy v gitu): `App:BaseUrl` (vč. PathBase, pro odkazy v e-mailech), `Email:Smtp:*` (Host, Port, Username, Password, From, FromName — společná služba `IEmailService` ze SharedServices; klubové e-maily přes `ClubNotificationService.SendEmailAsync` → `IEmailService.SendAsync`, testy mají `FakeEmailService` a čtou `factory.Emails.Sent`), `Ntfy:BaseUrl` (prázdné = vypnuto), `Ntfy:Auth`, `Seed:AdminPassword`.

## Testy

- `dotnet test src/ScorerApp.Tests/` — unit testy + integrační smoke testy (`/`, `/health`) běží bez databáze.
- **Kategorie `Database`** (`src/ScorerApp.Tests/Database/`) potřebuje lokální Postgres na `localhost:5432`
  (spuštění: `~/scorerapp-deploy-prep/10-dev-db-start.sh`). Bez něj: `dotnet test --filter Category!=Database`.
- `DatabaseTestFactory` před startem **smaže a znovu vytvoří** DB `ScorerApp_Tests` (nikdy vývojovou `ScorerApp`)
  — testy tím ověřují i migrace na čisté databázi. Heslo bere z `appsettings.Development.json`, jinou DB lze
  nastavit proměnnou `SCORERAPP_TEST_DB`.
- Přihlášení v testech: `TestAuthHandler` podle hlaviček `X-Test-UserId` a `X-Test-Roles` — žádná hesla ani cookie.
  Účty zakládá `factory.CreateUserAsync()` bez hesla.
- Scoped služby (`UserManager`, doménové služby) v testech ze scope (`factory.Services.CreateScope()`).
  Testy běží v Development s validací scope — chybná životnost služby v `Program.cs` se tak projeví hned při startu.
- V HTML ze stránek hledat ASCII data, ne přeložené texty: prerender kóduje diakritiku (`í` → `&#xED;`).
- Na vývojovém Macu smí běžet jen jeden `dotnet build/publish/test` najednou (8 GB RAM) — před spuštěním ověřit
  `pgrep -x dotnet` + sloveso v argumentech.

## Workflow

1. Admin → Nová liga → vybere sport
2. Admin → Nová sezóna → vybere ligu, formát, ELO ano/ne
3. Admin → Přidat účastníky (hledání, nový hráč jménem, hromadně) — sezóna přejde z Draft do Registration
4. Admin → Uzavřít registraci a generovat zápasy (sezóna přejde do InProgress)
5. Uživatel → SeasonMatches → rychlé zadání výsledků
6. Uživatel → MatchDetail → detailní statistiky (góly, karty, …)
7. Tabulka pořadí se zobrazuje na SeasonDetail

## Seed data

Při startu se automaticky vytvoří 8 sportů (Football, Ice Hockey, Basketball, Tennis, Darts, Padel, Cards, Running).
Admin účet: `admin@local` / `Admin123.`
