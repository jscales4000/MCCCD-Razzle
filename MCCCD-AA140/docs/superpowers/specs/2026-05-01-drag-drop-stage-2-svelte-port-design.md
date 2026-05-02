# Drag-and-Drop Source Routing — Stage 2 (Svelte Port) Design Spec

**Date:** 2026-05-01
**Status:** Approved for development (Stage 2)
**Owner:** Jordan Scales
**Stage 1 spec:** [`2026-05-01-drag-drop-source-routing-design.md`](2026-05-01-drag-drop-source-routing-design.md)
**Mockup:** `mockups/18-drag-drop-router.html` (Stage 1 deliverable)

## Problem

Stage 1 built a static HTML mockup at `mockups/18-drag-drop-router.html` exploring drag-and-drop source routing as a replacement for the current per-display 4-button grids on Home. Stage 2 ports that mockup into the live Svelte panel, wires it to real CrComLib signals, and deploys to TS-1070 — so the gut-check happens on the actual capacitive touchscreen, not in a browser with a mouse.

All UX decisions were settled in Stage 1: left-rail palette, three display drop zones, long-press 250ms gesture with tap-to-arm fallback, landed-chip metaphor, three-phase drop animation. Stage 2 does not revisit any of those.

## Approach

**Replace, don't toggle.** The Home page's per-display source-button grids go away entirely. The drag-drop view is the only routing UX. No feature flag, no settings preference, no fallback. Rationale: the gut-check requires living with the metaphor under real hardware constraints; a toggle dilutes the test, and `git revert` is one command away if it fails.

**Source of truth for routing shifts from mockup.** In the mockup, `appState.routing` was authoritative. In the Svelte version, the **feedback signal stores (`display{N}SourceFb`) are authoritative** — the landed chip reflects feedback directly, exactly as today's button grid does. The new router store tracks only drag/arm UI state; no routing state is duplicated.

**Branch & deploy cadence.** Continue on the existing `feat/drag-drop-router-mockup` branch — Stage 2 work commits on top of the Stage 1 mockup commits. One build + deploy at the end of all porting tasks (not per-task), to avoid repeated PROJECTLOAD restarts.

## Scope

### In scope (Stage 2)

- New file `MCCCD-AA140/src/lib/stores/router.ts` — Svelte `writable()` stores for the reactive UI state (`armedSource`, `draggingSource`, `cloneCoords`), module-level `let` declarations for the imperative state (`suppressNextClick`, `pressTimerId`, `pressOriginEl`, `pressOriginX/Y`, `lastPointerX/Y`, `armedTimeoutId`), plus action functions (`armChip`, `disarm`, `startDrag`, `endDrag`, `moveCloneTo`, `tileUnderPointer`, `updateHover`, `routeSource`, `shouldSuppressClick`, `onPointerMove`, `onPointerUp`, `onPointerCancel`). Convention matches the existing `signals.ts` and `page.ts` (writable stores + module-private `let`); does NOT use Svelte 5 runes (avoid introducing a new pattern incidentally).
- New file `MCCCD-AA140/src/components/SourceRail.svelte` — 96px left rail with 4 source chips (Room PC, Ext PC, AirMedia, Laptop), each owning its `pointerdown` and `click` handlers.
- New file `MCCCD-AA140/src/components/DragCloneOverlay.svelte` — fixed-position chip clone rendered at the App root (outside `.panel-stage` to avoid the panel-scale transform). Reads `draggingSource` from the router store and follows the pointer.
- Modified `MCCCD-AA140/src/components/DisplayTile.svelte` — replace the 4-button `.source-grid` with a drop-zone slot showing a "landed chip" reflecting `$display{N}SourceFb`, or "— No source —" placeholder when feedback is 0. Keep the existing audio-toggle and mirror-to-D3 icon buttons. Add DROP-VALID / DROP-HOVERING / DROP-NOOP classes driven by router state and the tile's `data-display` ID. Add tap-handler that routes when a chip is armed.
- Modified `MCCCD-AA140/src/pages/Home.svelte` — grid layout grows a left column. New `grid-template-areas: "header header" "rail tiles" "footer footer"` with the existing 92px / 1fr / 104px row sizing and a new 96px / 1fr column sizing. Mount `<SourceRail />` in the rail cell. The three `<DisplayTile />` instances stay where they are. Remove the now-redundant `sourceSetSignal` prop wiring (DisplayTile no longer publishes; the router store does).
- Modified `MCCCD-AA140/src/App.svelte` — mount `<DragCloneOverlay />` at root level, **outside** `.panel-stage`, so the clone uses viewport coordinates and isn't double-scaled.
- Modified `MCCCD-AA140/src/global.css` — add three globally-scoped keyframes: `chip-arm-pulse`, `thunk`, `tile-flash`. Svelte's per-component CSS scoping doesn't share keyframes across components, and these are referenced by SourceRail (chip-arm-pulse), DisplayTile (thunk on landed chip, tile-flash on tile), and DragCloneOverlay (snap transitions). Global is the right home.
- One end-of-stage build + deploy: `cd MCCCD-AA140 && npm run build` then `python scripts/deploy.py`.

