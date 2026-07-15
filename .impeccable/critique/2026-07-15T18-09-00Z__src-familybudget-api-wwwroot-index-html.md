---
target: src/FamilyBudget.Api/wwwroot/index.html
total_score: 22
p0_count: 2
p1_count: 2
timestamp: 2026-07-15T18-09-00Z
slug: src-familybudget-api-wwwroot-index-html
---
Method: dual-agent (A: a89d76cfee3f93999 · B: a67bc77f6ed8014e1)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 1 | No button disables or shows a spinner during its `fetch`; every save/delete just silently re-renders with no "saved" acknowledgment. |
| 2 | Match System / Real World | 3 | Domain vocabulary (מעשר, הו״ק, קופה) and the lettered א-ד tithe breakdown mirror how the household actually thinks about this money. |
| 3 | User Control and Freedom | 2 | Blur-to-save inline inputs have no cancel/Escape path — only Enter-to-commit is wired; clicking away silently commits the field. |
| 4 | Consistency and Standards | 2 | Delete confirmation applied to only 3 of 7 delete actions; the other 4 delete instantly with zero warning. |
| 5 | Error Prevention | 2 | Numeric guards exist but thresholds are inconsistent (`>= 0` vs `> 0` across near-identical forms), and no HTML-escaping on interpolated user text. |
| 6 | Recognition Rather Than Recall | 3 | Persistent labels and contextual hints (e.g. the funds empty-state naming the exact button to press) are good. |
| 7 | Flexibility and Efficiency | 3 | Formula-aware amount fields (`600-200`) and Enter-to-blur are genuine power-user features, undercut by add-rows that auto-close after one save. |
| 8 | Aesthetic and Minimalist Design | 3 | Flat surfaces, one accent, tabular-nums are consistently applied; Monthly Overview is dense but appropriately so for a monthly "closing the books" task. |
| 9 | Error Recovery | 2 | Validation messages are calm and in Hebrew, but raw server error text (`status: message`) is surfaced verbatim and unfiltered on any unhandled failure. |
| 10 | Help and Documentation | 1 | No help affordance or glossary anywhere; jargon like הו״ק has no inline explanation. |
| **Total** | | **22/40** | **Acceptable** |

## Anti-Patterns Verdict

**LLM assessment**: Not AI-slop. This is hand-written, domain-literate engineering — the CSS custom-property system maps precisely to DESIGN.md's tokens, badge tints are derived at runtime via `color-mix()` rather than hardcoded, and the money-formula parser is a deliberate, commented, non-`eval()` recursive-descent evaluator. What holds it back isn't genericness, it's production maturity: no loading states, an inconsistent delete-confirmation policy, and unescaped HTML interpolation throughout.

**Deterministic scan**: 6 findings (exit code 2). Two are false positives worth noting: the modal backdrop color (`rgba(0,0,0,0.45)`, line 131) is explicitly documented in DESIGN.md's Elevation and Components sections — the detector's string match just didn't normalize whitespace between `rgba(0, 0, 0, 0.45)` and `rgba(0,0,0,0.45)`. The `0.82rem` hint font-size (line 147) sits exactly inside DESIGN.md's documented 0.75–0.82rem Hint range; the detector checks literal values, not ranges, so it also misfired. Three findings are genuine, undocumented drift: `.tab-btn` at `0.95rem` (line 232), `.fund-name-input` at `1.05rem` (line 1600), and `.fund-balance-input` at `1.3rem` (line 1606) — none of these sizes appear anywhere in the DESIGN.md type ramp. The em-dash-overuse finding (17 matches) is technically correct but overstated: most matches are a UI placeholder glyph for not-yet-loaded numbers (`—` inside empty stat tiles) or em-dashes inside JS comments, not AI-cadence prose; only ~6 instances are genuine prose em-dashes.

**Visual overlays**: unavailable — no browser automation tool was exposed in this session, so no live DOM/screenshot inspection or on-page overlay was produced. This critique is based on a static source read plus the deterministic CLI scan only.

