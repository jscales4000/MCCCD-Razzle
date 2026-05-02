# Drag-and-Drop Source Routing — Design Spec

**Date:** 2026-05-01
**Status:** Stage 1 (static mockup) approved for development
**Owner:** Jordan Scales

## Problem

The current MCCCD-AA140 Home page routes sources to displays via a per-display 4-button grid: each `DisplayTile` renders four buttons (Room PC, Ext PC, AirMedia, Laptop), and tapping one publishes an analog (1–4) on that display's source-set signal. With three displays, that's twelve source buttons on screen at once.

Two things this design wants to test:

1. **Does a single shared source palette feel cleaner than three duplicated grids?** Twelve buttons compress to four.
2. **Does drag-and-drop routing feel right on a wall-mounted capacitive touchscreen?** It's a richer metaphor — "this signal goes to that display" — but touch DnD has well-known pitfalls (accidental drags, finger occlusion, no hover state) and we don't yet know if it survives contact with a real panel.

## Approach: two-stage prototype

**Stage 1 (this spec):** Static HTML mockup at `mockups/11-drag-drop-router.html`. Plain HTML/CSS/JS. No Svelte, no CrComLib, no signals. Goal: nail the layout, gesture, and visual language before spending a session wiring it up. Survives or dies on gut feel.

**Stage 2 (deferred):** If Stage 1 survives, a separate spec promotes the design to a real Svelte branch wired to `SIGNALS.display{N}Source`, deployed to TS-1070 over `.ch5z`, and gut-checked on actual hardware. This document does not cover Stage 2.

## Scope

### In scope (Stage 1)

- New file `mockups/11-drag-drop-router.html`, sized 1280×800 to match the existing mockup gallery convention.
- Update `mockups/index.html` to add card #11 to the gallery.
- Left rail with 4 source chips (Room PC, Ext PC, AirMedia, Laptop), each with an icon and label.
- Three display tiles to the right of the rail, each acting as a drop target with a "landed chip" slot inside.
- Header and footer reproduced visually from the current Home page (not under test — recreated for layout fidelity, no behavior).
- Three input flows: long-press-arm-then-drag, tap-to-arm-then-tap-display, mixed.
- All visual states from the design (idle, armed, dragging, drop-valid, drop-hovering, drop animation, snap-back).
- CSS-driven animations under 280ms each.
- One line of in-rail help text: `SOURCES — long-press or tap to route`.

### Out of scope (Stage 1)

