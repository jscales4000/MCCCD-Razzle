# AA140 CH5-Svelte Touchpanel — Design Spec

**Project:** MCCCD-AA140
**Room:** Maricopa Community Colleges District, Room AA140
**Date:** 2026-04-26
**Stack:** ch5-svelte-v2 (Svelte 5 + Vite 6 + TypeScript) + SIMPL# Pro on RMC4
**Status:** Draft for review (pre-implementation)

---

## 1. Context

Multi-source AV system for a teaching/conferencing space. Three displays driven by NVX, three 1Beyond cameras for capture/VTC ingest, and a Q-SYS Nano DSP that owns all audio. CH5-Svelte touchpanel UI is the user-facing control surface, deployed identically to a TS-1070, a TSW-1070, and a WebXPanel.

The build is opinionated: single-room scope, single panel layout for all surfaces (no per-panel divergence in v1), and the Q-SYS Nano is the audio brain — SIMPL# never drives the downstream amp.

## 2. Hardware Inventory

| Device | Model | IPID | IP | Role |
|---|---|---|---|---|
| Touchpanel | TS-1070 | 0x03 | 192.168.2.53 | Tabletop control |
| Touchpanel | TSW-1070 | 0x04 | 192.168.2.123 | Wall control |
| Soft-panel | WebXPanel | 0x05 | n/a (browser) | Tech / remote |
| Processor | RMC4 | — | 192.168.1.191 | Control system |
| Encoder | NVX E30 (Room PC) | 0x11 | TBD | Source 1 |
| Encoder | NVX E30 (Ext PC) | 0x12 | TBD | Source 2 |
| Encoder | NVX E30 (AirMedia) | 0x13 | TBD | Source 3 |
| Encoder | NVX-384 (HDMI + USB-C) | 0x14 | TBD | Sources 4 & 5 — auto-switch internal |
| Decoder | NVX D200 (D1) | 0x21 | TBD | Display 1 |
| Decoder | NVX D200 (D2) | 0x22 | TBD | Display 2 |
| Decoder | NVX D200 (D3) | 0x23 | TBD | Display 3 |
| DSP | Q-SYS Nano | 0x31 | TBD | All audio routing/mixing/level via Crestron Q-SYS PA module |
| Camera | 1Beyond i20 (Front) | — | TBD | Front-of-room |
| Camera | 1Beyond i12 (Back-Left) | — | TBD | Back-of-room left |
| Camera | 1Beyond i12 (Back-Right) | — | TBD | Back-of-room right |
| Sensor | PoE Occupancy | 0x41 | TBD | Auto-on / vacancy shutdown |

**Note on the NVX-384:** the original spec had 5 E30 encoders; we're replacing the HDMI and USB-C E30s with a single NVX-384 (4-input encoder) that handles both physical inputs and auto-switches internally to whichever input is plugged in / active. The user still sees both HDMI and USB-C as independent source buttons in the UI.

## 3. Stack & Comms

**Panel (frontend):**
- ch5-svelte-v2 template (Svelte 5, Vite 6, TypeScript 5)
- `@crestron/ch5-crcomlib` 2.17.x runtime
- Glass-card design system (dark theme, cyan accent, 1280×800 base scale)
- Single layout deployed to all panels

**Processor (backend):**
- SIMPL# Pro on RMC4 (no AV Framework — room is small enough that pure SIMPL# is the right fit)
- Crestron Q-SYS PA Module for all DSP control
- Native NVX SDK for routing
- HTTP REST for 1Beyond cameras (PTZ, presets, tracking modes)

**Comm topology:**
- Touchpanels ↔ RMC4: native CIP over LAN
- RMC4 ↔ NVX devices: NVX SDK
- RMC4 ↔ Q-SYS: PA module (TCP/QRC under the hood)
- RMC4 ↔ 1Beyond cameras: HTTP REST for control; cameras stream RTSP directly to the touchpanel `ch5-video` element (no processor in the video path)
- RMC4 ↔ Occupancy sensor: PoE / CIP

## 4. Sources (4 logical, 4 encoders)

| # | Label | Encoder | IPID | Notes |
|---|---|---|---|---|
| 1 | Room PC | E30 | 0x11 | |
| 2 | Ext PC | E30 | 0x12 | |
| 3 | AirMedia | E30 | 0x13 | |
| 4 | Laptop | NVX-384 (HDMI + USB-C) | 0x14 | One logical source; encoder auto-switches between HDMI and USB-C internally based on which is active |

