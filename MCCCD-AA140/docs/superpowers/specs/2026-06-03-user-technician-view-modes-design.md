# 2026-06-03 — Design: User / Technician Panel View Modes

**Branch:** `feat/screen-relay-and-view-modes` (off `feat/device-integration-usb-signage`)
**Status:** Approved design. **Panel-only** — no contract/processor change, no signals.

## Problem
The panel exposes installer-level controls (mic input trims, output-feed max level,
advanced camera settings) alongside everyday controls. End users only need a simple
subset. We want a **User view** (default) that hides advanced controls and a
**Technician view** (PIN-gated) that reveals everything.

## Approach
A single reactive **`role` store** (`'user' | 'tech'`, default `'user'`) that components
read to conditionally render advanced controls inline (`{#if $role === 'tech'}`). No
duplicated layouts or routes — keeps per-page granularity (the Cameras page shows some
controls and hides others). Rejected alternatives: separate `App.user`/`App.tech`
layouts (duplication), whole-page route gating (too coarse for partial-page hiding).

## Gating & access
- Panel **always boots to User view**. Role is **not persisted** across reboot.
- A **hidden affordance** — press-and-hold (~2 s) an invisible hotspot in a fixed
  screen corner — opens a **PIN modal**. Correct PIN sets `role = 'tech'`.
- **Auto-revert to User** after an inactivity timeout (**default 5 min**, reset on any
  pointer interaction) and via an explicit **"Exit Tech View"** button (shown in a small
  badge while in tech mode).
- PIN is a configurable client-side constant (set to `1981`). **This is panel-side
  gating, not a security boundary** — it deters end users, not a determined one. If a
  hard boundary is needed later, validate processor-side via a contract signal.

## What each view shows

### Cameras page (`pages/Cameras.svelte`)
- **User keeps:** preset recall, zoom, camera-feed selection (Front/Back multicam),
  presenter / group / auto framing.
- **Technician-only:** PTZ drive pad, pan/tilt/zoom **speed sliders**, preset **zones**,
  tracking **profiles**, home/tracking-shot, Send-to-VTC, live coordinates + zoom-ratio
  readout.

### Audio page (`pages/AudioMixer.svelte`)
- **User keeps:** mic **mute** toggles, **master volume** / Vol± / mute-all, **audio
  preset / scene recall**.
- **Technician-only:** per-mic input **trim/gain**, **output feed max** level.

### Display Routing page (`pages/DisplayRouting.svelte`)
- **Technician-only:** the **Outside Signage (D5)** routing section.
- **Stays visible to Users:** display routing matrix, USB host, and the new **Screen
  Up/Down** controls (manual override allowed).

### Debug / device-config
The CWS debug tool is a **separate browser UI served by the processor**, not a panel
page — it is inherently installer-only and needs no panel gating. (No panel router
change required; noted for completeness.)

## Components (`MCCCD-AA140/src/`)
- **`lib/stores/role.ts`** (new): `writable<'user'|'tech'>` defaulting `'user'`;
  `enterTech(pin)` (validates, sets role, starts inactivity timer), `exitTech()`,
  `bumpActivity()` (resets the timer). Timer constant `TECH_TIMEOUT_MS = 5*60*1000`.
- **`components/PinModal.svelte`** (new): numeric keypad, validates against the PIN
  constant, shake + clear on wrong entry, theme-compliant. Calls `enterTech`.
- **`components/TechGate.svelte`** (new): mounted globally in `App.svelte`. Renders an
  invisible corner long-press hotspot, the `PinModal` when armed, and a small "TECH" badge
  with an Exit button while `role==='tech'`. A global `pointerdown` listener calls
  `bumpActivity()` so the idle timer only fires when truly idle.
- **`App.svelte`**: mount `<TechGate />` at root (alongside `DragCloneOverlay`).
- **Existing pages**: wrap the advanced controls/sections listed above in
  `{#if $role === 'tech'}`.

## Data flow & edge cases
Pure client-side; no new signals. Hiding a control does **not** unbind its signal — it
simply isn't rendered, so no dead feedback / no contract churn. Wrong PIN → shake + clear,
stay User. Timeout → `role='user'` and any open tech-only modal closes. Re-entering a page
re-reads `$role` reactively.

## Testing
- Boot → User view; advanced controls absent on Cameras/Audio; D5 signage hidden.
- Long-press hotspot → PIN modal; correct PIN reveals all tech controls; wrong PIN rejected.
- Idle past timeout → reverts to User; Exit button reverts immediately.
- User view still has: presets/zoom/feed/framing, mic mute, master volume, audio presets,
  display routing, screen Up/Down.
- No signal-binding regressions (deploy both panels, feedback still live).

## Out of scope
Processor-side PIN validation, per-panel fixed roles, role persistence across reboot,
multiple privilege tiers beyond user/tech.
