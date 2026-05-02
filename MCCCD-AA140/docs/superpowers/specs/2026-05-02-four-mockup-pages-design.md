# Four Mockup Pages — Design Spec

**Date:** 2026-05-02
**Branch:** `feat/drag-drop-router-mockup` (continues from current branch)
**Mockups in scope:** #15 Shutdown Modal · #12 Splash/Off · #14 Display Routing · #13 Audio Mixer
**Mockup gallery:** `mockups/index.html`

## 1. Goal

Promote four mockup designs (originally produced as static 1280×800 HTML in `mockups/`) into the live Svelte CH5 panel. Each ships as an independent plan against the shared design vocabulary established here.

## 2. Decisions

| # | Topic | Decision |
|---|-------|----------|
| 1 | Scope | One spec, four plans, ship one at a time. Order: **#15 → #12 → #14 → #13**. |
| 2 | Signal fidelity | **Wire it all.** Add new signals to `contract.ts` as needed. SIMPL Pro / `.cce` regen catches up after the Svelte side lands. |
| 3 | #12 Splash | A *state* of `Home.svelte`, gated on `!systemPowerFb`. No new page route. |
| 4 | #14 vs DragDropRouter | #14 *replaces* `DragDropRouter.svelte`. New `DisplayRouting.svelte`. `'dragdrop'` route removed. Drag-drop store (`lib/stores/router.ts`) survives intact. |
| 5 | #14 source set | Stay at the existing 4 sources (`roomPc`, `extPc`, `airMedia`, `laptop`). Trim mockup to match. |
| 6 | #13 vs Settings | #13 *replaces* `Settings.svelte`. `'settings'` route removed. `MicChannel.svelte` and `MicVolumeModal.svelte` deleted. |
| 7 | Navigation | Tile-tap on Home → Routing. Footer becomes Power · Vol · Mic · `Cameras` · `Audio`. No `Settings` button. |
| 8 | #15 Modal | Replace `ConfirmShutdownModal.svelte` in-place. Same required props (`open`, `countdown`, `onConfirm`, `onCancel`). Optional `vacancyMinutes`, `shutdownItems` added. |
| 9 | Component sharing strategy | **Hybrid (C).** Per-page components by default. Extract only `VuMeter` and `SourceIcon` as `lib/ui/` primitives. |

## 3. Architecture overview

### 3.1 File-system delta

```
MCCCD-AA140/src/
├── components/
│   ├── ConfirmShutdownModal.svelte        REWRITE (#15, additive props)
│   ├── HomeSplash.svelte                  NEW   (#12)
│   ├── DragCloneOverlay.svelte            keep
│   ├── DisplayTile.svelte                 keep + tile-tap → routing
│   ├── PresetButton.svelte                keep (mixer presets reuse)
│   ├── MicChannel.svelte                  DELETE (with Settings)
│   ├── MicVolumeModal.svelte              DELETE (Settings only)
│   ├── DropZoneTile.svelte                DELETE (with DragDropRouter)
│   ├── SourceRail.svelte                  DELETE (with DragDropRouter)
│   ├── routing/
│   │   ├── SourceListItem.svelte          NEW (#14)
│   │   └── DisplayCell.svelte             NEW (#14)
│   └── mixer/
│       ├── MixerChannel.svelte            NEW (#13)
│       └── MasterStrip.svelte             NEW (#13)
├── lib/
│   ├── ui/
│   │   ├── VuMeter.svelte                 NEW shared primitive
│   │   └── SourceIcon.svelte              NEW shared primitive
│   ├── contract.ts                        +9 new signals (see §7)
│   └── stores/
│       ├── page.ts                        +'routing'|'audio'; -'settings'|'dragdrop'
│       ├── router.ts                      keep (drag-drop logic survives)
│       └── signals.ts                     +stores for new fb signals
└── pages/
    ├── Home.svelte                        splash branch + footer rework + tile-tap
    ├── Cameras.svelte                     keep
    ├── Settings.svelte                    DELETE
    ├── DragDropRouter.svelte              DELETE
    ├── DisplayRouting.svelte              NEW (#14)
    └── AudioMixer.svelte                  NEW (#13)
```

