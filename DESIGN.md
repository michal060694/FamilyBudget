---
name: FamilyBudget
description: The household's private ledger for annual budget, tithe, funds, and debts
colors:
  surface-1: "#fef9f7"
  page-plane: "#faf3f1"
  text-primary: "#120b0a"
  text-secondary: "#524440"
  text-muted: "#6b5550"
  gridline: "#e6dbd9"
  baseline: "#bdada9"
  dusk-slate: "#1d3f60"
  gold: "#a06200"
  sage: "#196632"
  berry: "#912061"
  status-good: "#036819"
  status-critical: "#b00a1d"
typography:
  display:
    fontFamily: "system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSize: "1.6rem"
    fontWeight: 400
    lineHeight: 1.2
    letterSpacing: "normal"
  headline:
    fontFamily: "system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSize: "1.25rem"
    fontWeight: 400
    lineHeight: 1.3
  title:
    fontFamily: "system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSize: "1rem"
    fontWeight: 600
  body:
    fontFamily: "system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSize: "0.88rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSize: "0.78rem"
    fontWeight: 600
rounded:
  sm: "8px"
  md: "10px"
  lg: "16px"
  pill: "999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "20px"
  xl: "24px"
components:
  button-primary:
    backgroundColor: "{colors.dusk-slate}"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "10px 20px"
  button-primary-hover:
    backgroundColor: "{colors.dusk-slate}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.text-secondary}"
    rounded: "{rounded.md}"
    padding: "10px 20px"
  card:
    backgroundColor: "{colors.surface-1}"
    rounded: "{rounded.lg}"
    padding: "20px"
  card-header:
    backgroundColor: "{colors.dusk-slate}"
    textColor: "#ffffff"
    rounded: "{rounded.lg}"
    padding: "14px 20px"
  badge-good:
    backgroundColor: "{colors.status-good}"
    textColor: "#ffffff"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
  badge-critical:
    backgroundColor: "{colors.status-critical}"
    textColor: "#ffffff"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
---

# Design System: FamilyBudget

## 1. Overview

**Creative North Star: "The Steady Home, Board Edition"**

FamilyBudget is one household's private ledger, not a fintech product for strangers — but the household explicitly asked for a bolder, livelier, more structured feel, naming monday.com's board view as the reference: chunky rounded shapes and a table/board structure with colored group headers. This is the system's second real revision. The first (terracotta warm-up) fixed "cold and anemic." This one fixed "boring" by borrowing monday's structural moves — colored section headers, chunkier rounded shapes, solid-fill status chips, soft card elevation — while keeping the parts of the brief that still hold: calm precision, tabular numerals, Hebrew-first RTL, and no literal multi-user/team chrome (the borrowed vocabulary is visual, not organizational — this is still built for two people).

A third revision followed almost immediately: the household found the Annual Budget and Monthly Overview screens tedious and hard to hold in their head — too long, too much always visible at once — while the simpler two-table Debts screen read fine by comparison. The fix was progressive disclosure, not restyling: front-load the answer (Monthly Summary moved to sit right after Income, before the detail sections) and make the secondary detail sections collapsible, closed by default where they're usually empty. Density that earns its place (Debts' two plain tables) was left alone; density that was just unmanaged length (12 always-expanded months, 7 always-expanded overview cards) was hidden behind a clear, reversible toggle.

