# Lessons Learned & Best Practices — Sony VPL RS‑232 Control over DM‑NVX COM (SIMPL# / 4‑Series)

**Scope:** Controlling Sony VPL laser projectors (verified on **VPL‑FHZ90L**, applies to PHZ/FHZ data‑projector line) over **RS‑232** when the serial line is the **COM port on a DM‑NVX‑D30 decoder**, driven by a 4‑Series processor (RMC4). Written for the FRED SIMPL# KB.
**Source:** MCCCD‑AA140, commissioning 2026‑06‑26/27. `SonyVplService.cs` rewritten from ADCP‑over‑TCP to ADCP‑over‑RS‑232.

---

## TL;DR — the keeper lessons
1. **Sony VPL ADCP serial = `38400, 8 data, EVEN parity, 1 stop, no flow` (8‑E‑1).** Not 8‑N‑1. This was the entire bug: wrong parity silently garbles every byte **in both directions**, so you get *zero* reply on any protocol or baud, even with perfect wiring. **Set parity first; don't chase cables until serial params are proven.**
2. **ADCP runs over RS‑232 on PHZ/FHZ** — the **same ASCII text commands as ADCP‑over‑LAN** (`power "on"`, `power "off"`, `input "hdmi1"`, `power_status ?`), **CR+LF** terminated, **no auth** on serial (the `NOKEY`/SHA256 handshake is LAN‑only). You do **not** need the legacy binary `A9…9A` protocol.
3. **A DM‑NVX‑D30's COM port is a first‑class serial transport** for the control system: `decoder.ComPorts[1]` after the device registers. One decoder per projector (D1→proj1, D2→proj2) gives you N serial ports without a serial expander.
4. **`SetComPortSpec` must be (re)applied *after* the NVX decoder is online**, with a **settle gap before transmitting**. Boot‑time config silently fails to "stick" if applied while the decoder is still coming up; back‑to‑back `SetComPortSpec`→`Send` drops the byte.
5. **A service can be 100% functional and still do nothing** if it isn't wired to the panel. `SystemPowerController` controlled NVX/screens/audio but had no projector reference, so the power button never reached the projectors. **Check the contract→service wiring, not just the driver.**

---

## The final working solution
**Serial spec (per projector COM):** `38400‑8‑E‑1`, no HW/SW flow control, RS‑232 protocol.
**Transport:** `NvxRoutingService` exposes `Disp1ComPort`/`Disp2ComPort` (`_decDispN.ComPorts[1]`, resolved lazily — null until the decoder registers + is online). `SonyVplService` owns the ADCP command layer over those ports.
**Commands (ASCII, `\r\n`):** `power "on"` / `power "off"` / `input "hdmi1"` / `input "hdmi2"` / `power_status ?` / `lamp_timer ?`. Replies: `ok`, `err_*`, or a quoted value (`"on"`, `"standby"`, `"warming"`, `"cooling"`).
**Liveness/boot self‑heal:** a per‑projector poll timer (~12 s) that, until a reply is seen, re‑asserts `SetComPortSpec(38400‑8‑E‑1)` then (after an ~0.8 s settle) sends `power_status ?`; once online it keeps polling for live power feedback. This makes the room come up correctly after a cold boot with no manual step.
**Panel power integration:** `SystemPowerController` holds the `SonyVplService` and calls `PowerAllOn()` in `PowerUpSequence` / `PowerAllOff()` in `PowerDownSequence` (triggered by the `DisplayPower` contract pulse from the panel power button).

**Verified on glass:** `power_status → "on"`; room power‑off → both projectors `ok` → `"standby"`; room power‑on via the panel sequence → both `ok` → `"on"`; self‑configures after a program restart.

---

## Diagnostic methodology — what actually isolated the fault
Do this in order; each step removes a whole class of cause. The instrumentation (below) is what made it fast.

