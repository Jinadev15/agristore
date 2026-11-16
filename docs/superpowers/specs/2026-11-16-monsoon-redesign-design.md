# AgriStore redesign: Monsoon theme with rich motion

Date: 2026-11-16. Status: direction chosen by the team (canvas "AgriStore Design Explorations", row C).

## Intent

The first version works but looks like a Bootstrap admin panel. The team wants a site that
feels studio-made and expensive, with motion throughout, while staying on ASP.NET Core MVC
with Razor (`.cshtml`) plus JavaScript. The same pass fixes every known rough edge.

## Goals

- Public landing page that tells the post-harvest-loss story, then leads into the app.
- Every app page (overview, stock, lot, storage unit, readings, crops, forms, sign-in,
  register, errors) restyled in one design system.
- Purposeful animation on load, scroll, hover, press, navigation and feedback.
- Edge cases fixed (list below), with tests where logic changes.

## Non-goals

- No change to the risk model, the data model's meaning, or the API contract.
- No SPA framework (React etc.). No build step for front-end assets.

## Anti-goals (a failure even if it works)

- Generic AI look: gradient washes, glassmorphism, left-border cards, emoji, all-caps
  eyebrows, "→" on buttons, identical rounded SaaS cards.
- Motion that slows work down: nothing blocks input, nothing replays on every page in the app,
  everything respects `prefers-reduced-motion`.

## Approaches considered

1. **Monsoon (chosen by the team)**: colour-blocked editorial story, Fraunces serif display +
   Instrument Sans body, indigo / saffron / paddy / mist blocks.
2. Seasons (crop colour system, Bricolage): strongest brand system, not chosen.
3. Ledger (dense keyboard-first app): best daily-use structure; its command menu is borrowed.
4. Thermal (dark heat scale): rule "every colour means something" is borrowed for risk colours.

## Design system

Fonts (Google Fonts, `display=swap`, system fallbacks): Fraunces (opsz, wght, SOFT 100) for
display and big numbers; Instrument Sans 400/500/600 for text and UI.

Colour roles (CSS custom properties in `site.css`):

| Token | Hex | Role |
|---|---|---|
| `--indigo` | #1F2A5C | Hero and page-header bands, primary dark surface |
| `--indigo-deep` | #161E45 | Footer, overlays, pressed states on indigo |
| `--saffron` | #F2A007 | The one call to action colour; "act now" band |
| `--paddy` | #2E6B3B | Crop guide band; Low risk; success |
| `--mist` | #E8EBF4 | App ground |
| `--paper` | #F3F1EA | Text on dark bands |
| `--ink` | #1A1530 | Body text on light, text on saffron |
| `--rust` | #B44A00 | High risk, warnings |
| `--crimson` | #9E1B32 | Critical risk, destructive actions only |

Rules: saffron is reserved for the primary action and the "move these first" band. Risk colours
(paddy, saffron-dark #8A5A00 text, rust, crimson) also differ in lightness. Colours repeat only
as the same role. Corners 4px (sharp, editorial); no drop shadows except overlays.

## Site structure

- `/` landing (anonymous): hero with rain, the 471 t story band, three-step band, crop guide
  band, closing CTA. Signed-in visitors see "Open the dashboard".
- `/Dashboard` overview (signed in): indigo band with headline sentence and counters, saffron
  "move these first" band, storage unit cards.
- Stock, lot detail, storage units, unit detail, readings, crops, all forms: indigo page band
  with the title, mist content area.
- Identity: custom Login, Register, Access denied pages; Manage pages get compatible styles.
- Error pages: 404 and 500 in the same style.
- Ctrl+K command menu everywhere in the app: jump to pages, lots and units.

## Motion inventory (GSAP 3 + ScrollTrigger from cdnjs, plus small vanilla JS)

1. First-visit curtain: indigo panel lifts off the landing once per session.
2. Hero headline: words rise out of masks in sequence.
3. Rain: canvas rain on indigo bands, drifts with the pointer.
4. Background colour morph: body colour follows the band in view on the landing.
5. Counters: big numbers count up when they enter the viewport.
6. Buttons: fill sweeps in from the pointer's entry side; primary CTAs pull towards the pointer
   (magnetic); press scales down.
7. Links: underline draws left to right.
8. Header: hides on scroll down, returns on scroll up; active nav indicator slides.
9. Lists and table rows: staggered reveal.
10. Risk score: ring draws and number counts on lot detail.
11. Shelf-life and capacity bars grow from zero.
12. Charts: Chart.js line draws in.
13. Page transitions: cross-document View Transitions (main content cross-fades and lifts).
14. Toasts: TempData messages slide in, auto-dismiss with a progress line.
15. Forms: label lift on focus, invalid fields shake once.
16. Cards: spotlight follows the pointer.
17. Command menu: scales in, keyboard navigable.

All of the above are disabled or reduced to instant state changes under
`prefers-reduced-motion: reduce`. Nothing relies on JS to show content: without JS every element
is visible.

## Edge cases to fix

- Refresh or Back after a failed form post hits `GET /StockLots/Save` (404): add GET fallbacks
  that redirect to the form.
- Over-posting on edit: copy only editable fields onto the tracked entity.
- `SetStatus` accepts any integer: validate with `Enum.IsDefined`.
- Lowering a unit's capacity below what it holds: refuse with the stored amount.
- Duplicate storage unit names: refuse.
- API accepts readings dated in the future: refuse (5 minute tolerance).
- API key compared with `==`: use constant-time comparison.
- Server runs in UTC on Render, so times show 5.5 h off: set `TZ=Asia/Kolkata` and en-IN culture.
- Bare 404/500 pages: styled status-code pages.
- Login has no lockout: enable `lockoutOnFailure`.
- Identity Manage pages depend on Bootstrap: provide compatible styles.
- Chart.js or fonts fail to load: page still readable, chart shows a text fallback.
- Empty states for every list (no units, no lots, no readings, no matches).
- Long names and big numbers: wrap or truncate without breaking layout at 360 px width.

## Left to the builder

Exact copy beyond the canvas, spacing scale, component class names, file split of JS.