A fourth revision replaced the primary accent itself: the household found the terracotta orange too warm. The four board colors keep their roles exactly (Gold / Sage / Berry unchanged), but the primary/hero role moved from Hearth Terracotta to **Dusk Slate**, a deep, muted, low-chroma slate-blue — deliberately not the bright, saturated corporate blue the very first revision moved away from. The underlying neutrals were **not** re-tinted to follow it: they stay warm (hue ~35, the original terracotta's hue) on purpose, so the system now reads as a warm household structure carrying one cool, deep accent, rather than one hue doing everything. This is an intentional temperature contrast, not leftover inconsistency.

**Key Characteristics:**
- Every card has a full-bleed colored header bar (white bold text on a solid color), the way a monday board group has a colored header row — not a plain black title anymore
- Four board colors in rotation, each carrying real meaning: **Dusk Slate** (hero/default), **Gold** (money coming in), **Sage** (tithe & receivables), **Berry** (expenses & payables) — a deliberate **Full Palette** strategy, not Restrained
- Chunkier, rounder shapes throughout: 10px button/input radius, 16px card radius, bold 600-weight button labels
- Soft card elevation (a layered box-shadow) replaces the earlier flat-only system — depth now comes from shadow, not just background layering
- Status badges are solid-fill chips (white text on full-strength status color), not soft tinted pills
- Tabs are a filled segmented-pill control (active tab = solid Dusk Slate pill), not an underline
- Secondary detail sections are collapsible (native `<details>`/`<summary>`, styled as the same colored header bar) — the payoff surfaces first, detail is opt-in
- Tabular numerals everywhere money appears; RTL Hebrew as the native reading direction

## 2. Colors

A **Full Palette** strategy (a deliberate step up from the prior Restrained one-accent system): four named board colors, each with a fixed semantic assignment, sitting on a warm hue-35 neutral family that intentionally does *not* share the primary accent's hue (see the temperature-contrast note in Overview).

### Primary
- **Dusk Slate** (`#1d3f60` light / `#4675a4` dark): the default/hero board color. Deep and low-chroma by design — not a bright sky blue. Used for any card without an explicit money-direction meaning (the monthly summary, the annual-budget "pull from reserve" connector card), for hero numeric figures, active tab pill, links, and input focus rings. White text on the light value hits 10.9:1; white text on the dark value hits 4.8:1.

### Board Colors (one per money-direction meaning; each card gets exactly one)
- **Gold** (`#a06200` light / `#ba8416` dark) — money coming in: the income card. White text: 4.95:1.
- **Sage** (`#196632` light / `#488055` dark) — tithe accounting and money owed *to* the household: the tithe card, "חייבים לנו" (receivables). White text: 7.03:1.
- **Berry** (`#912061` light / `#ab4a7d` dark) — money going out: every expense card (monthly, general, fixed, regular) and "אנחנו חייבים" (payables). White text: 8.13:1.
- Fund cards rotate through all four board colors by index (dusk slate, gold, sage, berry, repeating) as a thin top accent stripe — each fund gets a visually distinct identity, the way monday assigns a color per group.

### Neutral
- **Ink** (`#120b0a` light / `#f7f0ee` dark) — primary text, `text-primary`. 17.8:1 on `page-plane`.
- **Warm Slate** (`#524440` light / `#c7b2ad` dark) — secondary text, `text-secondary`. 8.5:1 on `page-plane`.
- **Warm Ember Gray** (`#6b5550` light / `#9b8782` dark) — muted hints, `text-muted`. 6.6:1 on `surface-1` (light), 5.1:1 (dark).
- **Paper** (`#fef9f7` light / `#211816` dark) — card surfaces, `surface-1`.
- **Hearth Floor** (`#faf3f1` light / `#0e0806` dark) — page background, `page-plane`.
- **Hairline** (`#e6dbd9` light / `#322927` dark) — table dividers, `gridline`.
- **Baseline Gray** (`#bdada9` light / `#524440` dark) — input/button borders, `baseline`.
- **Whisper Border** (`rgba(18,11,10,0.10)` light / `rgba(247,240,238,0.10)` dark) — card border, a tint of the text color.

### Status (semantic only — solid-fill chips, never tinted pills)
- **Good** (`#036819` light / `#0ca30c` dark) — fully-used / on-track. 6.7:1 white-text contrast.
- **Critical** (`#b00a1d` light / `#e66767` dark) — genuinely urgent states only. 6.9:1 white-text contrast.
- Status chips are always solid color with white text — this is the one deliberate exception to "board colors are per-card": status meaning must stay identical everywhere, so it never borrows the card's board color.

### Named Rules
**The Card-Owns-One-Color Rule.** Every card gets exactly one board color (via a `--card-accent` custom property), applied to its header bar, its tinted table rows, and any hero figure inside it. A card never mixes two board colors, and the same card type always gets the same color (expenses are always Berry, income is always Gold) so the color becomes a learned wayfinding cue, not decoration.

**The Status-Is-Universal Rule.** Badge/status colors (good/critical/pending/partial) never change with the card's board color — "critical" is always the same red everywhere, regardless of which colored card it appears in. Board color = wayfinding by section; status color = meaning, and the two must never be confused.

**The Warm-Neutral Rule.** Every neutral carries a small chroma tint toward a fixed warm hue (35) — independent of the primary accent's own hue, which is now Dusk Slate's cool 250. The underlying structure (backgrounds, borders, body text) reads as one warm family that the board colors, including the cool primary, sit on top of; it is never re-tinted to chase whatever the primary happens to be.

## 3. Typography

**Body Font:** `system-ui, -apple-system, "Segoe UI", sans-serif` (single family, no serif pairing)

**Character:** One neutral system sans, used at every weight and size the interface needs. Board-header text is always 700-weight white; everything else follows the ramp below. All monetary and numeric values use `font-variant-numeric: tabular-nums`.

### Hierarchy
- **Display** (400, 1.6rem, tabular-nums): hero stat figures and the fund balance input.
- **Headline** (400, 1.25rem): page title ("תקציב הבית").
- **Title** (600–700, 1rem): card headers (700, white, on the colored bar), the tab bar, fund name fields.
- **Body** (400, 0.88rem): table cells and form values.
- **Label** (600, 0.78rem, `text-secondary`): field labels, table headers, stat-tile captions.
- **Hint** (400, 0.75–0.82rem, `text-muted`, often italic): empty-state copy and inline explanatory notes.

### Named Rules
**The Tabular Numerals Rule.** Any element displaying money or a count uses `font-variant-numeric: tabular-nums`.

**The One Ramp Rule.** Every text element maps to one of the five documented sizes — no one-off inline `font-size` values on individual components.

## 4. Elevation

No longer flat-by-default. Cards and stat-tiles carry a soft, layered `box-shadow` (`--shadow-card`: a tight 2px contact shadow plus a diffuse 12px ambient shadow, both tinted toward ink rather than pure black) that lifts every card visibly off the page — this is the one structural elevation change the monday-board reference asked for. Modals go further: a more pronounced shadow (`0 8px 40px`) plus the existing `rgba(0,0,0,0.45)` backdrop dim, since a modal should read as clearly above the board.

### Named Rules
**The Soft-Lift Rule.** Every card-like surface (`.card`, `.stat-tile`, `dialog.modal`) casts a soft, tinted shadow at rest — never a hard, dark, or pure-black shadow, and never triggered only on hover (the lift is always present, matching the board's constant "physical card" feel).

## 5. Components

### Buttons
- **Shape:** 10px radius (up from 8px), bold (600-weight) label, `padding: 10px 20px` (up from 7px 16px).
- **Primary:** Dusk Slate background, white text.
- **Hover:** brightness darken plus a 1px lift (`translateY(-1px)`) — a small tactile "press" cue; respects `prefers-reduced-motion`.
- **Focus:** a 2px Dusk Slate `outline` at 2px offset, universal across every button and the two clickable stat-tiles regardless of the card's board color — focus is a system-wide signal, not a themed one.
- **Secondary (ghost):** transparent background, `text-secondary` text, 1px `baseline` border.
- **Delete-row / notes-btn (row icon actions):** 30×30px circular icon buttons (`rounded.pill`), `text-muted` at rest, no border — a fifth revision from the earlier text-only glyphs, which read as cramped when two sat side by side in one table cell. Hover fills a soft 12%-tint circle of the button's own meaning color (`status-critical` for delete, the card's `--card-accent` for notes) instead of just recoloring the glyph. Row cells holding more than one of these buttons use `.row-actions` (flex, `gap: 6px`) so they never touch. Delete-row's confirm behavior is unchanged: first click arms an inline "לאשר מחיקה?" state (`status-critical`, bold); a second click within 3 seconds commits.
- **Notes-btn tooltip:** hovering (or focusing, for keyboard users) a notes button that already has a saved note pops up its actual text via a `surface-1` + `shadow-card` popover — the same look as the delete-row confirm overlay — so reading a note never requires opening its edit modal first. Empty notes show a plain "אין הערה עדיין" hint instead.

### Cards
- **Corner style:** 16px radius (up from 12px).
- **Header bar:** every card's `<h2>` bleeds edge-to-edge via negative margins, rounded on its top corners only, solid-filled with the card's `--card-accent` board color, white 700-weight text, `padding: 14px 20px`. This is the signature "board group header" move.
- **Background:** `surface-1`. **Border:** 1px `border`. **Shadow:** `--shadow-card` (see Elevation).
- **Internal padding:** 20px (the header bar's negative margins exactly cancel this on all three bled sides).
- **Stat tile** (a card variant): 14px radius, same shadow, holds a label / display-size value / optional hint. `.value.accent` and the clickable-tile hover border both resolve to the surrounding card's `--card-accent` (falling back to Dusk Slate outside any card).
- **Fund cards:** no header bar (the name is an inline-editable field, not a title); instead a 4px solid top border in the fund's rotated board color, cycling dusk slate → gold → sage → berry by index.

### Inputs / Fields
- **Style:** 1px `baseline` border, 8–10px radius (up from 6px), transparent or `page-plane` background depending on context.
- **Focus:** border shifts to Dusk Slate (universal, not per-card-color — same reasoning as button focus), background shifts to `page-plane`.
- **Inline table inputs:** borderless and transparent at rest, border appears only on hover/focus; a brief ✓ flash confirms a successful save.

### Badges (status chips)
- **Shape:** fully rounded (`999px`), `padding: 4px 12px` (up from 2px 8px).
- **Style:** solid-fill, white 700-weight text — a deliberate move away from the earlier 12%-tint pill, matching monday's solid status-cell look. Four states: `good`, `pending` (`text-secondary` fill), `partial` (Dusk Slate fill — universal, not card-colored), `critical`.

### Modals
- **Style:** `<dialog>` element, 16px radius, `surface-1` background, `border`, `width: min(640px, 92vw)`, `box-shadow: 0 8px 40px rgba(18,11,10,0.25)`.
- **Backdrop:** `rgba(0,0,0,0.45)`.

### Tabs
- **Style:** a filled segmented-pill control — the `.tabs` container is itself a rounded pill (`999px`) with a 1px border and `page-plane` background; each `.tab-btn` is a smaller pill inside it. The active tab is a solid Dusk Slate pill with white text; inactive tabs are transparent with `text-secondary` text and a soft slate-tinted hover.

### Collapsible Sections (progressive disclosure)
- **Mechanism:** native `<details class="card [board-color]">` with a `<summary>` styled identically to the plain `<h2>` header bar (same bleed, same `--card-accent` fill, same radius) plus a trailing `▾` chevron that rotates 180° on open.
- **Default state is a judgment call, not a rule:** open by default for anything the household checks every time (Income, Tithe, Fixed Expenses, Regular Expenses); closed by default only for sections that are usually empty or purely informational (the annual-withdrawals connector card).
- **What stays a plain, non-collapsible `.card`:** the one "answer" card per screen — Monthly Summary is a plain card, not a `<details>`, and sits immediately after Income rather than at the bottom of the scroll, so the bottom line is never more than one card away.
- **The Annual Budget month table** uses the same open/closed judgment call per row instead of per card: a month with items expands automatically and shows its count inline (e.g. "יוני (3)"); a month with zero items collapses to a single header line. Clicking a month's name toggles it; clicking "+" always expands first, so the add-form is never hidden. The month-name button holds a fixed 120px width regardless of the Hebrew month name's length, so the "+" button lines up in a straight column down all 12 rows instead of drifting with "תשרי" vs "אב".

## 6. Do's and Don'ts

### Do:
- **Do** keep the household scale: no multi-user chrome, no permissions UI, no team-oriented navigation — the board *look* is borrowed, not the team-collaboration *function*.
- **Do** use tabular numerals for every monetary or count value.
- **Do** give every card exactly one board color from the four (dusk slate / gold / sage / berry), assigned by money-direction meaning, never mixed within one card.
- **Do** keep status chip colors (good/critical/pending/partial) identical everywhere regardless of the card's board color.
- **Do** apply the soft card shadow to every card-like surface at rest, not just on hover.
- **Do** tint every neutral toward hue 35 so the four board colors sit on a cohesive warm base rather than a gray shell.
- **Do** put the answer before the detail: a screen's one summary/hero card stays a plain always-visible `.card` near the top; supporting detail sections become collapsible `<details>`, closed by default when they're usually empty.

### Don't:
- **Don't** let a screen's total visible content grow unbounded just because the data model has room for it (12 months, 7 overview sections) — collapse what's usually empty or already-reviewed instead of asking the reader to scroll past all of it every time.
- **Don't** flatten cards back to borderless-and-shadowless — that was the pre-board-revision system; the shadow is now load-bearing for the board feel.
- **Don't** make this feel like a corporate SaaS admin panel or an anxious finance app (harsh red banners, dense enterprise data-grid density) — PRODUCT.md names both as anti-references; colorful ≠ corporate as long as the palette stays warm and the copy stays calm.
- **Don't** add a fifth board color, or assign board colors arbitrarily instead of by money-direction meaning — four is the limit, and each must earn its place.
- **Don't** let a board color leak into status-chip meaning (a "critical" badge inside a Gold card is still the universal critical red, never gold).
- **Don't** brighten or saturate Dusk Slate back toward a vivid sky/corporate blue — the household explicitly wanted "less warm," not "cold" again; the primary must stay deep and low-chroma (roughly L 0.35–0.55, C ≤ 0.09 in OKLCH), never a bright saturated blue.
- **Don't** re-tint the neutrals to follow the primary accent's hue — they stay warm (hue 35) by design, independent of whatever the primary is; that decoupling is what keeps a cool primary from reading as "cold" again.
- **Don't** use `border-left`/`border-right` colored stripes as a card or row accent — the fund-card top-border stripe is the one sanctioned exception (full-width, top edge only, never a side).
- **Don't** default to LTR layouts or English-first copy — the system is Hebrew-first and RTL by construction.