1. **Build status visibility into the service.** A debug endpoint returning per‑device `portResolved` / `ready` / `online` / `rx`(byte count) / `last`(reply) was the single highest‑value tool. It instantly distinguished "COM not resolving" from "configured but no reply."
2. **Separate TX‑works from RX‑works.** Add a **raw received‑byte counter** (incremented on *any* serial data, before line parsing). `online`/`last` only update on a *parsed* line; a byte counter catches replies with unexpected terminators and proves whether *anything* came back.
3. **Loopback at the NVX COM** (jumper TX↔RX on the decoder's terminal block). If a sent string echoes back (rx = bytes sent), the **NVX COM port itself transmits and receives** — isolates the controller from the cable/projector entirely. This proved our COM was fine while the projector stayed silent.
4. **Sweep the serial parameters as a matrix, not one at a time.** Protocol (ADCP text vs binary) × **parity (N/E/O)** × baud, all read against the byte counter. The reply appeared *only* at 8‑E‑1 — a single‑variable sweep that didn't include parity would have missed it.
5. **Loopback at the *far* (projector) end of the cable** to split "cable broken" from "projector not answering" — do this *before* concluding the projector is at fault.
6. **Confirm the panel→service wiring** once the driver works in isolation (we drove it via a debug `/sony/...` route long before the panel could).

**What this prevented:** a TX/RX cable swap that chased the wrong fault (it was parity), and blaming the projector when the controller was mis‑framed.

---

## What did NOT work (dead ends, documented so nobody repeats them)
- **8‑N‑1 (no parity)** — the original code default. Silent garble, zero reply, on every baud and both protocols. The trap: everything *looks* right (port ready, bytes transmitted) and the loopback *passes* (TX and RX share the same wrong setting), so the fault hides until you talk to the real device.
- **8‑O‑1 (odd parity)** — also nothing. Only EVEN works for this projector.
- **Legacy binary protocol** (`A9 17 2E … 9A`) — got no reply; FHZ90 serial speaks ADCP, not the old binary frames. Don't reach for binary on the laser line.
- **Swapping TX/RX at the NVX end** — no effect (the issue was parity, not polarity). Swapping un‑broke nothing and risks breaking a known‑good cable.
- **Baud sweep alone** — necessary but insufficient; the variable that mattered was parity.

---

## SIMPL# RS‑232 best practices (reusable)
- **Use the `ComPort` object**, not raw sockets. `device.ComPorts[1]` for endpoint‑hosted ports (NVX, DM); `cs.ComPorts[1]` for the processor's own. Register the parent device first; access the COM lazily (it's null until the device is online).
- **`SetComPortSpec(...)` is the contract** — get all of baud/data/**parity**/stop/protocol/handshake right. Parity is the most commonly‑wrong one for AV gear; **read the device's protocol manual for the exact framing** (Sony VPL = 8‑E‑1).
- **Re‑apply the spec after the endpoint comes online**, and **insert a short settle (~0.5–1 s) before the first transmit** after any `SetComPortSpec`. Back‑to‑back set+send drops bytes on NVX COM.
- **Poll for liveness and feedback** — serial has no "connected" state. A periodic status query both keeps the link healthy and gives the panel real device feedback (power/lamp). Track `online` from replies.
- **Instrument from day one:** expose `portResolved/ready/online/rx/last` and a raw‑send (`hex`) + set‑baud/parity debug route. It turns a multi‑hour mystery into a matrix sweep.
- **Thread safety:** guard shared serial state with a lock; never block in a `CTimer`/event callback (use a one‑shot timer for the settle delay instead of sleeping).
- **Wire the service to the contract.** A working driver is invisible until the panel events / power sequence call it. Pass the service into `SystemPowerController` (or whatever orchestrates room power) and verify the on/off path end‑to‑end.

---

## Crestron ops / console gotchas hit during this work
- **`err` paginates over SSH** → `exec_command` truncates to the first page. Use an interactive shell and page through, or build an HTTP/CWS status surface instead (we did the latter).
- **FIPS / Forced‑Auth lockout:** rapid repeated SSH connections lock the admin account; a wrong password after a password change compounds it. A **reboot clears it**. Back off SSH and prefer the CWS HTTP debug surface for iterative testing.
- **`proginfo -p:NN` is rejected on this firmware** — the deploy script's verify step errors harmlessly after a successful `PROGLOAD`; don't treat it as a failure.
- **CWS POST needs a Content‑Length** — send an empty body (`curl --data ""`) or you get `411 Length Required`. Params can ride the query string.
- **Standby still answers serial.** A projector in standby replies to `power_status ?` (`"standby"`); only fully‑unpowered (no AC) is silent — useful for "is it the cable or the projector" reasoning.

---

## Quick reference
| Item | Value |
|---|---|
| Projector | Sony VPL‑FHZ90L (PHZ/FHZ line) |
| Serial spec | **38400, 8, EVEN, 1, no flow** |
| Terminator | CR+LF (`0x0D 0x0A`) |
| Protocol | ADCP (ASCII text), no auth on serial |
| Transport | DM‑NVX‑D30 `ComPorts[1]` (D1/D2 decoders) |
| Power on / off | `power "on"` / `power "off"` |
| Status query / replies | `power_status ?` → `"on"`/`"standby"`/`"warming"`/`"cooling"` |
| Input select | `input "hdmi1"` / `input "hdmi2"` |
| Files | `SonyVplService.cs`, `NvxRoutingService.cs` (`Disp1/2ComPort`), `SystemPowerController.cs` |
