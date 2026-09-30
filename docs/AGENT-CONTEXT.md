# ScorerApp — Agent Context

> Vstupní bod pro každou agent session. Přečti jako první, pak CLAUDE.md v kořeni projektu.
> CLAUDE.md je výborně napsaný a aktuální — je primárním zdrojem pravdy.

---

## Co je projekt

Blazor Server sportovní aplikace pro správu lig, sezón, zápasů a hráčů. Modulární formát sezóny (RoundRobin / GroupStage / Swiss / Playoff). Modul Kluby — organizace, oddíly, chat, oběžníky, auta.

**Repo:** `github.com/olsanvit/ScorerApp`  
**Větev:** `main`  
**Deploy:** QNAP NAS, `~/deploy-to-qnap.sh scorer [prod]`

---

## Stav projektu (2026-09-23)

### Co funguje
- Modulární formát sezóny, playoff pavouk s vizualizací
- ELO rating + trvalý SportRating per sport  
- Generátor zápasů (Berger, skupiny, Swiss)
- Modul Kluby: organizace, oddíly, chat real-time, oběžníky, auta, pozvánky, FamilyLink
- E-maily v jazyce příjemce (`CultureScope`)
- Data Protection klíče v DB (přihlášení přežije restart kontejneru)
- MAUI mobile app (WebView + OAuth handoff přes `scorerapp://auth?token=...`)
- CI, lokalizace cs/en, 48 dokumentovaných stránek

### Known issues / TODO
- Zápas o 3. místo v pavouku chybí
- Forma (posledních 5 zápasů) v tabulce
- Ukončení sezóny nepřepočítá/nevyhlásí vítěze
- MatchDetail.razor má 717 řádků — kandidát na rozdělení
- RecomputeSeasonEloAsync duplikovaný — přesunout do service

---

## Klíčová architektura

```
src/
  ScorerApp.Web/
    Components/Pages/         Sport stránky + Admin + ClubModule
    Data/                     AppDbContext, SeedData, AppDbContextFactory
    Domain/Models/            Entity třídy + Enums.cs
    Domain/Services/          Sport services (Scoring, Standings, ELO, MatchGenerator, ...)
    Domain/Services/Clubs/    Klub services (ClubAccessService, ChatService, ...)
    Migrations/               7 EF migrací
  ScorerApp.Tests/            Unit + integration + DB testy
  ScorerApp.Mobile/           MAUI WebView wrapper
  SharedServices/             git submodule
```

---

## Klíčové pravidlo: Lifecycle sezóny

`Draft(0) → Registration(3) → InProgress(1) → Completed(2)` — hodnoty pevné, neposouvat!

---

## Klíčové services

| Service | Popis |
|---------|-------|
| `SeasonFormatService` | Čte/zapisuje `Season.FormatJson`; VŽDY přistupovat přes `.Resolve()` |
| `StandingsService` | Výpočet tabulky pořadí |
| `EloService` | ELO update (K=32) |
| `MatchGeneratorService` | Berger, skupiny, Swiss generátor |
| `PlayoffService` | Nasazení, volné losy, postup vítězů |
| `SportRatingService` | Trvalý rating per sport (přehrává celou historii) |
| `ClubAccessService` | Oprávnění — kontrolovat v services, ne na stránkách |
| `ClubChatBroadcaster` | Singleton — in-process real-time push |

---

## Kritické detaily pro vývoj

- **`AppDbContextFactory`** — povinný pattern (IDbContextFactory), ne přímá injekce
- **Soft delete** — global query filter `IsDeleted = false`; AuditInterceptor převádí Remove na soft delete
- **DataProtectionKeys** — tabulka v DB, `ApplicationName = "ScorerApp"` nesmí se měnit
- **`Stage + ModuleIndex + GroupIndex + Round`** — playoff začíná od kola 1; nestačí jen Round
- Do tabulky se počítají jen zápasy kde `Stage != Playoff`
- Singleton factory: `optionsLifetime: ServiceLifetime.Singleton` v DbContextFactory registraci

---

## Co číst na začátku session

1. **CLAUDE.md** — primární zdroj pravdy, aktuální k 2026-09-22
2. **`docs/pages/<Stránka>.md`** — stav konkrétní stránky (48 souborů)
3. Pokud měníš Playoff: PlayoffService + PlayoffBracket.razor
4. Pokud měníš Kluby: ClubAccessService jako první — kontroluje všechna práva
