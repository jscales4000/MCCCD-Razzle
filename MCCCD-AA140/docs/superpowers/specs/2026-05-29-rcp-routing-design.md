# 2026-05-29 — Reflected Ceiling Plan Routing (Display Routing v2)

## Status
Mockup B (Live Map + Inline Popover) approved 2026-05-29. Three HTML mockups in
`docs/mockups/rcp-routing/` document the alternatives; this spec captures the
winning direction so implementation can proceed.

## Problem
The current `DisplayRouting.svelte` is a matrix UI: source list on the left,
3 display cells on the right. Functional, but it loses the *spatial* context of
where each display physically lives in AA140 — useful when a user wants to send
content to "the projector on the right" rather than "Display 2". The new view
replaces the matrix with a reflected ceiling plan of the room, with tappable
display markers and an inline source picker.

## Decision: Mockup B — Inline Popover

Why B over A (centered modal) or C (isometric):
- **B's persistent sidebar** mirrors live state so the user never has to open
  anything to know what's playing where. A modal hides the room while open.
- **The popover anchors to the tapped display**, keeping eye position locked.
  A modal forces a focus jump to the center; an isometric scene is visually
  rich but harder to reason about on a 10" panel.
- B reuses our existing dark/orange aesthetic without exotic 3D transforms,
  so it ports cleanly to TSW-1070's webview.

## Layout (1280×800, native panel)

```
┌────────────────────────────────────────────────────────────────────────────┐
│ ◂ Home │ AA140 │ Display Routing · Live Map  [Manual|Mirror|Extend] [Auto] │ ← 60px header
├──────────────────────────────────────────────────────┬─────────────────────┤
│                                                      │                     │
│        ┌───────────────────────────────┐             │ DISPLAY STATUS      │
│        │  ▭ D1 Front Left  ▭ D2 Right  │             │ ┌─────────────────┐ │
│        │  ◇ pj1            ◇ pj2       │             │ │ D1 Front Left   │ │
│        │     mxa     mxa               │             │ │ ▸ Room PC       │ │
│        │    ⟍ ⟋  TABLE  ⟍ ⟋            │             │ └─────────────────┘ │
│        │     [ — D3 Newline — ]         │             │ ... D2, D3 rows    │
│        └───────────────────────────────┘             │                     │
│   (popover anchors here when display tapped)         │ AUDIO FOLLOWS: D1   │
│                                                      │                     │
├──────────────────────────────────────────────────────┴─────────────────────┤
│ QUICK ROUTES │ Room PC → All │ AirMedia → All │ Mirror All │ Clear All    │ ← 76px footer
└────────────────────────────────────────────────────────────────────────────┘
```

## Components

### `pages/DisplayRouting.svelte` (rewrite)
Owner of the page layout, header, footer, and the inline-popover state.
Holds one piece of local UI state: `openDisplay: DisplayId | null`.

### `components/routing/RoomPlan.svelte` (new)
The reflected-ceiling SVG/HTML composite. Pure presentation: takes
`displays`, `onDisplayTap`, and `openDisplay` as props. Renders:
- Room outline (rounded rect)
- Wall speakers (4× blue bars, top/bottom)
- Ceiling projectors (2× diamond markers, front quadrant)
- Mic arrays (2× MXA920W circles, mid-front)
- Cameras (2× small circles, top corners)
- Conference table (dashed ellipse, center)
- 3× `<DisplayMarker>` instances

### `components/routing/DisplayMarker.svelte` (new)
A tappable rectangle that represents one display in the plan.
Props: `displayId`, `label`, `activeSource`, `powerOn`, `selected`.
Visual: orange tint when sourced + powered, gray when off, accent glow when
selected (popover open). Click → emits `displayId` up to the page.

### `components/routing/SourcePopover.svelte` (new)
Anchored popover that drops below the tapped display marker.
Props: `displayId`, `activeSource`, `onSelectSource`, `onMirror`, `onClear`,
`onClose`.
Renders 4 source rows (Room PC / Ext PC / AirMedia / Laptop), Match-D1 and
Clear quick actions, close button. Uses the same arrow-pointing-up styling
from the mockup. Clicking a row routes immediately and closes the popover.

### `components/routing/DisplayStatusCard.svelte` (new)
Sidebar row showing one display's live state (ID badge, name, power, current
source, spec line). Tapping the row is a shortcut to open the popover for
that display.

