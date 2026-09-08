# ScorerApp — Game Design Spec

> Zadáno uživatelem 2026-09-08. Tento dokument popisuje účel, sporty, formáty turnajů a ranking systém.

---

## Přehled

Evidence domácích i oficiálních lig a turnajů v čemkoliv. Flexibilní správa — od roční ligy s opakujícími se sezónami až po jednodenní turnaj pěti kamarádů.

---

## Podporované sporty

| Sport | Typ | Poznámka |
|---|---|---|
| Tenis | Individuální | Složitější generování (sets/gamy), jednodušší zápis |
| Šipky (Darts) | Individuální | |
| Pétanque | Individuální / týmový | |
| Prší | Desková hra | Jednodušší generování, složitější zápis |
| Ticket to Ride | Desková hra | Jednodušší generování, složitější zápis |

---

## Typy lig / turnajů

| Typ | Popis |
|---|---|
| **Roční liga** | Opakující se sezóny, trvalé pořadí |
| **Jednodenní turnaj** | Vytvoří se, odehraje, hotovo |
| **Vícedenní turnaj** | Přes víkend nebo více dní |

---

## Generování zápasů

- Různé formáty: tabulka (round robin), tabulka + play off, turnajový pavouk
- **Flexibilní přidávání hráčů:** pokud nevíme kolik přijde (např. tenis na kurtu), hráče lze **zakliknout přímo na místě** a teprve pak se vygeneruje pavouk/tabulka
- Generování matchupů probíhá **podle rankingu** — silnější hráči se nepotkají hned v 1. kole

---

## Ranking systém

- Každý sport + každá desková hra má **samostatný ranking**
- Ranking se aktualizuje po každém odehraném zápasu/turnaji
- Na **profilu hráče** se zobrazuje i **celkový průměrný rating** přes všechny sporty/hry
- Ranking slouží jako základ pro generování matchupů (seed)

### Celkový rating — vážený průměr (doporučeno pro dlouhodobé použití)

Výpočet: `Σ (rating_sportu × počet_her_sportu) / Σ počet_her_všech_sportů`

**Proč vážený průměr a ne prostý průměr:**
- Prostý průměr lze snadno nafouknout — stačí odehrát 1 turnaj v novém sportu a vyhrát
- Vážený průměr odráží skutečnou hloubku hráče — čím více her v daném sportu, tím větší vliv jeho ratingu
- Hráč, který hraje jen pétanque, bude mít celkový rating blízký pétanque ratingu (nezfalšují ho 2 výhry v Prší)
- Přirozená motivace hrát více sportů — každý nový sport přidá váhu i do celkového čísla

---

## Zápis výsledků

- Různý dle sportu:
  - Tenis: sets, gamy
  - Darts, Pétanque: body / kola
  - Deskové hry: pořadí, skóre, body
- Zápis průběžný — nemusí se čekat na konec turnaje

---

## Admin funkce

Admin má přístup k plné správě obsahu přes `/admin/*` stránky.

### Ligy a turnaje

- Přehled všech lig a turnajů (filtr: sport, typ, stav)
- Vytvořit ligu: název, sport, formát (round robin / pavouk / tabulka+playoff), opakování sezón
- Vytvořit turnaj: jednodenní nebo vícedenní, datum, sport, formát
- Editovat nebo smazat ligu/turnaj
- Archivovat ukončenou ligu/turnaj (přesunout do historie)

### Sezóny

- Přehled sezón dané ligy
- Vytvořit novou sezónu (ručně nebo automaticky po ukončení předchozí)
- Uzavřít sezónu + přepočítat finální pořadí a ranking

### Zápasy

- Přehled všech zápasů (filtr: liga, sezóna, hráč, stav)
- Editovat / opravit výsledek zápasu po odehrání
- Smazat zápas (a přepočítat ranking)
- Ručně přegenerovat matchupy pro sezónu nebo kolo

### Hráči

- Přehled všech hráčů (filtr: sport, ranking)
- Vytvořit / editovat hráče (jméno, foto, kontakt)
- Slučovat duplicitní hráče (merge: zachovat historii výsledků)
- Deaktivovat hráče

### Ranking

- Přehled rankingu per sport + celkový vážený průměr
- Ruční přepočítání rankingu (po opravě výsledků)
- Reset rankingu pro daný sport nebo celkový

### Sporty

- Přehled podporovaných sportů
- Přidat nový sport: název, typ (individuální/týmový/desková hra), formát zápisu výsledku
- Editovat nebo deaktivovat sport

---

## Page Tracker

| Stránka | Route | Status |
|---|---|---|
| Home | `/` | ✅ existuje |
| Leagues | `/leagues` | ✅ existuje |
| League Detail | `/leagues/{id}` | ✅ existuje |
| Seasons | `/seasons` | ✅ existuje |
| Season Detail | `/season/{id}` | ✅ existuje |
| Season Matches | `/season/{id}/matches` | ✅ existuje |
| Match Detail | `/match/{id}` | ✅ existuje |
| Matches | `/matches` | ✅ existuje |
| Players | `/players` | ✅ existuje |
| Player Detail | `/player/{id}` | ✅ existuje |
| Teams | `/teams` | ✅ existuje |
| Team Detail | `/team/{id}` | ✅ existuje |
| Player Profile (ranking přehled) | `/profile` | ❌ chybí — celkový avg rating |
| On-site signup (zakliknutí hráčů na místě) | `/tournament/{id}/checkin` | ❌ chybí |
| Admin CRUD | `/admin/*` | ✅ existuje |
| Login / Register | `/account/*` | ✅ existuje |

---

## Rozhodnutí

1. **Ranking systém** — **ELO** (jako nyní)
2. **Deskové hry s více hráči** — párování **podle konkrétní deskovky** (každá hra má vlastní logiku matchupu)
3. **Turnaj bez registrace** — **NE**: registrace povinná
