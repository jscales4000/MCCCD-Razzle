# Session Handoff — Drag-and-Drop Source Routing — 2026-05-02

**Date:** 2026-05-02
**Driver:** Jordan Scales
**AI:** Claude Opus 4.7 (1M context)
**Branch:** `feat/drag-drop-router-mockup` (off `main`)
**Final commit:** `6f333a7` — `fix(stage-2): tune drag for touch panel feel`
**Panel state:** ✅ Stage 2 LIVE on TS-1070 @ `192.168.1.175`, drag-drop functional but tuning in progress

---

## TL;DR

Built a drag-and-drop source-routing UX in two stages:

- **Stage 1** — static HTML mockup at `mockups/18-drag-drop-router.html` (gut-check; validated)
- **Stage 2** — Svelte port wired to `display{N}Source` analog signals; replaces the 12-button per-display source grid with a left-rail palette + 3 display drop zones; deployed to TS-1070 and confirmed working

User reports the metaphor works on hardware but more tuning is needed. **Open task: drag-drop polish** (long-press reliability, hit zones, animation feel).

Theme alignment to Mockup 11 (MCCCD Orange ★) is the next track — was queued before user asked to update FRED. FRED MCP was offline so I logged here instead.

---

## Spec & Plan documents

- **Stage 1 spec:** `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md`
- **Stage 1 plan:** `MCCCD-AA140/docs/superpowers/plans/2026-05-01-drag-drop-source-routing-plan.md`
- **Stage 2 spec:** `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md`
- Stage 2 had no separate plan doc — implementation was driven directly from the spec via subagents

---

## Files added or modified for drag-drop

### Stage 1 (mockup only — reference artifact)

```
A  mockups/18-drag-drop-router.html       665 lines, single-file mockup
M  mockups/index.html                     gallery card for Mockup 18
```

### Stage 2 (Svelte port — live UX)

```
A  MCCCD-AA140/src/lib/stores/router.ts          state machine + actions for drag/arm
A  MCCCD-AA140/src/components/SourceRail.svelte  left rail with 4 chips
A  MCCCD-AA140/src/components/DragCloneOverlay.svelte   chip clone overlay (App-root)
M  MCCCD-AA140/src/components/DisplayTile.svelte challenge: source-grid → drop zone
M  MCCCD-AA140/src/pages/Home.svelte             grid layout: rail + display row
M  MCCCD-AA140/src/App.svelte                    mount DragCloneOverlay at root
M  MCCCD-AA140/src/global.css                    3 keyframes (chip-arm-pulse, thunk, tile-flash)
```

Commit list on the branch:

```
6f333a7 fix(stage-2): tune drag for touch panel feel
faed4c5 fix(stage-2): add keyboard handler to tile for a11y
09c42de feat(stage-2): mount DragCloneOverlay at App root
84335af feat(stage-2): wire SourceRail into Home grid layout
8a5fbe5 feat(stage-2): convert DisplayTile to drop zone with landed chip
b489053 feat(stage-2): add DragCloneOverlay component (chip clone overlay)
d294db9 feat(stage-2): add SourceRail component (left-rail with 4 chips)
8206739 feat(stage-2): add drag-drop keyframes (arm-pulse, thunk, tile-flash)
df6da79 feat(stage-2): add router store for drag-drop UI state
e484e0c docs(spec): drag-drop Stage 2 — Svelte port to TS-1070
c04a812 fix(mockup-18): update title tag + gallery subtitle to slot 18
6a17cfb fix(mockup): rename drag-drop router to slot 18 (slot 11 taken)
7fdfa6c feat(mockup-11): add gallery card linking to drag-drop router
8093145 fix(mockup-11): snap-back fade + null-safe originChip
3531578 feat(mockup-11): drop animation + snap-back curves
281aafd fix(mockup-11): listener leak, multi-touch, pointercancel
a215886 feat(mockup-11): long-press to drag with hover/no-op states
95fb07e feat(mockup-11): disarm on tap outside chip/tile
d4c27f1 feat(mockup-11): tap-to-arm and tap-to-route flow
ff709f1 feat(mockup-11): static layout (header, rail chips, display tiles, footer)
f4f1df7 feat(mockup-11): scaffold drag-drop router skeleton
e484e0c docs(spec): drag-drop Stage 2 — Svelte port to TS-1070
b033393 docs(spec): drag-and-drop source routing mockup (Stage 1)
```

---

## Architecture (Stage 2)

**State management** — `router.ts` exposes three writable stores (`armedSource`, `draggingSource`, `cloneCoords`) and ~10 action functions. Pattern matches the existing `signals.ts` / `page.ts` (NOT Svelte 5 runes — deliberate to avoid introducing a new state idiom incidentally).

**Source of truth for routing** — the existing `display{N}SourceFb` feedback stores. The router does NOT track local routing state. On drop, it calls `publishAnalog(SIGNALS[\`display${N}Source\`], sourceValue)`; the processor echoes back via the feedback signal and the landed chip re-renders reactively.

**Chip clone rendering** — `DragCloneOverlay.svelte` is mounted at the App root, OUTSIDE `.panel-stage`, so it doesn't inherit the panel's scale transform. The clone uses viewport coordinates directly. To match the rail chip's on-screen size, the clone's `transform: scale(1.08)` is multiplied by `var(--panel-scale)` (1.5× on TS-1070).

