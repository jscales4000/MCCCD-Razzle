# 2026-06-03 — Design: Projector Screen Relay Control

**Branch:** `feat/screen-relay-and-view-modes` (off `feat/device-integration-usb-signage`)
**Status:** Approved design. Panel + processor work; requires one Contract Editor rebuild.

## Problem
The room has **two motorized projector screens**. The Sony projectors (the image
devices) are already controlled over IP in `SonyVplService`; the **screens** (the
roll-up fabric) have no control today. We want raise/lower from the panel and on
system power, using the RMC4's onboard relays.

## Hardware constraints (field-confirmed by integrator)
- RMC4 has **two onboard relay ports** — not four. Fully independent up/down for two
  screens would need four, which we don't have and don't need.
- Both screens move **together**. Each direction is paralleled onto one relay:
  - **Relay 1 = UP** (both screens' up trigger wired in parallel)
  - **Relay 2 = DOWN** (both screens' down trigger wired in parallel)
- Motion model: **momentary pulse → runs to limit.** A short contact closure
  triggers the screen controller to drive fully up/down and stop at its internal
  limit. No Stop control needed; re-pulsing the same direction mid-travel is a no-op.
- "Drive each one high to trigger" = momentary relay closure (`Close()` then `Open()`).

## Behavior
- **Manual:** Screen Up / Screen Down buttons on the **Display Routing page**
  (visible to all users — not gated behind Technician view).
- **Power tie-in:** integrated into `SystemPowerController`:
  - `PowerUpSequence()` → pulse **DOWN** (screens drop when the room turns on).
  - `PowerDownSequence()` → pulse **UP** (screens raise on shutdown).
- No position feedback — dry-contact relays cannot report screen position. Buttons
  are stateless momentary commands. Any active-direction highlight is client-side only.

## Architecture
A new **`ScreenRelayService`** is the sole owner of the RMC4 relay hardware, slotting
into `ControlSystem` exactly like the other device services (constructed in ctor,
`Initialize()` after CIPNet, registered with `DebugServer`).

### Processor (`MCCCD-AA140-SIMPL/MCCCD-AA140/`)
- **`ScreenRelayService.cs`** (new)
  - Constants: `RELAY_UP = 1`, `RELAY_DOWN = 2`, `PULSE_MS = 500` (tunable).
  - `Initialize()`: register `RelayPorts[1]`/`[2]` guarded by
    `_cs.SupportsRelay && _cs.NumberOfRelayPorts >= 2`. If relays are unavailable,
    log once and no-op all calls (graceful degradation, mirrors `UsbSwitchService`
    field-config stub pattern). Subscribe contract events `ScreenUp` / `ScreenDown`
    (rising edge only).
  - `ScreenUp()` / `ScreenDown()`: `Pulse(RELAY_UP|RELAY_DOWN)`.
  - `Pulse(idx)`: under a `CCriticalSection`, ignore if a pulse is already in flight
    (interlock); open the opposite relay first; close `idx`; arm a `CTimer` that opens
    `idx` after `PULSE_MS` and clears the in-flight flag. Guarantees the two relays
    are never closed simultaneously. Emits `DebugTrace.Command("screens", "up"|"down")`.
  - `TriggerFromDebug(key)`: `"up"`/`"down"` for the debug tool.
  - `Dispose()`: kill the CTimer on program stop.
- **`ControlSystem.cs`**: construct `_screens`, `Initialize()` it, pass to
  `SystemPowerController`, add to `DebugServer.Configure(...)`.
- **`SystemPowerController.cs`**: take `_screens` in ctor; `PowerUpSequence()` →
  `_screens.ScreenDown()`, `PowerDownSequence()` → `_screens.ScreenUp()`.

### Contract (`MCCCD-AA140/contracts/scripts/build_aa140_cce.py`)
Add to `PURE_COMMAND`: `("ScreenUp", 1), ("ScreenDown", 1)` (digital pulse, panel→proc
events). Re-run the script → regenerate `.cce`. **Fold into the pending Phase 4
Contract Editor rebuild** rather than triggering a second one. No `*Fb` signals.

### Panel (`MCCCD-AA140/src/`)
- **`lib/contract.ts`**: add `screenUp: ${ROOM_NAME}.ScreenUp`, `screenDown: …ScreenDown`.
- **`components/ScreenControl.svelte`** (new): Up / Down button pair, `pulseDigital`
  on tap, theme-compliant, optional client-side active-direction highlight (no signal).
- **`pages/DisplayRouting.svelte`**: add a "Projector Screens" sidebar section with the
  `ScreenControl` component (visible to all users).

## Error handling / safety
- **Never both relays closed:** every pulse opens the opposite relay first; an
  in-flight pulse blocks a new one until the CTimer fires (guards a fast Up→Down tap).
- **Re-pulse same direction** mid-travel: harmless (screen already running to limit).
- **No relay hardware:** logged once, calls no-op; debug trace still reflects intent.
- **Program stop:** CTimer disposed via the existing `OnProgramStatus` path.

## Testing
- `dotnet build` → 0 errors.
- Debug tool: trigger `screen/up` + `screen/down` → exactly one relay clicks and
  reopens after `PULSE_MS`; hammer both to confirm the interlock holds.
- Power: power-up pulses Down; shutdown pulses Up (watch relay LEDs / screens).
- Panel: Up/Down on the routing page fire the relays.
- Commissioning: confirm wiring (R1=up, R2=down, both screens paralleled) and that
  500 ms latches the controller — bump `PULSE_MS` if the screen ignores a short pulse.

## Out of scope
Independent per-screen control (no relay budget), screen position feedback (no sensor),
Stop control (pulse-to-limit needs none).