**Auto-switch behavior on NVX-384:** the encoder accepts both HDMI and USB-C inputs and auto-selects whichever is currently active. The touchpanel surfaces a single "Laptop" source button; the user doesn't need to know which physical port is feeding the stream. (The standalone `NvxAutoSwitchSrc` feedback signal in the original spec is removed since it's no longer needed for UX.)

## 5. Information Architecture

Three pages total:

1. **Home** *(default)* — display routing, audio, mics, occupancy, system power, link to Cameras + Settings
2. **Cameras** — single-cam preview, PTZ + zoom, single-button presets (tap=recall, hold-3s=save), VTC ingest, tracking modes
3. **Settings** *(v1 — mic management)* — 5-mic management surface: connection status, real-time level meter, input gain trim slider, line-out trim slider, and mute (ceiling mics only — Lav/Handheld mute lives on Home)

Page navigation uses a simple `currentPage` Svelte store. No router library.

## 6. Routing UX (Home Page)

**Layout:** three "display tiles" side-by-side in the main area (D1, D2, D3). Each tile contains:

- Display label and current source name (driven by feedback)
- 4 source buttons in a 2×2 grid: Room PC / Ext PC / AirMedia / Laptop
- Display ON/OFF indicator dot (green = on, dim = off) driven by `Display<N>PowerFb` from the NVX D200 sink-connected feedback
- *(D1 and D2 only)* "↗ Mirror to D3" button — fires a one-shot push of this tile's current source to D3
- *(D1 and D2 only)* Audio-output speaker icon — toggles which display owns room audio (mutually exclusive)

