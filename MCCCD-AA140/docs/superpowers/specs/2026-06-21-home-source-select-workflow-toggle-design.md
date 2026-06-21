# Home Source-Select Workflow Toggle (Destination-first ⇄ Source-first) — Design

**Date:** 2026-06-21
**Scope:** Panel-side only (Svelte). No contract changes — both workflows ride the
existing `Display{N}Source` set signals and `Display{N}SourceFb` feedback.
**Pages / files touched:** `Home.svelte`, `lib/stores/router.ts`, `lib/stores/session.ts`.
**Purpose:** Ship a live A/B toggle so the room's source-selection workflow can be
evaluated on glass in two interaction models without rebuilding the panel between tests.

## 1. The two workflows

### Workflow A — Destination-first (current, unchanged)
The existing Home flow, kept byte-for-byte:
- Display strip defaults to **All targeted**.
- Tap chips to narrow the target set (first tap from All **solos**; further taps toggle;
  untoggling the last reverts to All).
- Tap a **source** card → `routeSourceToTargets(value)` routes to the targeted displays,
  then `resetTargetDisplays()` snaps the grouping back to All. 10s quiet-period timer and
  `onMount` reset cover picked-but-never-routed sets.
- The source tap is both "what" and "commit."

### Workflow B — Source-first / "paint" (new)
- Tap a source card → it **arms** (persistent highlight). No routing yet. A **"Send to All"**
  button fades in.
- Tap a display chip → **immediately** routes the armed source to that display; the chip
  repaints from the real `Display{N}SourceFb` feedback. The source stays armed — the user
  keeps painting additional displays.
- Tap **"Send to All"** → routes the armed source to all four displays in one touch.
- **Persistence:** the armed source stays armed until the user taps a *different* source
  (which simply re-arms to it). There is **no idle timer** in this mode — switching source
  is the only "reset." Page state is left exactly as shown between actions.

A **toggle above the source row** flips A ⇄ B live.

## 2. State model (`router.ts` + `session.ts`)

- `homeRouteMode = writable<'destination' | 'source'>` — lives in / is mirrored by
  `session.ts` (like `userPoweredOn`) so it survives `goToPage()` round-trips; **defaults to
  `'destination'`** so nothing changes until the toggle is touched.
- Reuse the existing `armedSource` writable already in `router.ts` (proven by Advanced-Routing
  drag-drop). New helpers:
  - `armForPaint(sourceId)` — sets `armedSource`, **without** the 4s auto-disarm that
    `armChip()` applies (source mode wants persistence). Tapping a different source just calls
    this again with the new id.
  - `routeArmedToAll()` — loops `routeSource(armed, d)` over `ALL_DISPLAYS`.
- `routeSource(sourceId, displayId)` (already exists) is the per-chip paint primitive; it
  no-ops when the display already shows that source (read from feedback).
- In source mode the `targetDisplays` set is **unused** (left at All); destination mode is
  untouched and keeps owning it.
- Feedback-driven only — no optimistic mirrors. Chip/card state always derives from
  `Display{N}SourceFb` / sync feedbacks, never from local assumption.

## 3. The toggle (above the sources)

A 2-segment control replacing/wrapping the `— Choose your source —` eyebrow:
- Segments: **"Display → Source"** | **"Source → Display"**, each with a tiny order glyph.
- ≥44px hit height, theme custom-properties, amber active fill mirroring the app's accent.
- `role="group"` + per-segment `aria-pressed`; the active segment is signified by fill **and**
  label weight (never color alone).
- Flipping modes: cross-fades the caption/Send-to-All zone (≤220ms) and clears transient
  state of the mode being left (destination → reset target set; source → keep armedSource so
  a deliberate flip back resumes, but Send-to-All hides).

## 4. Source cards — behavior by mode

- *Destination mode:* unchanged. Tap routes to the current target set + resets.
- *Source mode:* tap calls `armForPaint(value)`. The armed card gets a **persistent** amber
  treatment (reuse the `.active` stripe + glow, but driven by `armedSource`, not D1 feedback).
  The existing per-card sync dot and "Control" flag (D1 authority) keep working in both modes.

## 5. Display chips — behavior by mode

- *Destination mode:* unchanged — chip = target toggle (orange "targeted" ring + check glyph),
  "Source goes to: …" caption.
- *Source mode:* chip = immediate route button. A chip whose live `sourceFb` already equals the
  armed source shows a **"has it"** treatment (edge-lit border + check) — feedback-driven, not
  optimistic. The target caption is replaced by an armed caption:
  *"Sending **Room PC** — tap displays, or Send to All."* When nothing is armed yet:
  *"Pick a source, then tap displays."*

## 6. "Send to All" button

- Rendered **only** in source mode **and** only when a source is armed (fades in/out).
- Placement: right of the source row / above the chip strip — a minimal-touch path to the
  common "everywhere" case.
- Prominent amber, ≥52px (mirrors the existing `.adv-float` styling), single tap →
  `routeArmedToAll()`. Stays armed afterward (persistence rule).

## 7. Animations / transitions (all approved)

- **Toggle flip:** cross-fade + ~8px slide of the caption / Send-to-All zone (≤220ms).
- **Arm:** armed source card lifts + persistent amber stripe (reuse `.active`, driven by
  `armedSource`).
- **Paint:** on chip tap, a brief source→chip "fly" accent plus the existing `route-flash`,
  then the chip settles into its fed-back source label.
- **Send to All:** staggered chip flashes, ≤300ms total.
- All guarded by `prefers-reduced-motion`; `:active` press states mandatory on every control
  (capacitive panels have no hover).

## 8. Scope guardrails & persona compliance

- **Pure panel-side.** No contract signal, no `.cce`, no Contract Editor build, no processor
  build. Only existing `Display{N}Source` set + `Display{N}SourceFb` feedback are used.
- **Workflow A is untouched** — same code path, same timers, same `resetTargetDisplays()`.
- Persona-compliant (Crestron UX Master + GUI Room-Map): ≥44px targets, theme custom-properties
  only (no hardcoded color/size), state never on color alone (shape/label paired), animations
  ≤300ms with reduced-motion guards, single-tap primary actions, feedback-driven state.

## 9. Architecture choice

**Approach 1 (chosen):** mode store + thin branching in `Home.svelte`, logic centralized in
`router.ts`, reusing `armedSource` + `routeSource`. Both workflows share the same source-card
and chip markup so the toggle is a true A/B on identical pixels; Workflow A carries zero risk.
(Rejected: extracting two sub-components — duplicates shared markup/CSS and churns the working
flow; a full state machine in `router.ts` — more abstraction than two modes warrant.)

## 10. Out of scope

- No change to Advanced Routing (`DisplayRouting.svelte`), D5 signage, USB host, or the footer.
- No persistence of the chosen mode beyond the session (resets to destination-first on reboot).
- No new contract work; live signal verification still happens via `deploy:both` on glass.

## 11. Open questions for on-glass iteration

- Exact toggle copy ("Display → Source" vs "Classic / Source-First") — tune on glass.
- Whether "Send to All" should also appear (greyed) before a source is armed, as an affordance
  hint — default is hidden-until-armed; revisit after first feel test.
