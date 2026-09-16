# ScorerApp — Playoff bracket a modulární formáty

> Zadáno 2026-09-09. Plně flexibilní formát soutěže z modulů (RoundRobin, GroupStage, Swiss, Playoff), playoff bracket sestavený seed dle tabulky, vizuální pavouk.

---

## Cíl

Admin si při vytváření sezóny sestaví formát z modulů — kombinace skupinové fáze, round-robinu, Swiss systému a playoff bracketu. Playoff se generuje po dokončení předchozích fází, seed dle tabulkového pořadí.

---

## Modulární formát soutěže

### `SeasonFormat` — uložen jako jsonb na `Season`

```json
{
  "Modules": [
    { "Type": "GroupStage", "GroupCount": 4, "TeamsPerGroup": 4, "AdvanceCount": 2 },
    { "Type": "RoundRobin", "MatchesPerPair": 1 },
    { "Type": "Playoff", "BracketSize": 8, "Seeding": "TableRank" }
  ]
}
```

### Dostupné moduly

| Modul | Parametry | Popis |
|---|---|---|
| `RoundRobin` | `MatchesPerPair` (1 nebo 2) | Každý s každým, výsledkem je tabulka |
| `GroupStage` | `GroupCount`, `TeamsPerGroup`, `AdvanceCount` | N skupin, round-robin uvnitř, top X postupuje do další fáze |
| `Swiss` | `Rounds` | Swiss párování, výsledkem je tabulka |
| `Playoff` | `BracketSize` (4/8/16/32), `Seeding` ("TableRank") | Knockout bracket |

### Předdefinované šablony (rychlý výběr v UI)

| Šablona | Moduly |
|---|---|
| Jen tabulka | `[RoundRobin]` |
| Jen playoff | `[Playoff]` |
| Tabulka + playoff | `[RoundRobin, Playoff]` |
| Skupiny + playoff | `[GroupStage, Playoff]` |
| Swiss + playoff | `[Swiss, Playoff]` |

---

## Playoff bracket

### Seed dle tabulky

Po dokončení všech non-playoff modulů:
1. Načíst výslednou tabulku (nebo skupinové tabulky)
2. Seřadit hráče/týmy dle pořadí
3. Vzít top N (N = `BracketSize`)
4. Klasické nasazení: seed 1 vs seed N, seed 2 vs seed N-1, …

### Generování kol

1. Sestavit 1. kolo → uložit jako `PlayoffMatch` záznamy
2. Po každém výsledku → zkontrolovat zda jsou všechny zápasy kola hotové
3. Pokud ano → sestavit páry pro další kolo (vítězové v pořadí BracketPosition)
4. Opakovat až do finále

### Datový model — `PlayoffMatch`

| Sloupec | Typ | Popis |
|---|---|---|
| `Id` | UUID PK | |
| `SeasonId` | UUID | |
| `Round` | int | 1 = první kolo, 2 = čtvrtfinále, atd. |
| `BracketPosition` | int | Pozice v daném kole (1, 2, 3…) |
| `ParticipantAId` | UUID? | null dokud není znám postoupivší |
| `ParticipantBId` | UUID? | null dokud není znám postoupivší |
| `SeedA` | int? | Původní seed pro referenci |
| `SeedB` | int? | |
| `WinnerId` | UUID? | null dokud neodehráno |
| `MatchId` | UUID? | Odkaz na `Match` záznam po zápisu výsledku |

---

## `PlayoffService` — rozhraní

```csharp
public interface IPlayoffService
{
    // Generuje 1. kolo bracket ze závěrečné tabulky
    Task<List<PlayoffMatch>> GenerateBracketAsync(Guid seasonId, int bracketSize);

    // Po zapsání výsledku playoff zápasu — postup vítěze do dalšího kola
    Task ProcessPlayoffResultAsync(Guid playoffMatchId, Guid winnerId);

    Task<PlayoffBracketView> GetBracketViewAsync(Guid seasonId);
}
```

---

## UI

### Admin — `SeasonCreate.razor` / `SeasonEdit.razor`

Nová sekce **"Formát soutěže"**:
- Výběr šablony (tlačítka s ikonou + názvem pro každou šablonu)
- Nebo "Vlastní formát" → seznam modulů s tlačítkem +Přidat modul
- Každý přidaný modul má vlastní konfiguraci (počet skupin, BracketSize…)
- Preview: "Soutěž bude mít: round-robin fázi (každý s každým) → playoff top 8"

### `SeasonGenerateMatches.razor` (existující, extend)

- Nová sekce po dokončení skupinové/RR fáze: "Playoff bracket"
- Tlačítko **Generovat playoff bracket** (aktivní pokud jsou všechny skupinové zápasy hotové)
- Preview bracket (kdo vs kdo v 1. kole) před potvrzením
- Potvrzení → generování

### `SeasonDetail.razor` (existující, extend)

Nová záložka nebo sekce **"Playoff"**:
- Vizuální bracket: stromová struktura kol
- Každý zápas: [Hráč/Tým A] vs [Hráč/Tým B], skóre (pokud odehráno), vítěz tučně
- Budoucí zápasy: "?" vs "?" (čeká na výsledek předchozího kola)
- Responzivní: na mobilu lineární seznam kol

---

## Architektura souborů

| Soubor | Akce |
|---|---|
| `Domain/Models/Season.cs` | Modify — přidat `FormatJson` (string/jsonb) |
| `Domain/Models/PlayoffMatch.cs` | Nový — entita |
| `Domain/Models/SeasonFormat.cs` | Nový — POCO pro deserializaci FormatJson |
| `Domain/Services/PlayoffService.cs` | Nový — generování bracket, postup kol |
| `Domain/Services/MatchGeneratorService.cs` | Modify — číst `SeasonFormat` pro modulární generování |
| `Components/Pages/Admin/SeasonCreate.razor` | Modify — přidat formát builder sekci |
| `Components/Pages/Admin/SeasonGenerateMatches.razor` | Modify — playoff generování |
| `Components/Pages/SeasonDetail.razor` | Modify — playoff záložka / vizuální bracket |

---

## Co se nemění

- `Match` entita — playoff zápasy jsou standardní Match záznamy, jen navíc odkazované z `PlayoffMatch.MatchId`
- `StandingsService` — tabulky beze změn, PlayoffService je čte
- `EloService` — ELO se počítá i pro playoff zápasy
