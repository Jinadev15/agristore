# Monsoon Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild AgriStore's UI in the Monsoon design system with rich motion, and fix the known edge cases.

**Architecture:** Razor views stay server-rendered. Bootstrap is removed; one `site.css` holds tokens and components, `motion.js` (GSAP 3.12.5 + ScrollTrigger from cdnjs) holds animation, `app.js` holds behaviour (nav, toasts, command menu, buttons). Landing moves to `/` (anonymous), overview to `/Dashboard`.

**Tech Stack:** ASP.NET Core MVC (.NET 10), EF Core SQLite, Identity, GSAP 3.12.5, Chart.js 4.4.1, xUnit + Microsoft.AspNetCore.Mvc.Testing.

**Spec:** `docs/superpowers/specs/2026-11-16-monsoon-redesign-design.md`

## Global Constraints

- Fonts: Fraunces (opsz, wght, SOFT 100) display; Instrument Sans 400/500/600 text.
- Colours exactly: indigo #1F2A5C, indigo-deep #161E45, saffron #F2A007, paddy #2E6B3B, mist #E8EBF4, paper #F3F1EA, ink #1A1530, rust #B44A00, crimson #9E1B32. Corners 4px.
- CDN scripts pinned: `cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/gsap.min.js`, `.../3.12.5/ScrollTrigger.min.js`, `.../Chart.js/4.4.1/chart.umd.min.js`.
- Every animation off under `prefers-reduced-motion: reduce`; content visible without JS.
- No gradient washes, left-border cards, emoji, all-caps labels, "→" in buttons.
- Layout works at 360 px width with no horizontal page scroll.

## Review Focus

- Refresh/Back after a failed form post: expect the form again, never a 404.
- A sensor posting a reading dated tomorrow: expect 400, not a stored reading.
- Admin lowers a unit's capacity below its stock: expect a validation error naming the stored tonnes.
- Unknown URL: expect the styled 404 page with a way home.
- Visitor with reduced motion or JS blocked: every section and number is visible immediately.

---

### Task 1: Backend edge cases

**Files:**
- Modify: `src/AgriStore.Web/Controllers/{StockLotsController,StorageUnitsController,ReadingsController}.cs`, `Controllers/Api/StorageApiController.cs`, `Middleware/ApiKeyMiddleware.cs`, `Program.cs`, `Dockerfile`
- Create: `tests/AgriStore.Tests/ApiTests.cs`, `src/AgriStore.Web/Controllers/DashboardController.cs`

**Interfaces:**
- Produces: `DashboardController.Index()` returns the overview (moved from `HomeController.Index`); `HomeController.Index()` is `[AllowAnonymous]` landing taking `DashboardViewModel`; `HomeController.Status(int code)` renders `Views/Shared/Status.cshtml`.

- [ ] Tests (`ApiTests`, `WebApplicationFactory<Program>` with a temp SQLite file):
  `PostReading_FutureDate_Returns400` (RecordedAt = now + 1 day), `PostReading_WrongKey_Returns401`, `UnknownPage_Returns404WithStyledPage` (body contains "This page does not exist"), `Landing_IsPublic` (GET `/` → 200).
- [ ] Run, expect fails. Implement: API future check (> now + 5 min → ValidationProblem); `CryptographicOperations.FixedTimeEquals` in middleware; `SetStatus` rejects `!Enum.IsDefined`; unit Save refuses capacity < stored and duplicate names; lot Save copies fields onto the tracked entity on edit; `[HttpGet] Save` actions redirect to Create; `PasswordSignInAsync` lockout via Identity options (5 tries, 5 min); `UseStatusCodePagesWithReExecute("/Home/Status/{0}")`; en-IN request culture; `ENV TZ=Asia/Kolkata` + `tzdata` in Dockerfile.
- [ ] Run `dotnet test`, expect all pass. Commit.

### Task 2: Design system, layout and motion

**Files:** Replace `wwwroot/css/site.css`; create `wwwroot/js/app.js`, `wwwroot/js/motion.js`; rewrite `Views/Shared/_Layout.cshtml`, `_LoginPartial.cshtml`, `_RiskBadge.cshtml`, `_ShelfBar.cshtml`; create `Views/Shared/_PageBand.cshtml`, `_CommandMenu.cshtml`.

**Interfaces:**
- Produces CSS components: `.band` (+ `--indigo|--saffron|--paddy|--mist`), `.wrap`, `.btn` (+ `--primary|--ghost|--line|--danger|--small`), `.card`, `.table`, `.field`, `.risk risk--{low|medium|high|critical}`, `.bar`, `.toast`, `.empty`, `.grid-auto`.
- Data attributes `motion.js` acts on: `data-split` (word rise), `data-count="1303"` (+ `data-decimals`), `data-reveal` (fade-up), `data-stagger` (children stagger), `data-rain` (canvas rain), `data-bg="#hex"` (landing colour morph), `data-magnetic`, `data-bar-to="62"` (width %), `data-ring="84"`.
- `app.js`: header hide-on-scroll, nav toggle, toast auto-dismiss, button sweep (`--x/--y` vars), card spotlight, command menu (`Ctrl/⌘+K`, fetches `/api/units` and `/api/lots/at-risk?minLevel=Low`).
- `_PageBand` model: `(string Title, string? Subtitle)` via ViewData `Title`/`Subtitle`, actions via section `BandActions`.
- [ ] Build passes, layout renders in browser. Commit.

### Task 3: Landing and dashboard views

**Files:** `Views/Home/Index.cshtml` (landing), `Views/Dashboard/Index.cshtml` (overview).
- Landing sections in order: hero (indigo, rain, split headline, CTA), story (saffron, counter of tonnes at risk from live data), steps (mist, 3 steps), crop guide (paddy, rows from DB), closing CTA (indigo-deep). Uses live `DashboardViewModel` + crop list.
- [ ] Browser check at 1440 and 375 width. Commit.

### Task 4: Identity pages

**Files:** create `Areas/Identity/Pages/Account/{Login,Register,AccessDenied}.cshtml(.cs)`, `_ViewImports.cshtml`.
- Login: email, password, remember me, lockout message; Register: email, password, confirm; both split screen (indigo story panel + form).
- [ ] Sign in, wrong password ×5 shows lockout, register works. Commit.

### Task 5: App pages

**Files:** all views under `Views/StockLots`, `Views/StorageUnits`, `Views/Readings`, `Views/Crops`.
- Each uses `_PageBand`, `.card`/`.table`, empty states, motion attributes; lot details gets the risk ring; unit details chart draws in with text fallback.
- [ ] Browser walk-through of every page at 1440 and 375. Commit.

### Task 6: Verify and ship

- [ ] `dotnet test` all green; browser pass with reduced motion on; push; Render deploy live; smoke test live URLs.