### 3.2 Page routing

`PageName` becomes `'home' | 'cameras' | 'audio' | 'routing'` (was `'home' | 'cameras' | 'settings' | 'dragdrop'`).

```
Home (systemPowerFb=true)
  ├─ tap a display tile chrome ──→ goToPage('routing')
  ├─ footer "Cameras"           ──→ goToPage('cameras')
  ├─ footer "Audio"             ──→ goToPage('audio')
  └─ tap power button (system on) ──→ ConfirmShutdownModal overlay

Home (systemPowerFb=false)
  └─ renders <HomeSplash/> inline (no nav available)
      └─ tap big power button   ──→ pulseDigital(displayPower)
```

### 3.3 Ship order rationale

1. **Plan 1 — #15 Shutdown Modal.** Single component rewrite. Same API. No nav, no contract change. Establishes the danger-state visual language used by future destructive confirms. Smallest blast radius.
2. **Plan 2 — #12 Home Splash state.** Inline render branch in `Home.svelte`. No new page. No new signals. Establishes the splash visual language and clock pattern.
3. **Plan 3 — #14 Display Routing.** Replaces experimental `DragDropRouter`. Touches drag-drop store, page routing, Home tile interaction, and adds 5 new signals. Largest single page.
4. **Plan 4 — #13 Audio Mixer.** Replaces `Settings`. Largest signal surface (most existing, 4 new). Reuses primitives extracted earlier.

Each plan ends with an archive + deploy to TS-1070 per the panel-deploy workflow; user tests on panel before next plan starts.

## 4. Plan 1 — #15 Shutdown Modal upgrade

**Component:** `components/ConfirmShutdownModal.svelte` — full rewrite. Same required props.

### 4.1 Props

```ts
interface Props {
  open: boolean;
  countdown?: number;                // seconds; default 30
  title?: string;                    // default "Shut Down Room?"
  body?: string;                     // default vacancy-aware message
  vacancyMinutes?: number;           // optional; drives bottom strip; omit → strip hidden
  shutdownItems?: Array<{
    icon: 'display' | 'audio' | 'camera';
    label: string;
  }>;                                // optional; checklist rows; omit → checklist hidden
  onConfirm: () => void;
  onCancel: () => void;
}
```

