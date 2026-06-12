# Home Display Select + Control-Source Flag + Realistic Room Map — Design

**Date:** 2026-06-10
**Scope:** Panel-side only (Svelte). No contract changes — every feature rides existing signals.
**Pages touched:** `Home.svelte`, `DisplayRouting.svelte` (via `RoomPlan.svelte`), `lib/stores/router.ts`.

## 1. Home — per-display select buttons (route targets)

A new **display strip** sits below the hero source row: four chips, one per display
(D1 Front Left, D2 Front Right, D3 Rear Newline, D4 Podium). Each chip shows live
state from existing feedbacks: current routed source (`Display{N}SourceFb`, short
label + icon), power dot (`Display{N}PowerFb`), and a target ring when selected.

**Interaction model (target-toggle):**
- Default: **all four targeted** — tapping a source routes everywhere (the historical
  route-everywhere behavior). Zero behavior change for the default path.
- Tapping a chip while ALL are targeted **solos** that display (intent: "just this
  one"). Further taps toggle membership. Untoggling the last chip reverts to All.
- **Routing clears the grouping** (revised 2026-06-11 per user direction): after a
  source tap routes to the narrowed set, the set resets to All — the intended loop
  is pick displays → tap source → selection clears → pick again. A 10s quiet-period
  timer still reverts a set that was picked but never routed, and Home's `onMount`
  resets it, so a solo left behind can never trap the next presenter.
- A live caption directly above the strip states the destination in words ("Source
  goes to: All Displays · tap a display to limit" / "D2 + D4") — feedback is
  mandatory, never color alone; targeted chips also carry a check glyph.
- The tapped source card flashes (≤300ms) on every route so the action has visible
  result even when D1 (which drives the persistent active treatment) isn't targeted.

State lives in `router.ts`: `targetDisplays` writable store (new Set each mutation),
`toggleTargetDisplay()`, `routeSourceToTargets(value)`. Chips are ≥56px tall
(persona minimum 44px), 8px+ gaps, single-tap only.

## 2. Home — "Control Source" flag

The hero card whose `value` matches `$display1SourceFb` carries a labeled flag
(`⚑ CONTROL`), because D1's routed source is the room authority — program audio
follows D1 (and BYOD USB follows the active source via USB-SW-400). The flag is a
text badge (top-left, opposite the sync dot), shown in addition to the existing
orange active treatment so state is never conveyed by color alone. When displays
diverge (per-display routing used), the flag stays truthful: it tracks D1 feedback,
and the card highlight (`.active`) keeps tracking the same signal as today.

## 3. DisplayRouting — realistic, touchable room map

`RoomPlan.svelte` is upgraded from abstract rectangles to an architectural top-down
plan, keeping the exact same component contract (props, `onMarkerTap`, marker DOM/
`data-display` hooks the popover + sidebar rely on):

- **Touch layer unchanged:** `DisplayMarker` buttons remain HTML buttons ≥44px,
  absolutely positioned; popover anchoring and `.marker[data-display]` lookups work
  as before.
- **Scene realism (SVG underlay, `aria-hidden`, `pointer-events: none`):** double-line
  walls with thickness, door + swing arc (rear-right), conference table with chairs,
  podium outline, carpet/floor grid, projector throw cones from ceiling projectors to
  the D1/D2 front-wall screens (Sony VPL 100" surfaces drawn as screen bars), wall
  speakers, Cam1/Cam2, two MXA ceiling mic circles. Elements use theme custom
  properties; no hardcoded palette beyond existing accent rgba patterns.
- **Orientation preserved:** front of room at the BOTTOM (matches 2026-05-29
  reference image and existing marker positions).
- D5 (outside signage) is **not** added to the map — it is not in the d1–d4 routing
  model and is tech-gated elsewhere.

## Error handling / edge cases

- Empty target set is impossible (last-untoggle reverts to All).
- Feedback-driven rendering throughout — no optimistic mirrors (per retired-workaround
  rule).
- `prefers-reduced-motion` honored for all new animation.

## Testing

- `npm run check` (svelte-check) clean for touched files.
- Adversarial review pass: parallel UX-persona critique + CH5/Svelte-5 code review
  agents; findings triaged and fixed before deploy.
- `npm run deploy:both` after the change (both panels always get pushed); visual
  verification on TS-1070/TSW-1070 is the user's review gate.
