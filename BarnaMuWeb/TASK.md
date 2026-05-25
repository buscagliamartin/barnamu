# BarnaMu Web — Task Backlog

Prioritized work derived from the initial code review. Priorities ascending in urgency:
**P1 (Bugs) → P2 (Security) → P3 (Realtime) → P4 (Ideas)**.

---

## P1 — Bugs / correctness

- [ ] **Sidebar rates are hardcoded.** [_Layout.cshtml:96-104](Pages/Shared/_Layout.cshtml#L96-L104)
  prints `×30 / ×35 VIP` and `×5 / ×7 VIP` as literal text instead of reading
  `opts.Rates`. If rates change in `appsettings.json`, the sidebar lies. Bind to
  `@opts.Rates.ExpNormal` / `ExpVip` / `DropNormal` / `DropVip`.
- [ ] **Server clock ignores DST.** [_Layout.cshtml:202-203](Pages/Shared/_Layout.cshtml#L202-L203)
  hardcodes `+2h` (CEST). In winter Spain is CET (+1), so the clock is an hour off
  Oct–Mar. Same hardcoded assumption for Castle Siege "18:00 UTC" at
  [_Layout.cshtml:241](Pages/Shared/_Layout.cshtml#L241).
- [ ] **Event countdowns are hardcoded guesses.** `nextFire(...)` anchors at
  [_Layout.cshtml:226-231](Pages/Shared/_Layout.cshtml#L226-L231) may not match the
  real OpenMU event schedule. Players could show up at the wrong time. (Real fix lives
  in P3 — drive from server/config instead of JS literals.)
- [ ] **`State != 0` online count goes stale on a hard crash.**
  [BarnaMuDb.cs:134-139](Data/BarnaMuDb.cs#L134-L139) — if the game server dies without
  cleanly resetting account state, players stay "online" in the DB. Document the caveat
  / consider a freshness guard.
- [ ] **Synchronous `conn.Open()`** blocks a thread-pool thread per call.
  [BarnaMuDb.cs:27-32](Data/BarnaMuDb.cs#L27-L32) — switch to `OpenAsync()`.

---

## P2 — Security (before going public)

- [ ] **Enable HTTPS + secure cookie.** Auth cookie ([Program.cs:24-35](Program.cs#L24-L35))
  is sent over plain HTTP today. Put Cloudflare/TLS in front, then set
  `Cookie.SecurePolicy = Always` and re-enable HSTS + HTTPS redirect
  ([Program.cs:88-92](Program.cs#L88-L92)).
- [ ] **Least-privilege DB user.** [appsettings.json:11](appsettings.json#L11) still uses
  `postgres`. Create the `barnamu_web` role from the README and switch the connection
  string to it before opening the port.
- [ ] **Captcha on public forms.** `/Register` and `/ReportBug` have rate limiting only;
  add a captcha to stop bots once spam appears.

---

## P3 — Realtime info from server / DB

- [ ] **Live online-players list** (names + class + level + map). One query against
  `data."Character"` / account state. High value, low effort. (Also feeds the P4 widget.)
- [ ] **SignalR push** to replace the 30s `/api/online-count` polling
  ([_Layout.cshtml:259-274](Pages/Shared/_Layout.cshtml#L259-L274)) — instant updates,
  less DB hammering. Nice-to-have, not urgent at private-server scale.
- [ ] **Accurate event timers.** OpenMU runs events in-memory, so the DB won't give them.
  Replace hardcoded JS countdowns with a maintained config block mirroring the real
  server event schedule (fixes P1 event-countdown bug properly).
- [ ] **Castle Siege owner** — read which guild holds the castle from the guild schema.

---

## P4 — Ideas worth adding

### High value
- [x] **Password recovery ("forgot my password")** — two paths, both rate-limited:
  - Security-code reset (`/ForgotPassword`): account name + numeric code → new password.
    No email needed.
  - Email reset link (`/ForgotPasswordEmail` → emailed link → `/ResetPassword?token=`):
    single-use SHA256-hashed token, 1h expiry, neutral messaging (no enumeration).
  - Email sending via `EmailService` (MailKit) through a Brevo SMTP relay. Disabled until
    `BarnaMu:Smtp` creds are filled in `appsettings.Local.json` (logs the link meanwhile).
- [ ] **Live online-players widget** (consumes the P3 query).
- [ ] **VIP self-service status** — panel already reads `VipExpirationDate`; surface
  "VIP expires in N days" prominently.
- [ ] **News editor** — GM-only authenticated page to post news instead of hand-editing
  `Content/news.json`.

### Medium
- [ ] **Character profile enrichment** — `/char/{name}` could show equipment, guild logo,
  PK status, kill count.
- [ ] **Discord widget** in the sidebar (`DiscordInviteUrl` is configured but unused for
  embeds).
- [ ] **Castle Siege owner display** (overlaps P3).

### Bigger bets
- [ ] **Web shop / donation → in-game credits** pipeline (PayPal already wired for VIP).
- [ ] **Event calendar** driven by real server config.
- [ ] **Account email verification** (`EMail` column exists but isn't validated).

---

## P5 — Style & motion

All items are vanilla CSS/JS (no new dependencies) and must respect
`prefers-reduced-motion`.

### Hero / first impression (highest payoff)
- [ ] **Animated hero background** — looping muted `<video>` (with poster fallback) or
  parallax layered scene replacing the static elf image.
- [ ] **Particle/ember layer** — slow-drifting gold embers over the hero (small canvas,
  ~40 particles).
- [ ] **Animated stat counters** — headline numbers (accounts, characters, online) count
  up from 0 on scroll-in.

### Motion & micro-interactions
- [ ] **Scroll-reveal** — cards/sections fade + slide up on viewport entry
  (IntersectionObserver, no library).
- [ ] **Button & card juice** — gold glow on hover, subtle scale, sheen sweep on `.btn-gold`.
- [ ] **Animated gradient borders** on featured cards (VIP, Download) — rotating
  conic-gradient, pure CSS.
- [ ] **Tilt-on-hover** for character/rank cards (small vanilla JS).

### Live / dynamic content (ties into P3/P4)
- [ ] **Live online-players ticker** — horizontal marquee of who's online now (consumes
  the P3 query).
- [ ] **Top player of the week spotlight** card with class portrait.
- [ ] **Online-count flash** — flash/animate the number when the live count changes.

### Visual identity polish
- [ ] **Class portrait art** on rankings/profiles instead of plain text class names.
- [ ] **Rarity color-coding** extended consistently (top-3 ranks with crown icons, etc.).
- [ ] **Custom cursor / hover sound** on CTAs (optional, toggleable).

### Recommended starter pack (high ROI, self-contained)
1. Animated hero (video or parallax + embers)
2. Scroll-reveal + animated counters
3. Button shimmer / gold-glow hovers
4. Live players ticker (after P3 query exists)

