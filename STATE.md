# ScorerApp — STATE

Aktualizováno: 2026-10-22 · session ScorerApp (AIDB `session_status` SourceName `scorerapp`)

## Kde běží

- Produkce: https://scorer.vo2info.cz (scorerapp.vo2info.cz se trvale přesměruje), kontejner `scorerapp` na QNAPu, port 5021.
- Deploy: `~/.claude/deploy-gate.sh scorer prod ScorerApp` (klíč aplikace `scorer`, malými písmeny), nasazuje artefakt z CI.
- **Produkční DB: `ScorerApp`** na pg16 (ověřeno 15. 10. čtecím dotazem). `MyAppDb` v `appsettings.json` je jen výchozí
  hodnota pro lokální vývoj, produkce bere connection string z `appsettings.Production.json` na QNAPu.

## Rozdělané

Nic. Repo čisté, `main` = `origin/main`.

- `c1e6c28` (bump SharedServices na 381a1a6) je pushnutý, CI zelené, **nenasazený** (dle pokynu APP 18. 10.).

## Další krok

Čeká se na pokyn Applications coordinatora. Ve frontě (karta https://trello.com/c/P5bJx782, dávka 2):

1. čeština názvů sportů na dashboardu a žebříčcích,
2. překlad `/admin/access`,
3. whitelist do spuštění (jako USM),
4. zrychlit `/matches` (3,8 s i bez dat).

Známá vada mimo zadání: stránky nefiltrují soft-delete (`IsDeleted`), smazané záznamy zůstávají vidět.

## Poslední rozhodnutí Vítka

- 18. 10.: `AddVo2DataProtection` nepřidávat. Klíče zůstávají v DB pod ApplicationName „ScorerApp“ (karta 5L9Qoxoc).
- 17. 10.: ukázková data `[Demo]` v plném rozsahu, spouštění tlačítkem v `/admin`. Odebrání maže fyzicky jen `[Demo]`,
  spolek, oddíl a auto jen deaktivuje. Vítek data 17. 10. vytvořil.
- 15. 10.: duplicitní účty vlastníka sloučit v kódu; vlastník dostává Admin při každém přihlášení přes Google.
- 14. 10.: hlavní doména scorer.vo2info.cz, scorerapp.vo2info.cz přesměrovat 301.

## Po restartu

Nic nespouštět. `git status` + `git log -5`, AIDB stav `idle`, čekat na úkol od Applications coordinatora.