- Any Svelte, CrComLib, or signal wiring.
- Display-to-display drag (the chip inside a display tile is a status indicator, not a drag handle).
- Source disconnect (no way to clear a display's routing — dragging a different source replaces).
- Mirror-to-D3 and audio-output behavior changes (icon buttons exist visually on D1/D2 for layout fidelity, but are non-functional in this mockup).
- Multi-touch — second finger is ignored.
- Disabled-source state.
- Settings page, Cameras page, modals (shutdown / mic volume).
- TS-1070 1920×1200 layout polish — design targets 1280×800 and trusts the existing CH5 viewport scaler. 1920 polish is a Stage 2 concern.
- First-run tutorials, onboarding modals, or any animated tutorial overlay.

## Layout

Single-screen grid:

```
┌──────────────────────────────────────────────────────────┐
│ HEADER (92px) — eyebrow, room name, occupancy block,     │
│                  online pill, Cameras btn, Settings btn  │
├──────┬───────────────────────────────────────────────────┤
│      │                                                   │
│ RAIL │   DISPLAY 1     DISPLAY 2     DISPLAY 3          │
│ 96px │   (drop zone)   (drop zone)   (drop zone)        │
│      │                                                   │
│ chips│   each tile shows a landed source chip when      │
│ stack│   routed, or an empty placeholder when not       │
│      │                                                   │
├──────┴───────────────────────────────────────────────────┤
│ FOOTER (104px) — Power | Mics | Volume                   │
└──────────────────────────────────────────────────────────┘
```

**Source rail (left, 96px wide):**
- Header `SOURCES — long-press or tap to route` in muted small caps at the top.
- 4 stacked chips, ~88px tall, gap ~12px. Each chip: icon + label.
- Background slightly darker than panel surface to read as "supply" vs. "destinations".

**Display tile:**
- Header line: `● DISPLAY {N}` (power dot + label), matching current `DisplayTile` typography.
- Center: a slot. Either a "landed chip" (smaller version of the rail chip, ~70% scale, same icon/color) or an empty placeholder reading `— No source —` in muted text.
- Bottom-right: existing icon buttons rendered for layout fidelity (audio toggle on D1/D2, mirror-to-D3 on D1/D2). Non-functional in mockup.
- The whole tile is the drop target — finger doesn't need to land precisely on the slot.

**Header & footer:** visual reproduction of the current `Home.svelte` layout. Buttons render but do nothing.

## Interaction model

### State machine (per source chip)

```
IDLE ──tap──▶ ARMED ──tap on display tile──▶ ROUTED (drop animation)
  │             │                                       │
  │             └──tap chip again / 4s timeout──▶ IDLE
  │
  └──press 250ms──▶ DRAGGING ──finger over tile──▶ HOVERING(tile)
                       │                                │
                       │                                └──finger up──▶ ROUTED
                       │
                       └──finger up off any tile──▶ snap back to IDLE
```

### Three valid input flows

1. **Drag flow.** Finger lands on rail chip → 250ms hold → chip "lifts" (scales 1.05, gains drop shadow, rail slot shows 30% opacity ghost) → chip clone follows finger → over a display tile, that tile shows DROP-HOVERING state → lift finger over tile = route. Lift outside any tile = chip snaps back along a curved path.
2. **Tap-arm flow.** Tap chip without holding → chip enters ARMED state (cyan border + pulse + tooltip "Tap a display →") → tap any display tile = route, with the same drop animation as drag flow. Tap the chip again, tap any non-tile area, or wait 4s = disarm.
3. **Mixed.** Once ARMED, dragging works the same as DRAGGING. Tap-arm-then-drag = same outcome as drag from idle.

### Long-press threshold

250ms. Below the iOS/Android 500ms default — wall-mounted "always-ready" panels feel sluggish at 500ms. Drag-after-arm has zero hold delay (already armed).

### Cancellation

- During drag: lift outside any display tile → snap-back animation (~220ms ease-in-out), chip returns to rail slot, no "signal" sent.
- During armed state: tap chip again, tap anywhere outside the chip and outside any display tile (header, footer, rail blank space), or 4 seconds of idle → disarm with subtle fade.

### No-op cases

- Drop a source on the display where it's already routed → snap-back animation as if cancelled, no state change. While dragging, that tile does NOT show DROP-HOVERING — it remains in DROP-VALID with a lower-emphasis treatment, signaling "no point dropping here." (Implementation: check `tile.routedSource === draggingSource` to suppress hovering state.)

### Multi-touch

Ignored. Only the first finger down on a chip starts a gesture; second finger anywhere is dropped. Real-world reason: people lean on wall-mounted panels.

## Visual language

Reuse the existing palette from `MCCCD-AA140/src/global.css` and the mockups: cyan accent (`#38bdf8`), success green (`#22c55e`), panel/border tokens. No new colors.

### Source chip states (in rail)

- **IDLE** — `glass-card` style background, icon + label, soft border.
- **ARMED** — cyan border (1.5px), faint cyan inner glow, floating tooltip below: "Tap a display →". Subtle 1.5s pulse on the border.
- **DRAGGING (clone)** — clone follows finger, scaled to 1.08, drop shadow `0 12px 32px rgba(0,0,0,.5)`, slight rotation (+2°). Original rail slot shows a 30% opacity ghost outline.
- **DISABLED** — out of scope here; reserved for Stage 2 if a source ever reports unavailable.

### Display tile states

- **IDLE** — current `glass-card` look. If routed, the landed chip is visible inside (smaller scale, same icon/color). If unrouted, empty slot reads `— No source —` in muted text.
- **DROP-VALID** (during any chip's ARMED or DRAGGING state) — every display tile gets a 1.5px dashed cyan outline + faint cyan tint. Tells the user "these are the places you can drop."
- **DROP-HOVERING** (only the tile the finger is currently over during a drag) — solid cyan outline, brighter tint, 1.02 scale, centered hint label `Drop to route Room PC` (or whichever source).

### Drop animation (~280ms)

Three phases:
1. Chip clone snaps from finger position to the center of the target tile's slot — cubic-bezier ease-out, 180ms.
2. The newly-landed chip (whether replacing an existing chip or filling an empty `— No source —` placeholder) does a "thunk" — scale 1.0 → 1.06 → 1.0, 100ms. If a previous chip existed, it dissolves out (60ms fade) before the new one thunks. The placeholder text fades out simultaneously with the snap when filling an empty tile.
3. Tile's cyan border flashes once (150ms fade) to confirm.

### Snap-back animation (cancel)

Chip clone curves back to its rail slot along a quadratic bezier, 220ms ease-in-out. Opacity stays at 100%. Original rail-slot ghost dissolves back to full opacity as the clone arrives.

### Motion budget

All transitions ≤ 280ms. Wall-mounted touch panels need to feel immediate, not choreographed.

## Initial state (for the mockup demo)

- D1 routed to Room PC (chip landed in D1 slot).
- D2 routed to AirMedia (chip landed in D2 slot).
- D3 unrouted (empty placeholder).
- All 4 chips visible and grabbable in the rail.

This lets the demo showcase: drag onto an empty display, drag to replace an existing routing, drag the same source onto a second display (mirror-by-routing, since same source on two displays is allowed by the system).

## File deliverables

1. `mockups/11-drag-drop-router.html` — the new mockup.
2. `mockups/index.html` — append a card linking to mockup 11.

No other files are touched in Stage 1. Existing Svelte source under `MCCCD-AA140/src/` is read-only for this stage.

## Success criteria

Stage 1 is "successful" if, after building the mockup and clicking through it in a browser:

1. The 250ms long-press feels right — not so short that scrolls/brushes trigger drags, not so long that it feels sluggish.
2. The drop animation reads as "the signal landed" rather than "a button changed state."
3. Glance test: with the rail and three displays visible, can you tell at a glance what's playing where? (Today's tile-text label is fine but uniform; the landed-chip approach should be visibly faster to scan.)
4. Tap-to-route fallback feels equivalent in speed to today's per-display button grid.

If any of those fail, we either iterate on the mockup or kill the concept before promoting to Stage 2. The whole point of doing this in static HTML first is making that decision cheap.
