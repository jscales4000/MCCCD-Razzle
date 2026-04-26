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

## 4. Sources (5 logical, 4 encoders)

| # | Label | Encoder | IPID | Notes |
|---|---|---|---|---|
| 1 | Room PC | E30 | 0x11 | |
| 2 | Ext PC | E30 | 0x12 | |
| 3 | AirMedia | E30 | 0x13 | |
| 4 | HDMI | NVX-384 (HDMI input) | 0x14 | Shares stream with USB-C |
| 5 | USB-C | NVX-384 (USB-C input) | 0x14 | Shares stream with HDMI |

**Auto-switch behavior on NVX-384:** if the user selects HDMI but only USB-C is plugged in (or vice versa), the encoder auto-resolves to whichever physical input is active. The touchpanel shows the user-selected button as the "intended" source; SIMPL# subscribes to the NVX-384's active-input feedback and surfaces a small badge on the affected source button when actual ≠ selected (e.g., "HDMI selected · USB-C active").

## 5. Information Architecture

Three pages total:

1. **Home** *(default)* — display routing, audio, mics, occupancy, system power, link to Cameras
2. **Cameras** — single-cam preview, PTZ, presets, VTC ingest, tracking modes
3. **Settings** *(deferred to v2)* — VU meters, advanced source/display matrix, mic gain, system info

Page navigation uses a simple `currentPage` Svelte store. No router library.

## 6. Routing UX (Home Page)

**Layout:** three "display tiles" side-by-side in the main area (D1, D2, D3). Each tile contains:

- Display label and current source name (driven by feedback)
- 5 source buttons in a 3×2 grid: Room PC / Ext PC / AirMedia / HDMI / USB-C
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

**Signal flow:**

```
NVX-384 + E30 encoders ──AES-67──▶ Q-SYS Nano DSP ──line out──▶ dumb amp ──▶ ceiling speakers
                                       ▲
                       Lavalier mic ───┤  (wired analog inputs)
                       Handheld mic ───┘
```

**Audio-follows-display:** when the user toggles the speaker icon on D1 or D2, SIMPL# tells the Q-SYS PA module to switch its program audio source to the AES-67 stream from that display's currently routed encoder. D3 cannot own room audio.

**UI controls (home page footer):**
- Master program volume slider + mute
- Lavalier mic mute (with feedback indicator)
- Handheld mic mute (with feedback indicator)
- Audio-follows-display toggle is on the D1/D2 tiles (speaker icon)

VU meters and advanced mix controls live on the Settings page (deferred to v2).

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

**Presets row (bottom):** 3 preset slots per camera (DEFAULT, PRIMARY, SECONDARY) with Save / Recall / Delete buttons each, sent via REST.

## 9. Occupancy Automation

PoE occupancy sensor at IPID 0x41 reports occupancy as a digital feedback. SIMPL# state machine:

| Transition | Action |
|---|---|
| System off + Empty → Empty | no-op |
| Empty → Occupied | run system-on sequence: panels wake, displays power on, NVX last-active routes restored, audio unmute, D3 init from D2 |
| Occupied → Empty | start 30-minute soft-shutdown timer (configurable via SIMPL# constant) |
| Empty (timer running) → Occupied | cancel timer, no other action |
| Empty (timer expires) → Empty | run system-off sequence: displays off, source mute, audio mute |

**UI surface:** an occupancy pill in the home-page header with three states:
- Green "Occupied"
- Amber "Vacant"
- Red "Vacant — shutting down in N min" (only when timer is running, with live countdown from `ShutdownCountdown` analog feedback)

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

- **`src/pages/Home.svelte`** — composes from `layouts/App.dual-display.svelte` (per-display source picker pattern, audio toggle) plus a third display tile and per-tile mirror buttons. Source list trimmed to our 5 sources.
- **`src/pages/Cameras.svelte`** — adapted from `layouts/App.ptz-director.svelte`. Camera labels swapped to 1Beyond hardware (Front / Back-L / Back-R). Preview placeholder swapped for a real `ch5-video` element. Add: "Send to VTC" button, Tracking Mode radio group. Mic meters can stay as a nice-to-have driven by Q-SYS feedback.
- **`src/App.svelte`** — becomes a thin router that swaps between Home and Cameras based on a `currentPage` store.

The eight `layouts/*.svelte` files stay in the scaffold as reference but aren't loaded.

## 12. Signal Contract Additions

The seed contract has most of what we need. Additions to `contracts/MCCCD-AA140.cce` (build via Crestron Contract Editor — never hand-author the `.cse2j`):

| Signal | Type | Purpose |
|---|---|---|
| `${ROOM_NAME}.D1MirrorToD3` | digital pulse | Mirror button on D1 tile |
| `${ROOM_NAME}.D2MirrorToD3` | digital pulse | Mirror button on D2 tile |
| `${ROOM_NAME}.Display3Source` | analog | D3 source selection (1–5) |
| `${ROOM_NAME}.Display3SourceFb` | analog feedback | D3 active source |
| `${ROOM_NAME}.MicLavMute` | digital toggle | Lavalier mute (with feedback) |
| `${ROOM_NAME}.MicHandheldMute` | digital toggle | Handheld mute (with feedback) |
| `${ROOM_NAME}.OccupancyState` | analog feedback | 0=vacant, 1=occupied, 2=shutdown-pending |
| `${ROOM_NAME}.ShutdownCountdown` | analog feedback | Minutes remaining (UI shows in pill) |
| `${ROOM_NAME}.CamSendToVtc` | digital pulse | Set active camera as VTC ingest |
| `${ROOM_NAME}.CamTrackingMode` | analog | 1=People, 2=Group, 3=VX AutoSwitch |
| `${ROOM_NAME}.NvxAutoSwitchSrc` | analog feedback | NVX-384 actual active input (1=HDMI, 2=USB-C) |

The seed contract already covers: power, source selects for D1/D2, audio output select, master volume up/down/mute/set, mic mute (generic — split into Lav/Handheld in v1), camera select, PTZ up/down/left/right, shot preset recall/save/delete, ISMI connect, displayRoute_1..N (multi-routing — won't be used in v1 since we're per-tile), zone volume/mute, scene recall, lights toggle, record enable.

**Two-place contract maintenance:** every new signal added to `contracts/MCCCD-AA140.cce` must also be added to the `SIGNALS` object in `src/lib/contract.ts`. The `.cce` is the source of truth (Crestron Contract Editor builds it into `.cse2j` for the panel + `.g.cs` for SIMPL#); `src/lib/contract.ts` is the hand-maintained TypeScript mirror used by the Svelte components. Drift between the two will result in silent signal failures.

## 13. Out of Scope (v1)

- Lighting control (none specified in this room)
- Shade / blinds control (none specified)
- VU meters and advanced audio matrix (defer to Settings page in v2)
- Fusion enterprise reporting
- VTC dialing UX (only the "Send to VTC" handoff — the actual VC system is downstream, not driven by this panel)
- Multi-zone audio (single program zone in this room)
- Per-panel UI variants (all panels run identical layout for now)

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
