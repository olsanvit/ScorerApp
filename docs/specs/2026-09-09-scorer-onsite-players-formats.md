# ScorerApp — On-site hráči a formáty per sport

> Zadáno 2026-09-09. Dvě fáze sezóny (Registrace → Hra), on-site přidávání hráčů, sport-specifické formuláře výsledků (Tenis/Šipky/Pétanque/Prší/Ticket to Ride).

---

## Cíl

Admin může přidávat hráče těsně před turnajem (i nové jménem), pak uzavře registraci a teprve generuje zápasy. Každý sport má vlastní formulář pro zápis výsledku.

---

## On-site flow — dvě fáze

### `Season.Status` enum

`Draft` → `Registration` → `InProgress` → `Completed`

| Stav | Popis |
|---|---|
| `Draft` | Sezóna vytvořena, ještě nespuštěna |
| `Registration` | Přidávání hráčů, zápasy ještě nevygenerovány |
| `InProgress` | Zápasy vygenerovány, bracket fixní |
| `Completed` | Vše odehráno |

### Fáze 1: Registrace

- Admin otevře `/admin/season/{id}/participants`
- Přidává hráče: výběr ze stávajících v DB nebo jménem nového
- Nový hráč zadaný jménem → okamžitě se vytvoří `Player` + `SeasonParticipant`
- Hráče lze odebrat ze seznamu (pokud ještě nebylo generování)
- Tlačítko **Uzavřít registraci a generovat zápasy** → přechod do `InProgress`

### Fáze 2: Hra

- Stav `InProgress` — bracket/tabulka vygenerována
- Hráče nelze přidávat ani odebírat
- Zapisují se výsledky, tabulka se aktualizuje

---

## UI — `SeasonParticipants.razor` (existující, extend)

- **Vyhledávací pole** (Blazored.Typeahead) pro stávající hráče z DB → tlačítko Přidat
- **Pole "Nový hráč"** — textové pole pro jméno → tlačítko Přidat nového (vytvoří Player + zařadí)
- **Seznam přidaných hráčů** s možností Odebrat (X tlačítko)
- **Počítadlo:** "12 hráčů přidáno"
- **Tlačítko Uzavřít registraci** — disabled pokud < 2 hráči, po kliknutí modal potvrzení → generování zápasů

---

## Formáty záznamu výsledků per sport

### Tenis

```
Set 1: [ gamy A ] – [ gamy B ]
Set 2: [ gamy A ] – [ gamy B ]
[ + Přidat set ]
Tie-break (pokud 6-6): [ body A ] – [ body B ]
```

- Výsledek = počet vyhraných setů (kdo má víc, vyhrál zápas)
- Validace: gamy 0–7, tie-break jen při 6–6
- Data: `MatchSet` záznamy (existuje)

### Šipky (Darts)

```
Best of: [ 3 / 5 / 7 ] sety   (konfigurovatelné per sezóna)
Set 1: [ legs A ] – [ legs B ]
Set 2: [ legs A ] – [ legs B ]
...
```

- Výsledek = počet vyhraných setů
- Data: `MatchSet` záznamy

### Pétanque

```
Body: [ skóre A ] – [ skóre B ]
```

- Validace: max 13 bodů, vítěz musí mít přesně 13
- Výsledek = kdo dosáhl 13 bodů
- Data: `Match.ScoreA`, `Match.ScoreB`

### Prší

```
         Hráč 1   Hráč 2   Hráč 3   ...
Kolo 1:  [    ]   [    ]   [    ]
Kolo 2:  [    ]   [    ]   [    ]
[ + Přidat kolo ]
Celkem:  [auto]   [auto]   [auto]
```

- Multi-player (2–6 hráčů), záporné body za karty
- Výsledek = nejnižší celkové skóre vyhrává
- Data: `Match.ResultJson` (jsonb tabulka kol × hráčů)

### Ticket to Ride

```
           Hráč 1   Hráč 2   Hráč 3   ...
Trasy:     [    ]   [    ]   [    ]
Destinace: [    ]   [    ]   [    ]
Nejdelší:  [    ]   [    ]   [    ]   (checkbox kdo má)
Celkem:    [auto]   [auto]   [auto]
```

- Multi-player (2–5 hráčů)
- Celkem = Trasy + Destinace + Nejdelší (15 bodů bonus)
- Výsledek = nejvyšší celkové skóre vyhrává
- Data: `Match.ResultJson` (jsonb tabulka kategorií × hráčů)

---

## `ISportResultFormatter`

```csharp
public interface ISportResultFormatter
{
    string SportKey { get; }
    MatchResultView GetResultView(Match match);
    bool ValidateResult(MatchResultInput input, out string error);
    (Guid? winnerId, int scoreA, int scoreB) DetermineWinner(MatchResultInput input);
}
```

**Implementace:**
- `TennisResultFormatter`
- `DartsResultFormatter`
- `PetanqueResultFormatter`
- `PrsiResultFormatter`
- `TicketToRideResultFormatter`

Registrace jako `IEnumerable<ISportResultFormatter>` v DI kontejneru. `MatchDetail.razor` načte správnou implementaci dle `Match.Sport`.

---

## Datový model — změny

### `Season`

Přidat:
- `Status` (enum: Draft / Registration / InProgress / Completed, default Draft)

### `Match`

Přidat:
- `ResultJson` (string?) — sport-specifická data pro Prší a Ticket to Ride (jsonb)

---

## UI — `MatchDetail.razor` (existující, extend)

Formulář zápisu výsledku se mění dle sportu:

1. Načíst `ISportResultFormatter` pro `Match.League.Sport`
2. Zobrazit sport-specifický formulář (Razor komponenta per sport nebo dynamický render)
3. Validace při submitu (`ValidateResult`)
4. Uložení výsledku + určení vítěze (`DetermineWinner`) + update ELO

---

## Architektura souborů

| Soubor | Akce |
|---|---|
| `Domain/Models/Season.cs` | Modify — přidat `Status` enum |
| `Domain/Models/Match.cs` | Modify — přidat `ResultJson` |
| `Domain/Models/SeasonStatus.cs` | Nový — enum |
| `Domain/Services/SportResultFormatters/ISportResultFormatter.cs` | Nový — interface |
| `Domain/Services/SportResultFormatters/TennisResultFormatter.cs` | Nový |
| `Domain/Services/SportResultFormatters/DartsResultFormatter.cs` | Nový |
| `Domain/Services/SportResultFormatters/PetanqueResultFormatter.cs` | Nový |
| `Domain/Services/SportResultFormatters/PrsiResultFormatter.cs` | Nový |
| `Domain/Services/SportResultFormatters/TicketToRideResultFormatter.cs` | Nový |
| `Components/Pages/Admin/SeasonParticipants.razor` | Modify — on-site přidávání, fáze Registrace |
| `Components/Pages/MatchDetail.razor` | Modify — sport-specifický formulář |

---

## Co se nemění

- `EloService` — počítá ELO z výsledku, dostane jen `winnerId` a `loserId`
- `StandingsService` — tabulky beze změn
- `Player` entita — nový hráč přidaný on-site je standardní `Player` záznam