### Out of scope (Stage 2)

- Any changes to the camera page, settings page, or non-Home routing.
- Mirror-to-D3 and audio-toggle behavior changes — icon buttons stay as today.
- Multi-source-on-multi-display constraints (the system already allows this; no change).
- Source-disconnect (no way to clear a routing — drag a different source to replace).
- Multi-touch handling beyond the single-active-pointer guard (carry over the mockup's `if (draggingSource || pressTimerId) return;` defense).
- Performance benchmarks or telemetry.
- Onboarding or tutorial overlay.
- Any modifications to existing modals (`ConfirmShutdownModal`, `MicVolumeModal`).
- Settings page UI — no toggle, no preference, no Stage-2 escape hatch.
- TSW-770 (1280×800 native) layout polish — design targets TS-1070 (1920×1200, scale 1.5×). TSW-770 should still render correctly via the existing panel-scale system but is not the gut-check target.
- The Stage 1 mockup file itself (`mockups/18-drag-drop-router.html`) is unchanged in Stage 2 — it stays as a reference artifact.

## Architecture

### State management

`MCCCD-AA140/src/lib/stores/router.ts` exports `writable()` stores for the reactive UI state, plus action functions. Pattern matches the existing `signals.ts` (writable stores) and `page.ts` (writable + helper) convention:

```ts
import { writable, get } from 'svelte/store';
import { publishAnalog } from '../CrComLib';
import { SIGNALS } from '../contract';
import {
  display1SourceFb, display2SourceFb, display3SourceFb,
} from './signals';

export type SourceId = 'roomPc' | 'extPc' | 'airMedia' | 'laptop';
export type DisplayId = 'd1' | 'd2' | 'd3';

export const SOURCES: Record<SourceId, { label: string; value: 1 | 2 | 3 | 4 }> = {
  roomPc:   { label: 'Room PC',  value: 1 },
  extPc:    { label: 'Ext PC',   value: 2 },
  airMedia: { label: 'AirMedia', value: 3 },
  laptop:   { label: 'Laptop',   value: 4 },
};

// Reactive UI state (rendered components subscribe to these)
export const armedSource = writable<SourceId | null>(null);
export const draggingSource = writable<SourceId | null>(null);
export const cloneCoords = writable<{ x: number; y: number }>({ x: 0, y: 0 });

// Imperative state (module-private; never subscribed; used only inside actions)
let suppressNextClick = false;
let armedTimeoutId: ReturnType<typeof setTimeout> | null = null;
let pressTimerId: ReturnType<typeof setTimeout> | null = null;
let pressOriginEl: HTMLElement | null = null;
let pressOriginX = 0, pressOriginY = 0;
let lastPointerX = 0, lastPointerY = 0;
let dragCloneEl: HTMLElement | null = null;  // set by DragCloneOverlay's onMount

export function registerCloneEl(el: HTMLElement | null): void;

// Actions (exported)
export function armChip(sourceId: SourceId): void;
export function disarm(): void;
export function startDrag(sourceId: SourceId, originEl: HTMLElement, x: number, y: number): void;
export function endDrag(x: number, y: number): void;
export function onPointerMove(e: PointerEvent): void;
export function onPointerUp(e: PointerEvent): void;
export function onPointerCancel(e: PointerEvent): void;
export function shouldSuppressClick(): boolean;
export function routeSource(sourceId: SourceId, displayId: DisplayId): void;
export function tileUnderPointer(x: number, y: number): HTMLElement | null;

// Helper to read the current routing of a display (returns SourceId or null)
export function currentRouting(displayId: DisplayId): SourceId | null;
```

The action functions implement the same behavior as the mockup's plain-JS equivalents, with three adaptations:

1. **`routeSource(sourceId, displayId)`** publishes the analog signal: `publishAnalog(SIGNALS[\`display${N}Source\`], SOURCES[sourceId].value)`. It does NOT track local routing state — the feedback store re-renders the landed chip when the processor echoes back.
2. **`tileUnderPointer(x, y)`** uses `document.elementFromPoint(x, y)?.closest('.tile')` exactly as the mockup did. Works the same on the panel.
3. **`currentRouting(displayId)`** maps `display{N}SourceFb` analog feedback (1–4) back to a `SourceId`. Used by `updateHover` for DROP-NOOP detection (replacing the mockup's `appState.routing[displayId]` lookup).

`armedSource`, `draggingSource`, and `cloneCoords` are the only reactive bits. The dragging clone DOM node is owned by `DragCloneOverlay.svelte` itself (not stored in the router) — the overlay reads `draggingSource` and `cloneCoords` and renders accordingly. Press timing/origin coords are module-level `let` because they don't drive reactive rendering.

### Component breakdown

**`SourceRail.svelte`:**
- Renders a vertical column of 4 chip buttons.
- Each chip has `data-source` and the same SVG markup used in the mockup.
- Attaches `pointerdown` and `click` handlers on each chip; both delegate to the router store actions.
- The "ARMED" visual state is driven by `class:chip-armed={$armedSource === sourceId}` (subscription to the writable store).
- Width: 96px. Height: fills available vertical space. Background: subtly darker than panel surface.

**`DragCloneOverlay.svelte`:**
- Renders ONLY when `$draggingSource !== null` (Svelte `{#if}` against the store subscription).
- Fixed-position container at z-index 1000.
- During the drag phase: reads viewport coordinates from `$cloneCoords` (writable updated by `onPointerMove`) and applies them via inline `style="transform: translate(...)"`. The ` translate(x-40, y-44) scale(1.08) rotate(2deg)` formula matches the mockup.
- During the drop / snap-back phases: `endDrag` directly manipulates the clone DOM element (set `snapping` class, set inline transform/opacity to the target). To enable this, the overlay registers its DOM element with the router store via a setter (`registerCloneEl(el)` called in the overlay's `onMount`) so `endDrag` can grab the reference.
- Renders the same chip markup as `SourceRail`, looked up from `SOURCES[$draggingSource]`.
- Lives at the App.svelte top level (outside `.panel-stage`) so it doesn't inherit the panel-scale transform; its CSS sizes are in viewport pixels and scale up via a CSS variable when needed (see Coordinate handling below).

**`DisplayTile.svelte` (modified):**
- Existing prop signature mostly preserved: `label`, `activeSourceFb`, `powerOn`, `audioActive`, `onAudioToggle`, `onMirrorToD3`. The `sourceSetSignal` prop is removed (no longer needed; routing flows through the rail).
- A new `displayId` prop (`'d1' | 'd2' | 'd3'`) replaces the implicit identification by `sourceSetSignal`.
- Internally, the existing `.source-grid` markup is replaced with a `.tile-slot` element matching the mockup. The slot contains either a `<div class="landed-chip">` (when `activeSourceFb > 0`) or `<span class="slot-empty">— No source —</span>` (when `activeSourceFb === 0`).
- The whole tile (or specifically the slot) is the drop target. A click handler on the tile calls `routeSource(routerState.armedSource, displayId)` if a chip is armed.
- Reactive class bindings: `class:drop-hovering={...}`, `class:drop-noop={...}`, driven by router state plus the tile's hover detection.
- Tile DOM gets `data-display={displayId}` so `tileUnderPointer` can find it.
- The audio-toggle and mirror-to-D3 icon buttons remain at top-right of the tile, unchanged.

**`Home.svelte` (modified):**
- Grid layout updated:
  ```
  grid-template-rows: 92px 1fr 104px;
  grid-template-columns: 96px 1fr;
  grid-template-areas:
    "header header"
    "rail   tiles"
    "footer footer";
  ```
- `<SourceRail />` in the rail area.
- Existing `<DisplayTile />` instances in the tiles area, with the prop signature simplified (`displayId` added; `sourceSetSignal` removed).
- Header and footer are unchanged.

**`App.svelte` (modified):**
- One additional component mounted: `<DragCloneOverlay />`, rendered at the root level alongside `<div id="app">` content. Crucially, it's NOT inside `.panel-stage`, so the panel's scale transform doesn't apply.

### Coordinate handling (the panel-scale subtlety)

The panel renders at logical 1280×800 inside `.panel-stage`, which is then `transform: scale(var(--panel-scale))`. On TS-1070 (1920×1200), `--panel-scale: 1.5`. Pointer events fire with viewport coordinates.

`DragCloneOverlay` is rendered OUTSIDE `.panel-stage`, so its CSS pixels are viewport pixels. The clone's intrinsic size (80×88 logical pixels in the mockup) needs to match the rail chip's on-screen size. Two ways:

1. **Make the clone's box match the rail chip's on-screen size.** Apply `--panel-scale` to the clone's width/height: `width: calc(80px * var(--panel-scale))`. Then position the clone using raw `e.clientX/Y` from pointer events; the clone is "in viewport space."

2. **Make the clone's box match logical (1280×800) coordinates and re-scale.** Apply `transform: ... scale(var(--panel-scale))` on top of the position transform.

Approach #1 is simpler — the clone's geometry is direct viewport pixels and pointer coords are direct viewport pixels. Use this. The `--panel-scale` CSS variable is already on `:root`.

`tileUnderPointer(e.clientX, e.clientY)` works regardless because `document.elementFromPoint` operates on viewport coords and returns the element rendered there, which IS the (scaled) tile.

The "snap-back to origin chip's position" animation similarly reads `originChip.getBoundingClientRect()` (viewport coords) and translates the clone to that viewport position — it ends up landing visually on the chip regardless of scale.

### Animation strategy

Three keyframes live in `MCCCD-AA140/src/global.css`:

```css
@keyframes chip-arm-pulse {
  0%, 100% { box-shadow: 0 0 0 1.5px var(--color-accent-soft), 0 0 18px var(--color-accent-soft); }
  50%      { box-shadow: 0 0 0 1.5px var(--color-accent),       0 0 24px rgba(56,189,248,.35); }
}

@keyframes thunk {
  0%   { transform: scale(1.0); }
  40%  { transform: scale(1.06); }
  100% { transform: scale(1.0); }
}

@keyframes tile-flash {
  0%   { box-shadow: 0 0 0 0px var(--color-accent-soft); border-color: var(--color-accent); }
  100% { box-shadow: 0 0 0 6px transparent;              border-color: var(--color-border); }
}
```

The class-toggle pattern (remove → `void el.offsetWidth` reflow → add) is replicated inside `endDrag` to reliably retrigger animations.

The CSS token names map directly: mockup's `--accent` → `--color-accent`, `--accent-soft` → `--color-accent-soft`, `--border` → `--color-border`. Tokens already defined in global.css.

### Pointer-event lifecycle (carried forward from Stage 1 fixes)

The Stage 1 mockup absorbed three real bugs that this port must preserve:

1. **Multi-touch guard** — `pointerdown` early-returns if `draggingSource || pressTimerId` is truthy. Prevents a second pointer from clobbering an in-flight drag.
2. **Listener leak fix** — `pointerup` and `pointercancel` listeners are explicitly removed via a shared `detachPointerListeners()` helper, NOT via `{ once: true }`. Listeners are added on document on `pointerdown` and removed on either terminal event.
3. **`pointercancel` aborts cleanly** — separate `onPointerCancel` handler calls `endDrag(-1, -1)` so `tileUnderPointer` returns null and snap-back fires (no accidental drop commit).

These three behaviors are part of `router.svelte.ts`'s contract and must NOT be removed during the port.

## Build & Deploy

After all implementation tasks are complete and committed on `feat/drag-drop-router-mockup`:

```
cd MCCCD-AA140
npm run deploy:tabletop
```

This npm script (defined in `package.json`) runs `build.mjs`, then `ch5-cli archive` to produce `output/MCCCD-AA140.ch5z`, then `cross-env PANEL_HOST=192.168.2.53 python scripts/deploy.py` to SFTP + PROJECTLOAD to the TS-1070 tabletop panel.

Pre-deploy gate: also run `npm run check` (svelte-check) before `npm run deploy:tabletop` to catch type errors that would otherwise blow up at build time.

If `npm run check` fails, fix the type errors before deploy. If the build fails, fix the build error before deploy. If the deploy fails (network, auth), surface the error and ask before retrying — don't loop.

## Success criteria

Stage 2 is "successful" when:

1. The build completes without errors (`npm run build` exits 0).
2. The .ch5z deploys to TS-1070 successfully (deploy.py exits 0, panel UI restarts).
3. On the panel, the new Home page renders: source rail on the left with 4 chips, three display tiles in the main area, header/footer unchanged.
4. The current routing on each display matches what `display{N}SourceFb` reports — e.g., if D1 is on Room PC, the Room PC chip is "landed" inside D1's tile.
5. Tapping a chip arms it (visible cyan pulse). Tapping a display tile while armed routes successfully — the SIMPL# program receives the `display{N}Source` analog and the feedback updates the landed chip.
6. Long-press a chip ~250ms initiates a drag. Drag clone follows the finger. Releasing on a tile routes (with the three-phase drop animation). Releasing off any tile snap-backs (with the curve + fade).
7. The five disarm paths from Stage 1 still work: tap chip again, tap a tile, tap outside, 4-second timeout, drag-then-release-on-origin.
8. Settings, Cameras, Power, Mics, Volume — all the non-routing controls — still work unchanged.

A subjective gut-check on the panel decides whether the metaphor is good enough to keep. If yes: leave it merged. If no: `git revert` the Stage 2 commits (keeping the Stage 1 mockup as a reference) and rebuild + redeploy. Stage 2 outcome captured under a `## Stage 2 Outcome` heading at the bottom of this spec.