**D3 tile** has the same source picker but no mirror button (it's the target, not a source) and no audio toggle (audio only follows D1 or D2).

**Mirror behavior — fire and forget:**

- Tap "Mirror to D3" on D1 → SIMPL# publishes D1's current source value to D3's source signal once. D3's picker updates to reflect.
- Tap on D2 → same, but from D2.
- After publish, D3 is fully independent again. No live follow, no auto-release, no exclusive lock.

**D3 boot-time initialization:**

- On system power-on (RMC4 boot or "Power" button press), SIMPL# performs a one-shot push of `Display2Source` → `Display3Source` so D3 starts mirrored to D2 by default.
- After this initial copy, D3 is independent. If the user wants D3 to stay mirrored to D2 throughout, they need to re-tap mirror after each D2 source change.

## 7. Audio Architecture

The Q-SYS Nano DSP owns everything audio-related. SIMPL# never drives the downstream amp directly (the amp is "dumb" — line-level in, speakers out, no control).

**Microphone roster (5 mics total):**
- Lavalier (wired or wireless w/ wired-receiver patch)
- Handheld (wireless w/ wired-receiver patch)
- 3× Sennheiser TCCM ceiling mics (TeamConnect Ceiling Medium — Dante/AES-67 native, but for v1 patched into Q-SYS via the existing IO)

**Signal flow:**

```
NVX-384 + E30 encoders ──AES-67──▶ Q-SYS Nano DSP ──line out──▶ dumb amp ──▶ ceiling speakers
                                       ▲
                  Lavalier mic ────────┤
                  Handheld mic ────────┤  (wired analog / Dante inputs)
                  TCCM Ceiling 1 ──────┤
                  TCCM Ceiling 2 ──────┤
                  TCCM Ceiling 3 ──────┘
```

**Audio-follows-display:** when the user toggles the speaker icon on D1 or D2, SIMPL# tells the Q-SYS PA module to switch its program audio source to the AES-67 stream from that display's currently routed encoder. D3 cannot own room audio.

**UI controls (home page footer):**
- Master program volume — Vol−, Mute, Vol+ (no pill, no slider — pulse buttons)
- Lavalier mic mute toggle
- Handheld mic mute toggle
- Audio-follows-display toggle is on the D1/D2 tiles (speaker icon)

> Ceiling mics do NOT appear on Home. They're managed entirely from Settings.

**UI controls (settings page — Audio tab / mic management):**
For each of the 5 mics:
- **Connection status** indicator (green = connected, red = no signal/disconnected — driven by `Mic<Name>Connected` feedback from Q-SYS)
- **Live level meter** (real-time analog feedback `Mic<Name>Level` from Q-SYS, 0–100 scale)
- **Input gain (mic trim) slider** — `Mic<Name>Trim` analog set, 0–100 scale (Q-SYS named-control: input gain stage in dB or normalized)
- **Line-out level slider** — `Mic<Name>LineOut` analog set, 0–100 scale (Q-SYS named-control: output fader to program mix)
- **Mute toggle** (ceiling mics only — Lav and Handheld mute is on Home)

## 8. Camera UX (Cameras Page)

**Layout:**

```
┌─────────────────────────────────────────────────────────────┐
│ ← Home          AA140 Cameras                       Online  │
├──────────┬───────────────────────────────┬─────────────────┤
│ FRONT    │                               │ Pan Speed:  ─── │
│  i20     │   [ ch5-video preview ]       │ Tilt Speed: ─── │
│ ────     │   transparent PTZ overlay:    │                 │
│ BACK-L   │   ▲   ◀  ●  ▶   ▼            │ ┌─────────────┐ │
│  i12     │   on top of video             │ │▶ Send to VTC│ │
│ ────     │                               │ └─────────────┘ │
│ BACK-R   │                               │                 │
│  i12     │                               │ Tracking Mode:  │
│          │                               │ ○ People        │
│          │                               │ ○ Group         │
│          │                               │ ● VX AutoSwitch │
│          │                               │                 │
├──────────┴───────────────────────────────┴─────────────────┤
│ Presets: [DEFAULT]  [PRIMARY]  [SECONDARY]                  │
│   each with Save / Recall / Delete buttons                  │
└─────────────────────────────────────────────────────────────┘
```

**Camera selector (left sidebar):** picks which of the three 1Beyond cameras (Front i20, Back-L i12, Back-R i12) the touchpanel is currently previewing and controlling. This is the *operator preview* — independent from the VTC ingest feed.

**Live preview (center):** `ch5-video` element pointed at the selected camera's main RTSP stream (`rtsp://<cam-ip>/stream1`). Transparent PTZ buttons sit on top of the video at the four edges; tapping pulses pan/tilt commands via the 1Beyond REST API at the current speed values.

**PTZ speed sliders (right):** pan and tilt speed (1–100) sent as the speed param on REST PTZ calls.

**Send to VTC button (right):** sets the currently-selected camera as the active VTC ingest source. The touchpanel preview is for the operator; the VTC ingest is the outbound feed to whatever video conferencing system is downstream. This is a separate one-shot command per tap.

**Tracking mode toggle (right, radio group):**
- People Tracking — 1Beyond tracks a single individual
- Group Tracking — 1Beyond frames the whole group
- VX AutoSwitch — 1Beyond's auto-switching mode across cameras

Mode is per-camera and pushed to the active camera's REST endpoint when changed.

**Presets row (bottom):** 3 preset slots per camera (DEFAULT, PRIMARY, SECONDARY). Each preset is a **single button** with two-mode interaction:
- **Tap (release < 3s)** → Recall preset (fires `ShotPresetRecall` analog with the preset index)
- **Hold (≥ 3s)** → Save current camera position as preset (fires `ShotPresetSave` analog with the preset index)
- During hold, the button shows a filling progress ring; if released before 3s, no save happens (just recall on tap-up).
- Delete preset is **deferred** — long-press on a populated preset could open a delete confirmation modal in v2; for v1 there's no delete UI (saves overwrite).

**Camera zoom (new in v1):** zoom in / zoom out icon buttons in the controls panel beside the speed sliders. Each is a transparent flat-white icon (`+` and `−` glyphs, or magnifying-glass pair). Tap = single zoom step; press-and-hold = continuous zoom. SIMPL# fires `ZoomIn` / `ZoomOut` digital signals; SIMPL# translates into 1Beyond REST `cgi-bin/ptz?action=zoom&direction=in&speed=...` calls (start on rising edge, stop on falling edge — same press-and-hold pattern as PTZ pan/tilt).

## 9. Occupancy Automation

PoE occupancy sensor at IPID 0x41 reports occupancy as a digital feedback. SIMPL# state machine:

| Transition | Action |
|---|---|
| System off + Empty → Empty | no-op |
| Empty → Occupied | run system-on sequence: panels wake, displays power on, NVX last-active routes restored, audio unmute, D3 init from D2 |
| Occupied → Empty | start 30-minute soft-shutdown timer (configurable via SIMPL# constant) |
| Empty (timer running) → Occupied | cancel timer, no other action |
| Empty (timer expires) → Empty | run system-off sequence: displays off, source mute, audio mute |

**UI surface:** a non-pill occupancy indicator in the home-page header (rendered as a tagged status block in the chosen button style — no pill / capsule shapes). Three states:
- Green "Occupied"
- Amber "Vacant"
- Red "Vacant — shutting down in N min" (live countdown from `ShutdownCountdown` analog feedback)

## 9b. System Power Confirmation Modal

The home-page Power button has two states and an interaction confirmation flow:

- **System OFF state:** Power button at default size in the footer.
- **System ON state:** Power button rendered in the **enlarged** primary variant (per the chosen button style — typically ~84-88px tall with a stronger accent treatment) so the operator can see at a glance the system is live.
- **Tap when ON:** opens a modal "Are you sure you want to shut down?" with:
  - Title: "Shutdown AA140"
  - Body copy: "The system will power off in N seconds." with a 30-second countdown timer.
  - **Yes** button (danger variant — destructive)
  - **No** button (ghost / cancel)
  - Tapping **Yes** OR letting the timer hit 0 → fires `DisplayPower` pulse to SIMPL# `SystemPowerController.PowerDownSequence()`.
  - Tapping **No** → modal closes, no signals sent, no state change.
- **Tap when OFF:** immediately fires `DisplayPower` pulse to power the system on (no confirmation).

This protects against accidental shutdowns mid-class while still giving instructors the ability to do a fast power-up.

## 10. Personas (Archon project)

| Persona | Role |
|---|---|
| Crestron CH5 Extended Developer | ch5-svelte UI implementation (primary frontend) |
| Crestron UX Master | layout, accessibility, responsive scaling between TS-1070 and TSW-1070 |
| CH5 Video Integration Specialist | `ch5-video` config, RTSP wiring, transparent PTZ overlay |
| Crestron SIMPL# Engineer | NVX routing, Q-SYS PA module integration, occupancy state machine, system on/off, source-following audio |
| device-api-specialist | 1Beyond REST endpoints (PTZ, presets, tracking modes), Q-SYS named-component conventions |
| Crestron CWS & WebSocket Protocol Engineer | *deferred — only if SIMPL# Debug Tool is wired in for runtime debugging* |

## 11. Scaffold Composition

The scaffold gives us three layouts that map nearly 1:1 to our needs. Build plan:

- **`src/pages/Home.svelte`** — three display tiles (each with 4 source buttons + display-power-FB indicator dot + audio-out toggle on D1/D2 + mirror-to-D3 on D1/D2), header with non-pill occupancy block + online status, footer with Power (with confirm modal) + Vol−/Mute/Vol+ + Lav/Handheld mic mutes + Cameras link + Settings link.
- **`src/pages/Cameras.svelte`** — camera selector sidebar, `ch5-video` preview with transparent PTZ + zoom overlay, Send-to-VTC button, tracking mode radio group, **single-button presets** (tap=recall, hold-3s=save) with progress-ring during hold.
- **`src/pages/Settings.svelte`** — 5-mic management surface, one row per mic with: connection-status dot, real-time level meter, input-trim slider, line-out slider, mute toggle (ceiling mics only).
- **`src/components/`** — DisplayTile, ConfirmShutdownModal, MicChannel (used in Settings), PresetButton (with hold-to-save behavior).
- **`src/App.svelte`** — thin router across Home / Cameras / Settings.

The eight `layouts/*.svelte` files stay in the scaffold as reference but aren't loaded.

## 11b. Button Styling

Style **#2 Signal Tile** (UI Designer's mockup) — picked over the originally recommended #3 Hairline Schematic because the channel-strip language reads more decisively at a distance and gives radio-group buttons (camera tracking modes, source pickers) an unambiguous "this is selected" gesture.

- **Base button:** 8px-radius rectangle on a 50%-opacity slate surface. A 4px-wide left-edge accent bar (subtle gray at rest) sits inside the button's `overflow: hidden` clip.
- **Hover:** surface lifts slightly (65% opacity) and the left bar tints cyan.
- **Active:** the left bar fills horizontally from 4px → 100% with a cyan-fade gradient, button background tints cyan (14% opacity), border lightens to cyan, text goes pure white.
- **Pressed:** `scale(0.97)` + darker background (`rgba(15,23,42,0.7)`).
- **Icon buttons (`.icon-btn`):** transparent background, flat-WHITE icon glyph, 8px-radius rectangle (not circular). On active: soft cyan tint background (18% opacity) with a 1px cyan outline. **No colored borders or fills on the icon itself.**
- **Primary / power-on (`.btn.primary`):** 88px tall, cyan border (40% opacity), 8px-wide cyan left bar (always solid). Used for the Power button when `systemPowerFb` is true, and for confirmation-modal primary CTAs.
- **Danger (`.btn.danger`):** red left bar + red border + dark-red translucent fill. Used for "Yes / Shutdown" in the confirm modal.
- **Ghost (`.btn.ghost`):** transparent fill, faint gray left bar — used for "No / Cancel" in the modal.
- **Reduced-motion:** all button transitions clamp to 0ms when `prefers-reduced-motion: reduce`.
- **No pills:** every interactive surface uses `border-radius ≤ 8px`. The occupancy indicator and online-status block use the same border-radius (no `9999px` capsules).

CSS tokens (in `:root`):
```
--btn-fg: #f1f5f9
--btn-surface: rgba(30, 41, 59, 0.5)
--btn-surface-hi: rgba(51, 65, 85, 0.65)
--btn-bar-w: 4px
--btn-accent: var(--color-accent)        /* cyan #38bdf8 */
--radius-button: 8px
```

Full CSS lives in `src/global.css` under the `/* === Signal Tile button system (#2) === */` block. Mockups doc with all 10 considered options at [`docs/Lessons-Learned/Button-Style-Mockups.md`](../../Lessons-Learned/Button-Style-Mockups.md). Standalone interactive viewer at the project root: [`button-mockups.html`](../../../../button-mockups.html).

## 12. Signal Contract Additions

The contract delta in `contracts/MCCCD-AA140.cce` (revised after the audio scope expansion + power modal + zoom + display power FB):

**Signals dropped vs. earlier design:**
- `NvxAutoSwitchSrc` — no longer needed since HDMI/USB-C merged into one logical "Laptop" source.

**New / updated signals:**

| Signal | Direction | Type | Purpose |
|---|---|---|---|
| `${ROOM_NAME}.D1MirrorToD3` | command | digital pulse | Mirror D1 source to D3 |
| `${ROOM_NAME}.D2MirrorToD3` | command | digital pulse | Mirror D2 source to D3 |
| `${ROOM_NAME}.Display3Source` / `.Display3SourceFb` | command + fb | analog | D3 source select (1–4) + feedback |
| `${ROOM_NAME}.Display1PowerFb` / `.Display2PowerFb` / `.Display3PowerFb` | feedback | digital | Each display's actual power state (driven by NVX D200 sink-connected feedback) |
| `${ROOM_NAME}.MicLavMute` / `.MicLavMuteFb` | command + fb | digital | Lav mute toggle |
| `${ROOM_NAME}.MicHandheldMute` / `.MicHandheldMuteFb` | command + fb | digital | Handheld mute toggle |
| `${ROOM_NAME}.MicCeiling1Mute` / `.MicCeiling1MuteFb` | command + fb | digital | TCCM ceiling 1 mute (settings only) |
| `${ROOM_NAME}.MicCeiling2Mute` / `.MicCeiling2MuteFb` | command + fb | digital | TCCM ceiling 2 mute |
| `${ROOM_NAME}.MicCeiling3Mute` / `.MicCeiling3MuteFb` | command + fb | digital | TCCM ceiling 3 mute |
| `${ROOM_NAME}.MicLavTrim` / `.MicLavTrimFb` | command + fb | analog | Lav input gain trim (0–100) |
| `${ROOM_NAME}.MicHandheldTrim` / `.MicHandheldTrimFb` | command + fb | analog | Handheld input gain trim |
| `${ROOM_NAME}.MicCeiling1Trim` / `.MicCeiling1TrimFb` | command + fb | analog | Ceiling 1 input gain |
| `${ROOM_NAME}.MicCeiling2Trim` / `.MicCeiling2TrimFb` | command + fb | analog | Ceiling 2 input gain |
| `${ROOM_NAME}.MicCeiling3Trim` / `.MicCeiling3TrimFb` | command + fb | analog | Ceiling 3 input gain |
| `${ROOM_NAME}.MicLavLineOut` / `.MicLavLineOutFb` | command + fb | analog | Lav line-out level |
| `${ROOM_NAME}.MicHandheldLineOut` / `.MicHandheldLineOutFb` | command + fb | analog | Handheld line-out level |
| `${ROOM_NAME}.MicCeiling1LineOut` / `.MicCeiling1LineOutFb` | command + fb | analog | Ceiling 1 line-out |
| `${ROOM_NAME}.MicCeiling2LineOut` / `.MicCeiling2LineOutFb` | command + fb | analog | Ceiling 2 line-out |
| `${ROOM_NAME}.MicCeiling3LineOut` / `.MicCeiling3LineOutFb` | command + fb | analog | Ceiling 3 line-out |
| `${ROOM_NAME}.MicLavLevel` | feedback | analog | Real-time level (0–100, ~10–30 Hz update) |
| `${ROOM_NAME}.MicHandheldLevel` | feedback | analog | |
| `${ROOM_NAME}.MicCeiling1Level` | feedback | analog | |
| `${ROOM_NAME}.MicCeiling2Level` | feedback | analog | |
| `${ROOM_NAME}.MicCeiling3Level` | feedback | analog | |
| `${ROOM_NAME}.MicLavConnected` | feedback | digital | Mic detected (Q-SYS signal-present or input level above noise floor) |
| `${ROOM_NAME}.MicHandheldConnected` | feedback | digital | |
| `${ROOM_NAME}.MicCeiling1Connected` | feedback | digital | |
| `${ROOM_NAME}.MicCeiling2Connected` | feedback | digital | |
| `${ROOM_NAME}.MicCeiling3Connected` | feedback | digital | |
| `${ROOM_NAME}.OccupancyState` | feedback | analog | 0=vacant, 1=occupied, 2=shutdown-pending |
| `${ROOM_NAME}.ShutdownCountdown` | feedback | analog | Minutes remaining |
| `${ROOM_NAME}.CamSendToVtc` | command | digital pulse | Set active cam as VTC ingest |
| `${ROOM_NAME}.CamTrackingMode` / `.CamTrackingModeFb` | command + fb | analog | 1=People, 2=Group, 3=VX AutoSwitch |
| `${ROOM_NAME}.ZoomIn` | command | digital level | Press-and-hold zoom in (rising = start, falling = stop) |
| `${ROOM_NAME}.ZoomOut` | command | digital level | Press-and-hold zoom out |
| `${ROOM_NAME}.SystemPowerFb` | feedback | digital | True when system is ON (drives the home Power button's enlarged variant) |

The seed contract already covers: power, source selects for D1/D2, audio output select, master volume up/down/mute/set, camera select, PTZ up/down/left/right, shot preset recall/save/delete, ISMI connect.

**Two-place contract maintenance:** every new signal added to `contracts/MCCCD-AA140.cce` must also be added to the `SIGNALS` object in `src/lib/contract.ts`. The `.cce` is the source of truth (Crestron Contract Editor builds it into `.cse2j` for the panel + `.g.cs` for SIMPL#); `src/lib/contract.ts` is the hand-maintained TypeScript mirror used by the Svelte components. Drift between the two will result in silent signal failures.

## 13. Out of Scope (v1)

- Lighting control (none specified in this room)
- Shade / blinds control (none specified)
- Advanced source/display matrix (the existing `App.multi-routing.svelte` layout — defer to v2)
- Fusion enterprise reporting
- VTC dialing UX (only the "Send to VTC" handoff — the actual VC system is downstream, not driven by this panel)
- Multi-zone audio (single program zone in this room)
- Per-panel UI variants (all panels run identical layout for now)
- Preset **delete** UI on Cameras page (saves overwrite for v1 — long-press-to-delete-with-confirm deferred to v2)
- Mic VU peak-hold + spectrum analyzer (the v1 level meter is a single-bar amplitude indicator)

## 14. Open Items (need confirmation before/during implementation)

- **Static IPs** for NVX devices (encoders + decoders), Q-SYS DSP, the three cameras, and the occupancy sensor
- **VLAN / routing** between 192.168.2.x (panels) and 192.168.1.x (processor) — likely L3 routed; confirm no firewall rules block CIP
- **1Beyond REST auth** — basic auth, token, or per-camera credentials? Will determine SIMPL# REST module config
- **Q-SYS Designer file** — which named components and controls the PA module exposes (program input, mic 1, mic 2, master out, etc.)
- **Mic types** — Lavalier and Handheld: wired or wireless (with receivers patched into DSP wired inputs)?
- **Initial preset positions** for each camera (DEFAULT, PRIMARY, SECONDARY)
- **Soft-shutdown timer** — proposed default 30 minutes; confirm
- **Occupancy on-trigger** — should occupancy auto-power-on always, or only during business hours? Need a schedule?

---

## 15. Implementation Plan Pointer

Once this design is approved, the next step is a detailed, ordered implementation plan covering: scaffold cleanup → page split (Home / Cameras / router) → contract editing in Crestron Contract Editor → SIMPL# project skeleton → NVX routing → Q-SYS PA module wiring → 1Beyond REST module → occupancy state machine → deploy + smoke test.

That plan is generated by the `writing-plans` skill in the next session step.
