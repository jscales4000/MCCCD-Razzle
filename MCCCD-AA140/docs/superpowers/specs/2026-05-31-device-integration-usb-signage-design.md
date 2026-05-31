# 2026-05-31 — Design: USB Switching, Signage Decoder, Debug Health/Ping, Shure Audit

**Status:** approved design (brainstorming output) — ready for implementation plan.
**Supersedes the open questions in:** `docs/Handoffs/2026-05-31-device-integration-handoff.md`.
**Codebase:** AA140 main room — `MCCCD-AA140-SIMPL` (processor, C#) + `MCCCD-AA140` (CH5 panel, Svelte).

---

## 0. Context & what the user clarified

The handoff left several "take to the integrator" questions open. The user answered them,
which **resolves the so-called NVX discrepancy** and adds real new work:

| Question (handoff) | Answer | Consequence |
|---|---|---|
| Laptop source path? NVX-384 (not on BOM), USB, or HDMI? | **NVX-384 on the TX side.** Input 1 = HDMI, Input 3 = USB-C. | Existing `NvxRoutingService` source-4 model (`DmNvx384`) is **already correct**. The "trust the xlsx, there's no 384" call was wrong — fix the docs, no code change. |
| What does the 5th DM-NVX-D30 feed? | **Signage display outside the conference space.** | Add a 5th decoder (D5), independently switchable. |
| Is USB-SW-400 routing control-driven or manual? | **Controlled, one-tap.** A dedicated panel **host selector** (Room PC / AirMedia / Laptop); picking a host instantly routes the room's USB peripherals (camera + Shure) to it. No signal auto-detect. | New `UsbSwitchService` + new Contract signals + advanced-routing UI. |
| MXA920 control scope beyond mute? | **Audit only this pass.** Produce a states/events doc; decide later. | Doc deliverable, no Shure code change. |

Transmitters (confirmed): **E30 ×3** = Room PC (src 1), Ext PC (src 2), AirMedia (src 3);
**NVX-384** = src 4 (HDMI in1 / USB-C in3, internal auto-switch).
Receivers: **D30 ×5** = D1–D4 (in-room displays incl. podium confidence D4) + **D5 (signage)**.

USB hosts on the USB-SW-400: **Room PC, AirMedia, Laptop (USB-C off the 384)**. The room's
USB peripherals (camera + Shure mic/speaker) are physically downstream of the switch's device
ports and follow the selected host.

### USB-SW-400 control facts (from Crestron doc 9403, "USB 3.2 Data Matrix Switcher")
- "USB-SW-400 is identical" to the USB-SW-200 except port count.
- Control = **IPID peer-to-peer to the control system** (configurable IP ID + Room ID, optional
  encryption, ONLINE/OFFLINE status) → modeled like the NVX gear: a SimplSharpPro device on a
  static IPID, **not** a `devices.json` IP entry.
- It is a matrix: "select a cell to route a device to that host." Hardware **Auto-Route** toggle
  exists but stays **OFF** — we drive routing explicitly from the panel.
- The exact SimplSharpPro device class name is confirmed at implementation time (same pattern as
  the "verify command syntax against the real device" TODOs already in `SonyVplService` /
  `NewlineService`).

---

## 1. Scope

Four work items, one design, **one phased implementation plan**. Shipped in a single `.cce`
regeneration + processor/panel deploy pass (except the doc-only items).

1. **D5 signage decoder** — independently routable, like D4.
2. **USB-SW-400 host switching** — `UsbSwitchService` + advanced-routing UI (**mockup-first**).
3. **Debug panel: real health/ping + save confirmation** — fixes the "cameras look online but
   aren't" confusion.
4. **Shure states/events audit** — documentation only.

Plus a **docs fix** (item 0): correct the stale memory/handoff claim that there is no NVX-384.

### Out of scope (already satisfied / unchanged)
- 3rd-party persistent IPs: `DeviceConfigStore` → `devices.json` already persists host+enabled
  for p300, mxa-a/b, sony-1/2, newline, cam-1/2. Sony/Newline still need real IPs + `enabled=true`
  via the debug panel — that's an operational step, not new code.
- The name-based Contract / feedback layer (fixed and merged in `753e638`).

---

## 2. Item 0 — Docs fix (NVX-384)

- Update the auto-memory note `reference_mcccd_aa140_equipment.md` (currently says "no NVX-384"):
  the NVX-384 **is** present on the TX side as source 4 (HDMI in1 / USB-C in3). The code was right.
- Add a one-line correction to the handoff's "NVX discrepancy" section pointing here.
- No code change.

---

## 3. Item 1 — D5 signage decoder

### Architecture
A decoder is a pure receiver: it pulls one of the existing source stream URLs (1–4). No new
encoder, no new multicast. D5 is D4-shaped but maps to its own display index.

### Changes — `NvxRoutingService.cs`
- Add `IPID_D30_DISP5 = 0x25` (next free after D4 `0x24`).
- Add `_decDisp5`, register it, `WireDecoderOnline(_decDisp5, 5)`, extend `GetDecoder(5)`.
- **Bump fixed-size arrays** `_pendingUrl` and `_rxConfigured` from `[5]` to `[6]` (they index by
  display number 1..5; current size only covers 1..4). Extend the reapply loop bound
  `for (d = 1; d <= 4)` → `<= 5` in `ReapplyRoutesForSource`.
- `RouteSourceToDisplay` already guards src 0..4; add the `case 5:` feedback write
  (`Display5SourceFb`).

### Changes — `SystemPowerController.cs`
- Wire `Display5Source` (panel→proc) → `RouteSourceToDisplay(v, 5)` + write `Display5SourceFb`.
- Power sequencing: treat D5 like D4 — **PowerDown clears it; PowerUp seeds it to none (src 0)**.
  Rationale: D5 shows an in-room NVX source on an *outside* display; showing a dead room-PC stream
  while the room is off is wrong. The user routes signage explicitly when wanted. (If a default
  signage source is desired later, this is the one line to change.)

### Changes — debug
- Extend `/nvx/route` validation from `dec 1..3` to `dec 1..5` (currently rejects >3; D4/D5 should
  be testable from the debug tool too).

### Contract / panel
- New signals `Display5Source` (command, panel→proc) + `Display5SourceFb` (feedback, proc→panel).
- Panel: the advanced-routing view already stubs "Display 5" / "Outside" buttons — wire them to
  the new signal (see Item 2 UI).

---

## 4. Item 2 — USB-SW-400 host switching

### Architecture
New `UsbSwitchService` owns the USB-SW-400 as a Crestron SimplSharpPro device on a static IPID
(`0x31`, conflict-free: panels 0x03/0x04, encoders 0x11–0x14, decoders 0x21–0x25). It exposes a
single room-wide operation:

```
SelectHost(UsbHost host)   // host ∈ { RoomPC=1, AirMedia=2, Laptop=3 }
```

which sets the matrix so the **device ports (camera + Shure USB)** route to the chosen **host
column**. Camera/Shure need no per-device code — they are physically downstream of the switch.
Auto-Route stays OFF. Online/offline lifecycle → `DebugTrace.Lifecycle` (like NVX), so the debug
panel shows USB-SW ONLINE/OFFLINE.

Host→input-port indices are named constants (calibrated to the physical wiring at commissioning).

### Decoupling & power
- The USB host selector is **independent of per-display video routing** (the user rejected
  "USB follows source selection" — a single room-wide host is cleaner than tying USB to one of
  five independently-routed displays).
- **PowerUp → `SelectHost(RoomPC)`** + `UsbHostSelectFb = 1` (the in-room PC is the default driver).
- **PowerDown → no USB change** (don't strand a laptop that's mid-call when the room cycles); the
  Fb always reflects the actual host.

### Changes
- New `UsbSwitchService.cs`: register device, `Initialize()` wires `UsbHostSelect` (panel→proc)
  event → `SelectHost()` + writes `UsbHostSelectFb`. Online lifecycle tracing.
- `ControlSystem.cs`: construct, `Initialize()`, and pass into `DebugServer.Configure(...)`.
- `SystemPowerController.cs`: hold a `UsbSwitchService` ref; call `SelectHost(RoomPC)` in
  `PowerUpSequence`.
- Debug: new `POST /usb/host/<roompc|airmedia|laptop>` → `SelectHost(...)` for bring-up.
- No `DeviceConfigStore` entry (IPID device, like NVX). No enable gate.

### Contract / panel
- New signals `UsbHostSelect` (command, ushort 1/2/3) + `UsbHostSelectFb` (feedback, ushort).
- UI lives in **advanced routing** (`App.multi-routing.svelte`) as a host-selector region.

### Mockup-first (gate before any .cce / Svelte build)
- Produce an HTML mockup in `mockups/` (style consistent with `mockups/18-drag-drop-router.html`)
  showing the advanced-routing layout **with** the USB host selector + D5/signage routing.
- **User approves the mockup before** the `.cce` regen and panel build in Phase C.

---

## 5. Item 3 — Debug panel: real health/ping + save confirmation

### Problem
The debug row conflates two unrelated things, which is why cameras read "online" but do nothing:
- The text label (`enabled`/`disabled`) is **just the config flag** (`debug.js` `renderDevices`),
  not connectivity.
- The colored badge is real online/offline but derived **only from `/events` lifecycle messages**.
  TCP devices emit `device_connected`; **`CameraService` and AirMedia are stateless REST and emit
  no lifecycle events**, so their badge stays "unknown" and only "enabled" shows.

`enabled=true` only means *an IP is configured*. It never checks reachability, nor whether the
camera's `cgi-bin/*` endpoints match the real 1Beyond API.

### Solution — transport-appropriate reachability probe
- Backend `POST /devices/<key>/ping` performs a short-timeout probe on a worker thread:
  - **TCP devices** (p300, mxa-a/b, sony-1/2, newline) → TCP connect to `host:port`. Success =
    reachable (proves the *control port* is open — more meaningful than ICMP).
  - **REST devices** (cam-1/2, airmedia) → HTTP GET to the device root. Any HTTP response
    (incl. 401/404) = reachable (the box answered).
  - Returns `{ key, reachable: bool, detail: "<code|connected|timeout|refused>" }`.
- Frontend (`debug.js` / `debug.html` / `debug.css`):
  - Each device row gets a **Ping button** and a **tri-state indicator** that separates:
    **config** (enabled/disabled) · **reachable** (ping result) · **online** (service-connected,
    from `/events`).
  - Slow **auto-ping (~10 s)** in addition to the manual button.
  - **"saved ✓" flash** on IP `change` (the POST already persists immediately; this just confirms
    it visibly) and echo the server-returned host back into the field.
  - Surface the latest camera command HTTP result (already logged via `DebugTrace.Error`) in the
    camera row, so "reachable but commands 4xx" is distinguishable from "unreachable."

### Note on the camera root cause
Reachability is necessary but may not be sufficient: the 1Beyond `cgi-bin/ptz|preset|tracking|
vtc-ingest` paths in `CameraService` are assumed and may not match the real camera API. The health
probe + surfaced HTTP codes make this **diagnosable**; correcting the actual endpoints (if wrong)
is a follow-up once we can see real responses, not part of this spec.

---

## 6. Item 4 — Shure states/events audit (doc only)

Deliverable: `docs/Handoffs/2026-05-31-shure-states-events-audit.md`.
- Enumerate every **available** P300-IMX and MXA920 ASCII command/param/event (from the Shure
  command spec) vs what `ShureP300Service` / `ShureMxaService` **currently use**.
- Flag gaps and what they'd buy: e.g. MXA `AUDIO_GATE_OUT_*` coverage-zone activity, LED/identify,
  preset recall (already coded, unwired to a contract); P300 states not surfaced to the panel.
- **No code changes.** Output is a recommendations list for a later decision.

---

## 7. Contract / `.cce` regeneration (the risk surface)

Four new signals:

| Signal | Direction | Type | attributeType (corrected) |
|---|---|---|---|
| `Display5Source` | panel → proc (command) | analog/ushort | 1 = Event = command = event |
| `Display5SourceFb` | proc → panel (feedback) | analog/ushort | 0 = State = feedback = setter |
| `UsbHostSelect` | panel → proc (command) | analog/ushort | 1 = Event = command = event |
| `UsbHostSelectFb` | proc → panel (feedback) | analog/ushort | 0 = State = feedback = setter |

- **Apply the corrected direction encoding** per `reference_cce_direction_encoding`: `attributeType
  0 = State = feedback (proc→panel) = setter`; `1 = Event = command (panel→proc) = event`. The FRED
  guide is backwards; encoding these the wrong way is exactly what caused the prior dead-feedback
  bug. This is the make-or-break step.
- Regenerate `Contract.g.cs` from the updated `.cce`.
- Verify the four new methods/events exist on `_contract.AA140` after regen.

---

## 8. Deploy

```
# processor
cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=192.168.2.198 python scripts/deploy.py MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz
# panels (both)
cd MCCCD-AA140 && npm run deploy:both
```
(Per `feedback_panel_deploy_workflow`: both panels always pushed.)

---

## 9. Testing

- **D5:** `POST /nvx/route?dec=5&src=N` → signage shows source N; panel "Display 5/Outside" selector
  routes; power-cycle clears (down) and seeds none (up).
- **USB:** `POST /usb/host/<h>` → the USB-SW-400 web UI shows the device→host column move and the
  camera/Shure enumerate on the chosen host; panel host selector + `UsbHostSelectFb` track; PowerUp
  defaults to Room PC; `USB-SW ONLINE` appears in `/events`.
- **Debug health:** Ping reports reachable/unreachable correctly per device; a camera with a wrong
  IP shows **unreachable**, a reachable camera with bad endpoints shows **reachable + HTTP 4xx**
  (no longer a false "online"). IP-save flash confirms persistence.
- **Feedback round-trip (dead-feedback regression):** `Display5SourceFb` and `UsbHostSelectFb`
  drive the panel markers correctly after the `.cce` regen.

---

## 10. Phased implementation plan (outline — detailed plan via writing-plans)

- **Phase A — Debug health/ping + save confirmation.** No `.cce`, no panel redeploy; processor +
  embedded debug UI only. Immediately useful for bringing up Sony/Newline/cameras on real hardware.
- **Phase B — Advanced-routing mockup (GATE).** HTML mockup of advanced routing with USB host
  selector + D5 signage. **User approval required before Phase C.**
- **Phase C — `.cce` regen + D5 decoder + `UsbSwitchService` + panel UI + deploy.** The single
  build/deploy pass. Includes the corrected direction encoding and full hardware verification.
- **Phase D — Shure states/events audit doc.** Independent; can run in parallel with A/B.

---

## 11. IPID map (after this work)

| Device | IPID |
|---|---|
| TSW-1070 primary / secondary | 0x03 / 0x04 |
| E30 RoomPC / ExtPC / AirMedia | 0x11 / 0x12 / 0x13 |
| NVX-384 (HDMI+USB-C) | 0x14 |
| D30 D1–D4 | 0x21–0x24 |
| **D30 D5 (signage)** | **0x25** |
| **USB-SW-400** | **0x31** |

---

## 12. Open / confirm-at-implementation
- Exact SimplSharpPro device class for the USB-SW-400 (USB 3.2 Data Matrix Switcher) and its
  route API shape (`Route(device, host)` vs per-column set).
- USB-SW-400 host-input port indices vs physical wiring (Room PC / AirMedia / Laptop).
- Whether the SDK USB device exposes ICMP-free reachability we can fold into the health probe (else
  TCP/HTTP probe stands).