**Pointer event lifecycle** — three real bugs were absorbed across two fix cycles:
1. Listener leak — `{once: true}` only removes the listener that fires; sibling stays armed. Fixed via explicit `detachPointerListeners()` helper.
2. Multi-touch — guard `if (draggingSource || pressTimerId) return;` at top of pointerdown.
3. Pointercancel committing drop — separate `onPointerCancel` calls `endDrag(-1, -1)` to force snap-back via off-tile coords.

**Touch-panel hardening** (added in commit `6f333a7`):
- `setPointerCapture(e.pointerId)` on chip pointerdown — pins all pointer events to the chip even if the Crestron driver tries to re-target. Released on every cleanup path.
- `MOVE_CANCEL_THRESHOLD = 30px` (was 10px) — capacitive jitter wobbles ±15px even with a stationary finger.
- `transform: scale(1.08 * panelScale)` on the clone — visually matches rail chips on the 1.5× panel.

---

## How to deploy

The TS-1070 IP changed during a firmware update from `192.168.2.53` to `192.168.1.175`. Override the default in `package.json`:

```bash
cd MCCCD-AA140
npm run archive               # build .ch5z
PANEL_HOST=192.168.1.175 python scripts/deploy.py   # SFTP + PROJECTLOAD
```

Or in one shot (after fixing the script default):

```bash
npm run deploy:tabletop       # currently hardcoded to old IP — review
```

**Memory file** at `~/.claude/projects/c--Users-scale-CascadeProjects-Archon-Tests-MCCCD-Razzle/memory/reference_mcccd_aa140_panel.md` already records the new IP.

---

## Open polish items (next session)

1. **Long-press reliability** — current threshold 30px may still be tight. Try 50px or switch to "leaves chip bounding box" detection (tolerates any jitter inside the chip).
2. **Drag motion smoothness** — if it still feels jumpy after the panel-scale fix, throttle `cloneCoords.set()` calls via `requestAnimationFrame`.
3. **Hit zones** — verify the audio/mirror icon buttons on D1/D2 don't intercept tile clicks via `elementFromPoint`. If they do, set `pointer-events: none` on them during active drag.
4. **Tile-flash target** — the `flash` class is added to `.tile`, not `.tile-slot`. Reviewer flagged that the tile's outer border-color is what flashes. Verify on hardware whether this reads correctly.
5. **Stage 2 outcome write-up** — under `## Stage 2 Outcome` heading at the bottom of `2026-05-01-drag-drop-stage-2-svelte-port-design.md`. Capture the gut-check verdict.
6. **Pre-existing svelte-check error** — `MicVolumeModal.svelte:64:78`, type mismatch. Not from drag-drop work; flag for cleanup.
7. **`npm run deploy:tabletop` default** — `package.json` hardcodes `192.168.2.53`. Update or document the override.

---

## Theme alignment (next track, started this session)

User picked **Mockup 11 — MCCCD Orange Theme ★ Campus Standard** (`mockups/11-orange-theme.html`). Palette: `#f5a623` accent on `#0d1b2e` navy, sourced from the existing MCCCD panel in another campus room.

The `.theme-orange` class is already pre-baked in `global.css` lines 48–56 — apply to `#app` to activate. But two gaps:

1. The `.theme-orange` block doesn't override `--color-accent-dim` or `--color-panel-soft`, both of which are referenced by Stage 2 components.
2. Hardcoded `rgba(56, 189, 248, ...)` values exist in 4 places that won't theme:
   - `global.css:426` — `chip-arm-pulse` keyframe
   - `components/DragCloneOverlay.svelte:59` — clone box-shadow
   - `components/DisplayTile.svelte:227` — body.any-armed slot tint
   - `components/PresetButton.svelte:141` — preset-btn.pressing background

Plan for theme alignment:
- Extend `.theme-orange` with the two missing token overrides
- Replace the 4 hardcoded rgba values with token references (`var(--color-accent-soft)` or `var(--color-accent-dim)`)
- Add `theme-orange` class to `#app` element in `main.ts`
- Build + deploy

This was in progress when FRED MCP went offline; user asked to log everything before continuing.

---

## FRED state (as of this session)

- FRED MCP was online at session start; logged the Stage 2 deploy + tuning activity (`caa917b9` and `931ee5a5`).
- Task `318aec66-c590-4d48-8f3e-6d3404e028d6` (Stage 1 drag-drop mockup) is in `review` status. Should be moved to `done` and a polish follow-up task created.
- FRED MCP went offline mid-session (not connected). When it reconnects, the polish follow-up task should be created with the open items listed above.

---

## Quick reference — key file locations

```
mockups/18-drag-drop-router.html                    Stage 1 mockup (final)
mockups/index.html                                   Gallery (mockup 11 = orange theme ★)
mockups/11-orange-theme.html                         Theme reference

MCCCD-AA140/src/lib/stores/router.ts                 Drag/arm state machine
MCCCD-AA140/src/lib/stores/signals.ts                CrComLib feedback subscriptions
MCCCD-AA140/src/lib/contract.ts                      SIGNALS namespace
MCCCD-AA140/src/components/SourceRail.svelte         Left rail
MCCCD-AA140/src/components/DragCloneOverlay.svelte   Chip clone
MCCCD-AA140/src/components/DisplayTile.svelte        Drop zone tile
MCCCD-AA140/src/pages/Home.svelte                    Layout
MCCCD-AA140/src/App.svelte                           Mount overlay
MCCCD-AA140/src/global.css                           Tokens + .theme-orange + keyframes
MCCCD-AA140/src/main.ts                              Theme class application point

MCCCD-AA140/scripts/deploy.py                        SFTP+PROJECTLOAD to panel
MCCCD-AA140/build.mjs                                Vite + index.html post-process
```