## Overall Impression

The system is well-built where it counts (the token system, the formula parser, the calm badge vocabulary) and genuinely earns its "Steady Home" brief in its restraint. But it currently behaves like a developer's API test client rather than a household's trusted financial ledger: nothing confirms a save happened, deletes behave inconsistently (some ask, most don't), and any raw server hiccup or a stray `"` in a note field will surface as broken markup or a technical error message — exactly the moments where "calm, gracious host" matters most and currently isn't delivered. The single biggest opportunity is closing the gap between the (good) visual calm and the (missing) operational calm: acknowledge saves, unify deletes, and never let raw technical text leak into a warm Hebrew interface.

## What's Working

- **Formula-aware amount inputs**: typing `600-200` in an income/expense field is parsed by a hand-rolled, explicitly non-`eval()` recursive-descent evaluator that shows the computed value while blurred and the raw formula while focused — a real accommodation for two people doing manual arithmetic bookkeeping, not a generic input.
- **Dark-mode-complete token system**: every color in `:root` has a mirrored dark-mode override, and badge tints are derived at runtime via `color-mix(in srgb, var(--status-good) 12%, transparent)` rather than separately hand-picked pastel hexes — one source of truth for the calm status vocabulary in both themes.
- **Inline add-row over modal-first**: every "add" affordance across all 4 tabs inserts a real `<tr class="add-form">` with input fields directly into the table rather than popping a dialog, reused identically everywhere — genuine progressive-disclosure discipline, with modals reserved only for the two cases (fixed-donations, prior-month) that need real drill-down.

## Priority Issues

**[P0] Inconsistent delete confirmation policy.** Deleting a transaction, standing order, budget item, or fund earmark fires the DELETE request immediately with zero confirmation, while deleting an annual-budget item, a fund, or a debt requires a native `confirm()`. For a two-person household ledger, an accidental ✕ click permanently removes a real transaction or standing order with no warning and no undo.
**Why it matters**: this directly undermines "everything organized, nothing forgotten" — the one thing the household is trusting the app to protect.
**Fix**: pick one confirmation pattern (ideally not native `confirm()` — see next issue) and apply it to all 7 delete paths, e.g. an inline arm/confirm state on the ✕ button itself, or the existing `<dialog class="modal">` component already used elsewhere in the file.
**Suggested command**: `/impeccable harden`

**[P0] No pending/disabled state on any async action.** Every button that triggers a `fetch` stays fully clickable for the request's duration, with no spinner, no `disabled`, and no success acknowledgment beyond the eventual silent re-render. On a slow connection, a double-click on the repay button or an income add-row can double-post a debt repayment or a transaction.
**Why it matters**: in a money-tracking tool this is a correctness risk, not just polish — and it's also why "Visibility of System Status" scored 1/4.
**Fix**: disable the triggering control for the request's duration and add a brief inline "נשמר" confirmation on success.
**Suggested command**: `/impeccable harden`

**[P1] Unescaped user input interpolated into `innerHTML`.** Every row-render function builds markup via template literals with raw field values (item name, debt notes, fund name, earmark label) with no HTML-escaping utility anywhere in the file. A household member typing a `"` or `<` into any description/name field will corrupt that row's markup or inject arbitrary HTML.
**Why it matters**: this isn't hypothetical — it will happen the first time either spouse types a quotation mark into a note field.
**Fix**: add a small `escapeHtml()` helper and route every interpolated string field through it before insertion.
**Suggested command**: `/impeccable harden`

**[P1] Raw server errors surfaced verbatim.** Any unhandled API error is thrown as `` `${res.status}: ${text}` `` and displayed as-is, prefixed only with "שגיאה: ". A technical, possibly non-Hebrew error string can appear inside an otherwise fully warm Hebrew interface at exactly the moment (an error) when tone matters most.
**Why it matters**: directly contradicts the "gracious host, not admin panel" brand goal from PRODUCT.md.
**Fix**: map known status codes to calm Hebrew copy; gate raw technical text behind a collapsed "details" affordance.
**Suggested command**: `/impeccable clarify`

**[P2] Monthly Overview has no in-tab navigation, and typography has started drifting from DESIGN.md.** The Overview tab is a single uninterrupted scroll through 7 always-expanded cards, with no sticky section index and no default-collapse for often-empty sections (e.g. annual withdrawals). Separately, the deterministic scan caught 3 genuine undocumented font sizes (`.tab-btn` 0.95rem, `.fund-name-input` 1.05rem, `.fund-balance-input` 1.3rem) that don't match DESIGN.md's ramp — an early sign of the kind of drift `document`/`polish` passes are meant to catch before it compounds.
**Why it matters**: given the once-a-month "closing the books" use case the density itself is broadly appropriate, but every visit still requires scrolling past unchanging content, and unmanaged type-scale drift will make future screens harder to keep consistent.
**Fix**: add a slim sticky in-tab jump list, default-collapse the annual-withdrawals card when empty, and fold the 3 drifted font-sizes into the documented ramp (or add them to DESIGN.md if they're intentional new roles).
**Suggested command**: `/impeccable layout`

## Persona Red Flags

**Alex (impatient power user, monthly use)**: Every add-row form is single-shot — after saving once, the form closes and refilling it from scratch is required to add a second income line or expense category, with no "save & add another." The month-number input (`ov-month`) isn't client-side clamped to 1-12, so a typo only surfaces as a raw server error (compounding the P1 above). Tab switching is click-only with no keyboard path.

**Sam (accessibility-dependent)**: The two clickable stat-tiles that open the fixed-donations and prior-month modals are plain `<div>`s with only a click listener — no `<button>`, `role`, or `tabindex` — **a keyboard-only user cannot open either modal at all.** All delete buttons rely solely on a `title` attribute for their accessible name (not reliably exposed by all screen readers, and gives no row-specific context). There's no `aria-live` region anywhere, so a screen-reader user gets no announcement when stat tiles update after a save. `text-muted` (#898781) on `surface-1` is a borderline-to-failing contrast pair for the empty-state and hint copy that carries a lot of the interface's guidance text.

**Riley (stress tester)**: Formula support (`600-200`) works in Overview's income/expense fields but not in Funds or Debts amount fields, which are plain `<input type="number">` — an inconsistency Riley will find fast. Validation thresholds differ between visually identical forms (`amount > 0` in most places, `amount >= 0` in budget items and the inline blur-commit). The unescaped-`innerHTML` issue above is the single most concrete stress-test failure: any `"` or `<` in a name/note field breaks or corrupts that row.

## Minor Observations

- Empty states are inconsistently taught: the funds empty-state names the exact button to press ("לחצי על + הוסף קרן"), but most other empty rows (e.g. the monthly expense tables) just state absence.
- `fmtMoney` always rounds to 0 decimals, but fund balances, earmarks, and debts all accept cents (`step="0.01"`) — displayed totals can silently diverge from stored precision.
- Buttons have no custom `:focus` styling (inputs do); likely falls back to the browser default focus ring, which works but is visually inconsistent with the rest of the tokenized system.
- The em-dash detector finding is mostly a false alarm on this file (see Anti-Patterns Verdict) — the actual prose em-dash count is small; no action needed there beyond maybe swapping the placeholder glyph convention if you want to remove the finding entirely.

## Questions to Consider

- Is silent, unconfirmed deletion for transactions/standing-orders/budget-items/earmarks intentional (cheap to fix back) or an oversight relative to the confirmed deletes elsewhere? Right now both behaviors coexist with no visible logic distinguishing them.
- The page still brands itself as "בדיקת API" (API test client) in the title and subtitle — is that deliberate scaffolding you plan to replace, or has it quietly become the permanent UI? That changes how much visual/tone investment is worth making right now.
- Is the funds/debts formula-input gap a deliberate scope decision (those amounts are always single known numbers) or just an implementation gap worth closing for consistency with Overview?
