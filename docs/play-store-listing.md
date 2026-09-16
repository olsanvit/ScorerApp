# ScorerApp — podklady pro Google Play Console

## Základní údaje

| Pole | Hodnota |
|---|---|
| Název appky | ScorerApp |
| Package name | `cz.olsansky.scorerapp` |
| Kategorie | Sport |
| Privacy Policy URL | https://scorerapp.vo2info.cz/privacy |
| Kontaktní e-mail | olsanskyvitek@gmail.com |
| Reklamy | Ne |
| Nákupy v appce | Ne |

## Krátký popis (max 80 znaků)

```
Multi-sport liga a turnajový manažer — ligy, zápasy, ELO a tabulky.
```

## Plný popis (max 4000 znaků)

```
ScorerApp je jednoduchý správce sportovních lig a turnajů. Založte ligu,
přidejte sezónu, hráče nebo týmy, vygenerujte rozpis zápasů a zadávejte
výsledky — appka za vás spočítá tabulku pořadí i ELO rating.

Podporované sporty: fotbal, hokej, basketbal, tenis, šipky, padel, karty,
běh a další.

Hlavní funkce:
• Správa lig a sezón (round robin i další formáty)
• Přidávání hráčů a týmů
• Automatický generátor rozpisu zápasů
• Rychlé zadávání výsledků
• Tabulky pořadí a ELO rating
• Statistiky zápasů (góly, karty, sety podle sportu)
• Přihlášení e-mailem nebo Google účtem

ScorerApp je určen pro amatérské ligy, turnaje s přáteli, firemní soutěže
i kluby, které chtějí mít přehled o výsledcích na jednom místě.
```

## Content rating dotazník (odpovědi)

Appka neobsahuje násilí, sexuální obsah, vulgarismy, hazard ani odkazy na
alkohol/drogy. Uživatelský obsah je omezen na sportovní data (názvy týmů,
hráčů, výsledky) — bez chatu, komentářů nebo veřejného sdílení mezi
uživateli navzájem.

Doporučená klasifikace: **Everyone / PEGI 3**.

## Data safety formulář

**Sbíraná data:**
- E-mailová adresa — účel: vytvoření a správa účtu, přihlášení
- Uživatelské jméno — účel: vytvoření a správa účtu

**Nesbíráme:** polohu, kontakty, fotky, finanční údaje, zdravotní data,
historii prohlížení.

**Sdílení s třetími stranami:** žádné. Data se nikam neprodávají ani
nepředávají.

**Zabezpečení přenosu:** veškerá komunikace appky probíhá přes HTTPS.

**Možnost smazání dat:** ano — uživatel může požádat o smazání účtu a všech
dat e-mailem (viz privacy policy).

**Google Sign-In:** appka při přihlášení přes Google získává pouze e-mail
a jméno účtu, nic dalšího.

## Cílová skupina

Obecná (appka není zaměřená na děti, nevyžaduje deklaraci "Designed for
Families").

## Co ještě zbývá udělat v Play Console (vyžaduje váš Google login)

1. Založit novou appku v [Play Console](https://play.google.com/console)
2. Vyplnit store listing výše uvedenými texty
3. Nahrát ikonu (512×512 PNG) a feature graphic (1024×500) — zatím nejsou
   hotové, appka má jen jednoduchou placeholder ikonu s písmenem "S"
4. Nahrát min. 2 screenshoty z telefonu (mohu vygenerovat z emulátoru)
5. Vyplnit content rating dotazník (odpovědi výše)
6. Vyplnit data safety formulář (odpovědi výše)
7. Nahrát podepsaný release balíček:
   `src/ScorerApp.Mobile/bin/Release/net10.0-android/publish/cz.olsansky.scorerapp-Signed.aab`
8. Odeslat k internímu testování nebo rovnou k produkční revizi
