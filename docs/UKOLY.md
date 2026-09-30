# ScorerApp — otevřené úkoly

Vedeno od 2026-09-27. Soubor je zatím neverzovaný (není v gitu) — ke commitu jen na pokyn.

## Právě rozpracované

- [ ] **Port opravy ConfirmDialog do SharedServices** — hotové v pracovní kopii submodulu, testy 128/128.
  Zbývá: ověřit dialog v běžící aplikaci → commit + push v `src/SharedServices` → bump ukazatele
  submodulu ve ScorerAppu → dát vědět koordinátorovi (chce koordinovat aktualizaci u ostatních projektů).
- [ ] **Nasazení do produkce** (Vítek schválil). Produkce běží na verzi z 2026-09-24, takže tam pořád
  nefungují potvrzovací akce. `~/scorerapp-deploy-prep/04-deploy.sh`, pak ověřit `/health` a migrace.

## Úkol 2 — responzivní redesign (7 z ~14 stránek)

Hotovo: zápasy sezóny, dashboard, `/seasons`, `/matches`, `/players`, `/teams`, detail zápasu.

- [ ] `/seasons/{id}` — detail sezóny: HOTOVO v kódu, čeká na testy + proklikání + nasazení
- [ ] **Lokalizace Account stránek v SharedServices** — ChangePassword (6 textů), AccessPending (2),
  ForgotPassword (1), Register (1) mají texty natvrdo česky; žádná z nich nepoužívá `@S[...]`.
- [ ] `/players/{id}`, `/teams/{id}`, `/leagues/{id}` — detaily
- [ ] `/races`, `/races/{id}` — závody
- [ ] `/rankings`, `/profile`
- [ ] Klubové stránky: `/clubs`, `/organizations`, `/chat`, `/circulars`, `/cars`, `/join`
- [ ] Admin stránky

## Rozhodnutí Vítka (2026-09-28)

- Nasazování: **průběžně po každé hotové stránce**, ne až na konci.
- Lokalizace přihlašovacích stránek: opravit **v SharedServices**, ať to mají všechny aplikace.
- Google login: **až po úkolu 2**. Kód je zapojený, chybí jen klíče v produkční konfiguraci
  (callback `https://scorer.vo2info.cz/signin-google`); klíče vkládá uživatel sám.
- Zátěž Macu: uživatel **ukončí nepotřebné sessions**.

## Čeká na uživatele

- [ ] **SMTP** není vybrané → e-maily (pozvánky, oběžníky, Identity) jsou tiché.
  Připraveno: `~/scorerapp-deploy-prep/07-smtp-config.sh`. Doporučeno Brevo. Pak DNS SPF/DKIM/DMARC.
- [ ] **Heslo admin účtu** změnit (staré je v historii veřejného repa).
- [ ] **ntfy heslo** uložit do Vaultwardenu.
- [ ] **Historická expozice hesla `scorer_usr`** ve veřejném repu — přepsat historii, nebo repo na private?
  (Rotace netřeba, hodnota už v pg16 neplatí — ověřeno 2026-09-24 s governance session.)
- [ ] **Synchronizace sessions do mobilu** — zapnout účtové nastavení
  `connect_new_sessions_to_remote_control`? Ostatní sessions si Remote Control musí zapnout samy.

## Technický dluh nalezený při práci

- [ ] **Dev konfigurace na QNAPu** `/share/Public/BlazorScorerAppdev/…` má neplatné heslo k DB —
  doplnit produkční, až se bude dev kontejner spouštět. Governance session to chce ověřit.
- [ ] **Noční scheduled task „ScorerApp: noční deploy“** po úspěšném nasazení vypnout.
- [ ] **Přímé zprávy (DM)** — `ThreadParticipant` je namapovaný, ale `ChatService` nemá cestu, jak DM
  vlákno založit. Ve specifikaci ClubManageru byly až ve V2, takže to není regrese.
- [ ] **Aktualizace submodulu SharedServices** — dlouho se vynechávala; při nejbližší příležitosti
  vzít i cizí commity (mj. `d6b7936`, oprava `.sidebar--drawer`, nás se netýká).
- [ ] **Hláška `libgssapi_krb5.so.2`** v logu produkčního kontejneru — neškodná, jde umlčet doplněním
  balíčku do image.
- [ ] **Data Protection klíče v DB nejsou šifrované** (`No XML encryptor configured`) — vyžadovalo by certifikát.

## Zjištění k předání ostatním projektům

- `UiStats` ze SharedServices je stylovaný jen v `ui-2026.css`; aplikace, které ho nenačítají, mají
  karty bez pozadí a ikonu roztaženou přes celou šířku. Totéž hrozí u dalších sdílených komponent.
- `ConfirmService` + `ConfirmDialog` spolu nekomunikovaly → potvrzované akce tiše nedělaly nic.