Existing usage in `Home.svelte` continues to work (only required props passed). To get the full mockup look, Home will pass `vacancyMinutes={$shutdownCountdown}` (re-purposed; it's already an analog feedback) and a default `shutdownItems` array.

### 4.2 Visual structure

- Fixed full-bleed backdrop: `rgba(4,8,18,.72)` + `backdrop-filter: blur(8px)`. Z-index 1000.
- 560px modal card, `border:1px solid rgba(239,68,68,.3)`, `border-radius:20px`, layered red-tinted shadow.
- 4px animated danger gradient stripe at top — `linear-gradient(90deg, #ef4444, #f97316, #ef4444)` with `background-size:200%` and `@keyframes stripe-slide` 2s linear infinite.
- 72px circular danger icon (power glyph) above title.
- Title: 26px / 900 weight.
- Body text: 14px / soft color, vacancy-aware default.
- 120px SVG countdown ring centered:
  - Two `<circle cx="60" cy="60" r="52">` — track (`stroke=rgba(239,68,68,.12)`) and fill (`stroke=var(--danger)`).
  - `stroke-dasharray=326`, `stroke-dashoffset` reactively driven by `(countdown - remaining) / countdown * 326`.
  - `transition:stroke-dashoffset 1s linear` so the ring ticks smoothly between 1Hz state updates.
  - Number readout (42px / 900) + "sec" eyebrow, both inside the ring.
- Action grid (2 cols): Cancel (neutral) · Shut Down Now (danger).
- Optional `shutdown-list` block with `border-top`, label "Will be powered off", and rows mapping each `shutdownItems[i]` to icon + text.
- Optional bottom `vacancy-bar` strip with pulsing dot: `Triggered by occupancy timeout · Room vacant {vacancyMinutes} min · Auto-shutdown threshold: 15 min`.

### 4.3 Behavior

- Existing countdown logic (`setInterval(1s)`) preserved.
- `aria-live="polite"` added to the countdown number for screen reader updates.
- `prefers-reduced-motion` extended to disable `stripe-slide`, `pulse`, and `stroke-dashoffset` transition.

### 4.4 Zero-state behavior with new optional props

- `vacancyMinutes` undefined → vacancy strip hidden, default body text falls back to "Are you sure you want to shut down?".
- `shutdownItems` undefined → checklist block hidden.

### 4.5 Home.svelte change scope for this plan

Only the call site of `<ConfirmShutdownModal>` in `Home.svelte` is touched — to pass `vacancyMinutes={$shutdownCountdown}` and a default `shutdownItems` array. No layout, footer, splash, or routing changes happen in Plan 1.

### 4.6 Out of scope for this plan

- No nav changes.
- No new signals.

## 5. Plan 2 — #12 Home Splash state

### 5.1 Render gate in `Home.svelte`

```svelte
{#if !$systemPowerFb}
  <HomeSplash
    roomName={ROOM_NAME}
    panelOnline={$panelOnline}
    occupancyState={$occupancyState}
    shutdownCountdown={$shutdownCountdown}
    onPowerOn={() => pulseDigital(SIGNALS.displayPower)}
  />
{:else}
  <header>...</header>
  <main class="display-row">...</main>
  <footer class="app-footer">...</footer>
{/if}
```

Existing `<ConfirmShutdownModal/>` stays mounted at the page root regardless of branch (it only renders if `open=true`, which can only become true when system is on).

### 5.2 `HomeSplash.svelte`

**Props:**

```ts
interface Props {
  roomName: string;
  panelOnline: boolean;
  occupancyState: number;            // 0=idle, 1=occupied, 2=vacant
  shutdownCountdown: number;         // minutes (used for vacancy chip)
  onPowerOn: () => void;
}
```

**Layout:** column flex, three vertical zones — top bar / hero / status strip.

**Top bar:**
- Left: MCCCD logo block (square mark + label "MCCCD · Maricopa").
- Right: stacked time (22px / 800 / tabular-nums) + date (12px / soft).
- Time updates via `setInterval(updateNow, 1000)` driving `let time = $state(...)` and `let dateStr = $state(...)`. Cleared on `onDestroy`.

**Hero (center):**
- Eyebrow: "Classroom AV Control · TSW-770".
- Big room name: 88px / 900, last 3 chars colored `var(--accent)`. (For "AA140" → `AA` plain + `140` orange.)
- Sub: "Maricopa Community College · Building A".
- Power CTA button:
  - 96px circle, `rgba(245,166,35,.1)` background, 2px `rgba(245,166,35,.4)` border.
  - Two pseudo-element ring-expand layers with `@keyframes ring-expand` (1.4× scale + opacity 0.6 → 0).
  - Box-shadow `@keyframes ring-pulse` 2.8s ease-in-out infinite.
  - "Touch to Start" hint with `@keyframes fade-cycle` 2.8s.
  - On click → `onPowerOn()`.

**Status strip:** 4 chips horizontally centered.
1. **Network** — green dot if `panelOnline`, else dim. Label "Network Online" / "Panel Offline".
2. **Displays** — always "Displays Off" with off-state dot (system is powered down).
3. **Audio** — always "Audio Idle" with off-state dot.
4. **Occupancy** — `occupancyState`-driven:
   - `1` → green dot, "Occupied"
   - `2` → orange dot, "Vacant · {shutdownCountdown} min"
   - else → dim dot, "Vacant"

### 5.3 Reduced-motion

`@media (prefers-reduced-motion: reduce)` disables `ring-pulse`, `ring-expand`, `fade-cycle`, and the vacancy `pulse`.

### 5.4 No new signals, no new pages

`PageName` unchanged in this plan. Only `Home.svelte` and the new `HomeSplash.svelte` change.

## 6. Plan 3 — #14 Display Routing

### 6.1 Page shell

`pages/DisplayRouting.svelte` — `display:grid; grid-template-rows: 60px 1fr 88px; gap: 10px; padding: 10px`.

### 6.2 Header

- Back-to-Home button → `goToPage('home')`.
- Vertical separator.
- `AA140` room label (18px / 900).
- Vertical separator.
- Eyebrow: "Display Routing".
- Spacer.
- **Mode segmented control** — 3 buttons: `Manual` / `Mirror All` / `Extend`.
  - Bound to `routingModeFb` (1/2/3); active button highlighted.
  - Click → `publishAnalog(routingMode, n)`.
- **Auto-Route chip** — toggle.
  - Bound to `autoRouteEnableFb`.
  - Click → `publishDigital(autoRouteEnable, !$autoRouteEnableFb)`.
  - Pulsing green dot when enabled.

### 6.3 Body

`display:grid; grid-template-columns: 260px 1fr; gap: 10px;`

#### 6.3.1 Source list (left)

`SourceListItem.svelte × 4` rendered from a static `SOURCES` map iteration:

```ts
['roomPc', 'extPc', 'airMedia', 'laptop'].map(id => ({ id, label, value }))
```

Each item:
- `<SourceIcon source={id} />` (40×40 rounded square, accent-tinted when armed/active).
- Name + sub ("Input N · {connectorType}"). Connector types: HDMI / HDMI 2 / Wireless / HDMI 3.
- Right-aligned route badge: comma-joined display IDs (`D1 D2`) computed reactively from `display{1,2,3}SourceFb`.
- `data-source={id}`, `class="chip"` so existing `router.ts` selectors continue to find it.
- Tap to arm (`armChip`), long-press 250ms to drag (`chipPointerDown`).

#### 6.3.2 Display matrix (right)

`matrix-panel` contains a body grid: `grid-template-columns: repeat(3, 1fr); gap: 14px`.

`DisplayCell.svelte × 3` (D1, D2, D3) plus a row-spanning `mirror-row` of quick mirror buttons.

`DisplayCell.svelte` props:

```ts
interface Props {
  displayId: 'd1' | 'd2' | 'd3';
  label: string;                     // "Front Left", etc.
  spec: string;                      // "65\" 4K · NEC"
  activeSourceFb: number;            // analog feedback
  powerOn: boolean;
  audioActive: boolean;
  onPowerToggle: () => void;
  onAudioToggle: () => void;
  onMirror?: () => void;             // optional; D1/D2 only
}
```

Renders:
- `class="tile"`, `data-display={displayId}`, with inner `.tile-slot` so router.ts hover/drop logic finds it.
- 16:9 screen mockup block:
  - `<SourceIcon>` for current source (if any) or muted icon if `activeSourceFb===0`.
  - Source name (or "No Source" placeholder).
  - Top-left power LED (green if `powerOn`).
  - Top-right `♪ AUDIO` tag (orange) if `audioActive`.
  - `disp-drop-hint` overlay (hidden by default, shown via `.drag-over`).
- Info row: D# label + name + spec / power button / audio button / optional mirror button.

#### 6.3.3 Mirror quick-actions row

Spans all 3 columns below the cells:
- `Mirror D1 → D3` → `pulseDigital(d1MirrorToD3)`.
- `Mirror D2 → D3` → `pulseDigital(d2MirrorToD3)`.
- `All Displays Same` → `pulseDigital(mirrorAllSame)` (new signal).

### 6.4 Footer

```
┌──────────────────────────────────────────────────────────┐
│ QUICK ROUTES   [Room PC → All]  [AirMedia → All]   [Clear All Routes] │
└──────────────────────────────────────────────────────────┘
```

- "Room PC → All" / "AirMedia → All" — tap publishes that source value to all three `displayNSource` analogs in one tick.
- "Clear All Routes" — tap publishes `0` to all three `displayNSource` analogs.

### 6.5 Drag-drop reuse

`lib/stores/router.ts` is unchanged. Class names preserved: `.chip` (rail) / `.tile` (display cell) / `.tile-slot` (drop area). `DragCloneOverlay` continues to render at App root.

`SourceListItem` calls `chipPointerDown(e, el, sourceId)` on `pointerdown`. `DisplayCell` does not need a click handler for routing — `router.ts` reads `data-display` from the `.tile` element and publishes the selected source's value to the corresponding `displayNSource` analog.

### 6.6 Home tile-tap navigation

`components/DisplayTile.svelte` gains an outer click handler on the tile chrome (NOT the source-select buttons inside). Tapping the tile itself calls `goToPage('routing')`. The existing source-select buttons keep their direct-route behavior (`armChip` / `routeSource` / `publishAnalog`). The arm/drag interaction is unchanged.

Implementation note: a new outer `<div>` wraps the existing tile content with the navigation click handler. Each existing source-select `<button>` inside the tile gets an `onclick` handler that calls `event.stopPropagation()` (in addition to its existing route action) so taps on a button do not bubble up and trigger navigation. The header label area, source-list container, and any non-button regions DO bubble and trigger navigation. Long-press / drag (`pointerdown`-driven via `chipPointerDown`) is unaffected because pointer events are independent of the click bubble path.

### 6.7 Cleanup in this plan

- `pages/DragDropRouter.svelte` deleted.
- `components/SourceRail.svelte` deleted.
- `components/DropZoneTile.svelte` deleted.
- `'dragdrop'` removed from `PageName` in `lib/stores/page.ts`.
- App.svelte `{:else if $currentPage === 'dragdrop'}` branch removed; `'routing'` branch added.

### 6.8 New signals (Plan 3)

| Signal | Direction | Type | Purpose |
|--------|-----------|------|---------|
| `routingMode` | set | analog | Mode segmented (1=Manual, 2=Mirror, 3=Extend) |
| `routingModeFb` | fb | analog | Active mode for highlight |
| `autoRouteEnable` | set | digital toggle | Auto-Route chip |
| `autoRouteEnableFb` | fb | digital | Auto-Route active state |
| `mirrorAllSame` | pulse | digital | "All Displays Same" quick-action |

### 6.9 Zero-state behavior

- `routingModeFb === 0` → Manual button highlighted (default).
- `autoRouteEnableFb === false` → chip neutral, no pulse.
- `display{N}SourceFb === 0` → cell shows "No Source" placeholder.

## 7. Plan 4 — #13 Audio Mixer

### 7.1 Page shell

`pages/AudioMixer.svelte` — `display:grid; grid-template-rows: 60px 1fr 88px; gap: 10px; padding: 10px`.

### 7.2 Header

- Back-to-Home button → `goToPage('home')`.
- `AA140` room label.
- Eyebrow "Audio Mixer".
- Spacer.
- **Master volume readout chip:** label "Master" + dB value (computed from `progAudioLevelFb`) + `−`/`+` buttons (→ `volumeDown` / `volumeUp` digital pulses).
- **Mute All** button (red) → `pulseDigital(muteAll)`.

### 7.3 Body

`display:grid; grid-template-columns: repeat(5,1fr) 2px 140px; gap: 0;`

Five `MixerChannel.svelte` strips, then a 2px divider, then one `MasterStrip.svelte`.

#### 7.3.1 `MixerChannel.svelte` props

```ts
interface Props {
  type: string;                      // "Wireless · Ch 1"
  name: string;                      // "Lavalier"
  model: string;                     // "CCS-UWB Beltpack"
  connected: boolean;
  level: number;                     // 0..100, drives stereo VU bars
  lineOut: number;                   // 0..100, drives fader fill + thumb position
  trim: number;                      // -20..+20 dB
  muted: boolean;
  onLineOutChange: (n: number) => void;
  onTrimChange: (n: number) => void;
  onMuteToggle: () => void;
}
```

#### 7.3.2 Channel strip layout

- `ch-head` (top, ~80px): type / name / connection dot+label / model.
- `ch-body` (1fr, padded):
  - `fader-wrap` row: stereo `<VuMeter>` left / vertical fader / stereo `<VuMeter>` right.
  - Vertical fader: 28px-wide track, `fader-rail` 4px, `fader-fill` height = `lineOut`%, `fader-thumb` positioned at `bottom: lineOut%`.
  - Trim row at the bottom of `ch-body`: label "Trim" + `<input type="range" min="-20" max="20">` + readout.
- `ch-foot` (bottom, ~70px): mute button (`LIVE` / `MUTED` / `OFFLINE`) + Peak readout (computed from `level`).

When `connected===false`: `fader-wrap`, `trim-row`, and mute button get `opacity:.35` and `cursor:not-allowed`. Mute button reads `OFFLINE`. Peak shows `—`.

#### 7.3.3 Channel-to-signal mapping

| # | Channel name | level fb | trim set/fb | lineOut set/fb | mute set/fb | connected fb |
|---|---|---|---|---|---|---|
| 1 | Lavalier | `micLavLevel` | `micLavTrim` / `micLavTrimFb` | `micLavLineOut` / `micLavLineOutFb` | `micLavMute` / `micLavMuteFb` | `micLavConnected` |
| 2 | Handheld | `micHandheldLevel` | `micHandheldTrim` / `micHandheldTrimFb` | `micHandheldLineOut` / `micHandheldLineOutFb` | `micHandheldMute` / `micHandheldMuteFb` | `micHandheldConnected` |
| 3 | Ceiling 1 | `micCeiling1Level` | `micCeiling1Trim` / `micCeiling1TrimFb` | `micCeiling1LineOut` / `micCeiling1LineOutFb` | `micCeiling1Mute` / `micCeiling1MuteFb` | `micCeiling1Connected` |
| 4 | Ceiling 2 | `micCeiling2Level` | `micCeiling2Trim` / `micCeiling2TrimFb` | `micCeiling2LineOut` / `micCeiling2LineOutFb` | `micCeiling2Mute` / `micCeiling2MuteFb` | `micCeiling2Connected` |
| 5 | Ceiling 3 | `micCeiling3Level` | `micCeiling3Trim` / `micCeiling3TrimFb` | `micCeiling3LineOut` / `micCeiling3LineOutFb` | `micCeiling3Mute` / `micCeiling3MuteFb` | `micCeiling3Connected` |

All exist already in `contract.ts`.

#### 7.3.4 `MasterStrip.svelte`

- "Output" label.
- Vertical master fader bound to `progAudioLevel` (set) / `progAudioLevelFb` (new fb signal). 6px-wide rail with gradient fill.
- Master dB readout (22px / 900) computed from `progAudioLevelFb`.
- Output select 2-row grid: `D1` / `D2` buttons. Active highlighted from `audioOutputSelectFb`. Click → `publishAnalog(audioOutputSelect, 1|2)`.

### 7.4 Footer

```
┌────────────────────────────────────────────────────────────┐
│ PRESETS  [Lecture] [Presentation] [Hybrid] [Recording]   [Link Ceilings 1+2] │
└────────────────────────────────────────────────────────────┘
```

- Preset buttons (`PresetButton.svelte` reused): map to `sceneRecall` analog values 1–4. Active button highlighted from `sceneRecallFb`. Click → `publishAnalog(sceneRecall, n)`.
- "Link Ceilings 1+2" chip — toggle bound to `audioLinkCeilings12Fb`. Click → `publishDigital(audioLinkCeilings12, !$audioLinkCeilings12Fb)`.

### 7.5 Cleanup in this plan

- `pages/Settings.svelte` deleted.
- `components/MicChannel.svelte` deleted.
- `components/MicVolumeModal.svelte` deleted.
- `'settings'` removed from `PageName`.
- App.svelte `{:else if $currentPage === 'settings'}` branch removed; `'audio'` branch added.

### 7.6 New signals (Plan 4)

| Signal | Direction | Type | Purpose |
|--------|-----------|------|---------|
| `progAudioLevelFb` | fb | analog | Master fader read-back |
| `sceneRecallFb` | fb | analog (1–4) | Active preset highlight |
| `audioLinkCeilings12` | set | digital toggle | Link Ceiling 1+2 faders |
| `audioLinkCeilings12Fb` | fb | digital | Link active state |

### 7.7 Zero-state behavior

- `progAudioLevelFb === 0` → master fader at 0%, dB readout shows `−∞` (or `-60`).
- `sceneRecallFb === 0` → no preset highlighted.
- `audioLinkCeilings12Fb === false` → chip neutral.
- `mic{N}Connected === false` → that strip dimmed and disabled.

## 8. Shared primitives

### 8.1 `lib/ui/VuMeter.svelte`

```ts
interface Props {
  level: number;                     // 0..100
  segments?: number;                 // default 14
  orientation?: 'vertical';          // default 'vertical' (only vertical for now)
}
```

Renders a column-reverse stack of `<div class="vu-seg">` segments. Coloring rule by index:
- segments 0..(0.5×N) → green (`lit-green`)
- segments (0.5×N)..(0.75×N) → yellow (`lit-yellow`)
- segments (0.75×N)..N → red (`lit-red`)

Lit count = `Math.round((level / 100) * segments)`. Unlit segments use the dim base color.

### 8.2 `lib/ui/SourceIcon.svelte`

```ts
interface Props {
  source: 'roomPc' | 'extPc' | 'airMedia' | 'laptop';
  size?: number;                     // default 20
  state?: 'default' | 'active' | 'disabled'; // default 'default'
}
```

Static SVG glyph map:
- `roomPc` → desktop monitor icon.
- `extPc` → desktop tower icon.
- `airMedia` → wifi icon.
- `laptop` → laptop icon.

`state='active'` applies accent-color stroke; `'disabled'` reduces opacity. No background container — caller wraps in any tile/badge as needed.

## 9. Contract additions consolidated

```ts
// Plan 3 — Display Routing
routingMode:           `${ROOM_NAME}.RoutingMode`,
routingModeFb:         `${ROOM_NAME}.RoutingModeFb`,
autoRouteEnable:       `${ROOM_NAME}.AutoRouteEnable`,
autoRouteEnableFb:     `${ROOM_NAME}.AutoRouteEnableFb`,
mirrorAllSame:         `${ROOM_NAME}.MirrorAllSame`,

// Plan 4 — Audio Mixer
progAudioLevelFb:      `${ROOM_NAME}.ProgAudioLevelFb`,
sceneRecallFb:         `${ROOM_NAME}.SceneRecallFb`,
audioLinkCeilings12:   `${ROOM_NAME}.AudioLinkCeilings12`,
audioLinkCeilings12Fb: `${ROOM_NAME}.AudioLinkCeilings12Fb`,
```

**Total:** 9 new signal names. Each fb signal also gets a `writable` store registered in `lib/stores/signals.ts`.

`.cce` regen and SIMPL Pro catch-up are out of scope for this branch. Until SIMPL is updated, the new fb signals will read `0` / `false`. The UI must render sensibly in that zero state per §6.9 and §7.7.

## 10. Page state & nav transitions

`lib/stores/page.ts`:

```ts
export type PageName = 'home' | 'cameras' | 'audio' | 'routing';
```

`'settings'` and `'dragdrop'` removed.

App.svelte branches:

```svelte
{#if $currentPage === 'home'}
  <Home />
{:else if $currentPage === 'cameras'}
  <Cameras />
{:else if $currentPage === 'audio'}
  <AudioMixer />
{:else if $currentPage === 'routing'}
  <DisplayRouting />
{/if}
```

`<DragCloneOverlay />` stays mounted at App root.

Footer rework on `Home.svelte`:

```svelte
<div class="nav-group">
  <button class="btn nav-btn" onclick={() => goToPage('cameras')}>Cameras</button>
  <button class="btn nav-btn" onclick={() => goToPage('audio')}>Audio</button>
</div>
```

## 11. Risks & mitigations

| # | Risk | Mitigation |
|---|------|-----------|
| 1 | Drag-drop class-name dependency. `router.ts` selects `.chip`, `.tile`, `.tile-slot` globally. New components must keep these names or the drag flow breaks silently. | Spec mandates these class names on `SourceListItem` and `DisplayCell`. Plan 3 includes a panel-side smoke test of long-press → drop → published signal. |
| 2 | `.cce` regen is out of scope. New fb signals read 0/false until SIMPL catches up. | §6.9 and §7.7 specify zero-state UI behavior. Spec is documented as the SIMPL-side TODO list. |
| 3 | Footer space at 1280×800. Power · Vol-group · Mic-group · Cameras · Audio is dense. | Verified mockup layout fits at 1280; TS-1070 (1920×1200) panel-scaled has headroom. If overflow appears at 1280, tighten `.footer-btn` padding or `.footer-label` margin. |
| 4 | Splash clock is browser-driven. | Acceptable; panel has its own clock. If wall-clock drift becomes an issue, future plan can subscribe to a CH5 time signal. Out of scope here. |
| 5 | Reduced-motion regressions. Each new piece adds keyframes. | Each plan ends with a `prefers-reduced-motion` audit checkpoint. |
| 6 | Tile-tap on Home steals taps from inner buttons. | Implementation note in §6.6: handler attached to outer chrome only; inner button events stop propagation. Plan 3 includes panel-side test that source-select buttons still route directly. |
| 7 | Settings-page deletion removes the only place currently editing trim/lineOut. | Plan 4 ships the Mixer and deletes `Settings.svelte` / `MicChannel.svelte` / `MicVolumeModal.svelte` in the same commit. The footer change ("Settings" → "Audio" label) is also in Plan 4 so users never see a "Settings" button that opens nothing. |
| 8 | Plans 1–3 ship while `Settings` is still in the codebase. The footer "Settings" button continues to work in Plans 1–3 even though Settings.svelte is on death row. | Acceptable — Settings remains fully functional until Plan 4 lands. No interim broken state. |

## 12. Testing & rollout

Each plan ends with the panel-deploy workflow: `archive` + deploy `.ch5z` to TS-1070 (192.168.2.53). User performs panel-side acceptance per the workflow memo before the next plan begins. No browser-only sign-off.

**Plan 1 acceptance:** open the modal from Home power button → ring counts down → confirm and cancel both work → reduced-motion off.

**Plan 2 acceptance:** put system into off state (whatever path SIMPL exposes; or by manual `systemPowerFb` toggle in dev) → splash renders → tap big power button → system transitions to on layout.

**Plan 3 acceptance:** reach Routing via tile-tap on each of D1/D2/D3 → arm a source from list → tap a cell → cell updates → drag a source to a cell → cell updates → quick-route "Room PC → All" updates all three → clear-all clears all three. Mode buttons publish (verifiable in panel logs even if SIMPL isn't wired).

**Plan 4 acceptance:** reach Mixer via footer → all five strips render with current trim/lineOut/mute fb values → adjust each control → fb returns reflect change (or, until SIMPL catches up, optimistic UI keeps the value visible) → master fader publishes → preset buttons publish → mute-all pulses → output D1/D2 toggles.

## 13. Out of scope

- `.cce` contract regeneration.
- SIMPL Pro signal catch-up.
- Camera page changes (Mockup #07 is in the gallery but not this scope).
- Edge-rail / Mockup #10 chrome refactor.
- Splash → home transition animation beyond the existing render-branch swap.
- Landscape vs portrait at the same time (current panel is landscape only).
- Touch-targets audit per WCAG (deferred until a dedicated accessibility pass).