## Data flow

```
TouchUp on DisplayMarker(d2)
  → DisplayRouting.svelte sets openDisplay = 'd2'
  → SourcePopover mounts, anchored to d2

TouchUp on SourcePopover source row "AirMedia"
  → SourcePopover calls onSelectSource('airMedia')
  → DisplayRouting calls routeSource('airMedia', 'd2')  ← existing fn
  → publishAnalog(SIGNALS.display2Source, 3)
  → SIMPL echoes back to display2SourceFb
  → DisplayMarker.activeSource updates reactively
  → openDisplay = null (popover closes)
```

Existing pieces we reuse without change:
- `lib/stores/router.ts` — `routeSource`, `SOURCES`, `SourceId`, `DisplayId`,
  `currentRouting`
- `lib/stores/signals.ts` — `display{1,2,3}SourceFb`, `display{1,2,3}PowerFb`,
  `routingModeFb`, `autoRouteEnableFb`, `audioOutputSelectFb`
- `lib/contract.ts` — `SIGNALS.display{N}Source`, `mirrorAllSame`,
  `d{1,2}MirrorToD3`, `routingMode`, `autoRouteEnable`

What we drop:
- Drag-drop chip flow on this page. The new model is tap-to-open-popover;
  drag-drop adds complexity without payoff once the popover exists. The
  drag rail (`SourceListItem`, `DragCloneOverlay`) stays in the codebase but
  is not used by this page. Other pages may still use it.
- `components/routing/DisplayCell.svelte` and `SourceListItem.svelte` — no
  longer referenced by `DisplayRouting.svelte` but kept on disk in case
  another mockup wants them.

## Equipment-accurate placement

From the BOM (reference_mcccd_aa140_equipment.md):
- 2× Sony VPL projectors → diamond markers at ~30%/70% across, ~22% from top
- 1× Newline 86" interactive → D3 marker at rear-center
- 2× MXA920W ceiling arrays → circle markers at ~25%/75% across, mid-front
- 2× cameras (12x + 20x) → small circles top-front
- Wall speakers (OFE) → 4× blue bars, front + rear walls
- D1/D2 projection surfaces → orange rectangles on front wall edge

## Edge cases

- **Display powered off**: marker renders gray, source line shows "—", but
  still tappable (so the user can route ahead of turning the system on).
- **No source routed**: marker shows "No Source" + dim border; popover shows
  no active row.
- **Two displays already on the same source**: each marker shows its source
  independently; no special "same-as" indicator (keep it simple v1).
- **Popover opens with offscreen risk**: D3 sits at the bottom of the plan;
  its popover should anchor *above* rather than below. Detect with marker
  position vs container height — if marker.bottom > containerHeight * 0.55,
  flip the popover above (arrow on bottom edge).
- **Tap outside popover**: closes it. Tap on another marker: closes current,
  opens new one for the tapped display.
- **Mode is not Manual**: still allow per-display routing, but if mode = Mirror
  or Extend the SIMPL side may override. The UI surfaces the mode chip so the
  user knows what's in effect.

## Out of scope

- Settings page changes (separate effort).
- New SIMPL signals — everything wires to existing signals.
- Drag-and-drop chip routing (intentionally dropped on this page).
- Animation choreography beyond a fade-in on the popover.

## Verification

1. `npm run check` — zero TS/Svelte errors.
2. `npm run build` — Vite build succeeds, bundle size sane.
3. Browser dev: `npm run dev`, tap each display, verify popover anchors
   correctly and routes via publishAnalog.
4. Deploy to TSW-1070 via `npm run deploy:wall` (192.168.2.78). Tap each
   marker on the physical panel; verify SIMPL Display{N}Source updates.
5. Compare layout proportions against the original schematic image — mics
   visually fall under the ceiling, displays sit on the walls.

## Files touched

- New: `src/components/routing/RoomPlan.svelte`
- New: `src/components/routing/DisplayMarker.svelte`
- New: `src/components/routing/SourcePopover.svelte`
- New: `src/components/routing/DisplayStatusCard.svelte`
- Modified: `src/pages/DisplayRouting.svelte` (full rewrite)
- Unchanged (kept on disk for now): `DisplayCell.svelte`, `SourceListItem.svelte`
