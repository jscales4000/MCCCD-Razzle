# 2026-05-31 — Handoff: Button Up the Remaining Gear (Device Integration)

**Context.** The contract/feedback root cause is fixed and merged to `main` (commit
`753e638`): all panel I/O is name-based through the generated `Contract`, feedback
works end-to-end, the raw-join layer is gone. See
`2026-05-31-feedback-rootcause-lessons.md`. **This handoff is the next phase:**
integrate / enable / test the devices from the proposal that aren't yet live.

**Authoritative BOM:** `MCCCD_AA140_Equipment_List.xlsx` (American Sound Proposal
10297 / PO DSTOF-100135103, signed 2026-05-19). 91 line items; the AA140 main room
is this codebase. Satellite rooms are separate Teams Rooms (Videobar 70) projects —
**out of scope here.**

---

## Device-by-device status (AA140 main room)

| Device (BOM) | Qty | Service | Status | What's left |
|---|---|---|---|---|
| DM-NVX-E30 encoders | 3 | `NvxRoutingService` | ✅ Live (routing + HDMI sync dots verified) | — but see **NVX discrepancy** below |
| DM-NVX-D30 decoders | **5** | `NvxRoutingService` | ✅ 4 wired (D1–D4) | **5th D30 unaccounted for** — see discrepancy |
| Shure P300-IMX (DSP) | 1 | `ShureP300Service` (273 ln, TCP) | ✅ Enabled, deep (mute/trim/lineout/levels) | Verify against real P300 once on net |
| Shure MXA920W-S arrays | 2 | `ShureMxaService` (103 ln, TCP) | ⚠️ **Thin — mute-only** | Decide if per-lobe / preset / LED control needed |
| 1Beyond IV-CAM-I12-B / I20-B | 2 | `CameraService` (233 ln) | ✅ Enabled (PTZ, zoom, presets, tracking, VTC) | Verify on real cameras |
| AM-3200-WF AirMedia | 1 | `AirMediaService` (234 ln, REST) | ✅ Enabled (start/stop + sharing-method dots) | Verify AM-TX3-200/100 endpoints pair |
| **OFE Sony projectors** | 2 | `SonyVplService` (247 ln, TCP) | 🔴 **Coded but DISABLED** (`sony-1`/`sony-2` enabled=false, stub IPs .191/.192) | **Get real IPs, enable, test power + HDMI input** |
| **OFE Newline display** | 1 | `NewlineService` (239 ln, TCP) | 🔴 **Coded but DISABLED** (`newline` enabled=false, stub .195) | **Get real IP, enable, test power/input/vol** |
| **USB-SW-400** 4-in USB matrix | 1 | *(none)* | 🔴 **NOT integrated** | Decide if BYOD USB routing needs control; if so, add a service |
| HD-CONV-USB-300 (HDMI→USB) | 1 | *(none)* | ⚪ Passive | Likely no control needed; confirm |
| PSU-MIDSPAN-USB-1 | 1 | — | ⚪ Passive power | No control |
| TSS-1070 scheduling panel | 1 | *(separate)* | ⚪ Out of scope | Room-scheduling app, not this processor |
| Netgear GSM4230P switch | 1 | — | ⚪ Infra | No control |

Legend: ✅ live · ⚠️ partial · 🔴 needs work · ⚪ no control needed/out of scope

---

## ⚠️ NVX hardware discrepancy — ✅ RESOLVED 2026-05-31

> **RESOLVED:** The integrator/user confirmed a **DM-NVX-384 IS on the TX side** as source 4
> (HDMI in1 / USB-C in3, internal auto-switch) — it just wasn't itemized on the signed proposal
> (treat as a change/substitution). The `NvxRoutingService` 384 model is **correct**. The **5th
> D30 = signage display outside the conference space** (now wired as D5). USB BYOD is handled by
> the **USB-SW-400** host switch, separate from NVX video. Full design + implementation:
> `docs/superpowers/specs/2026-05-31-device-integration-usb-signage-design.md` and
> `docs/superpowers/plans/2026-05-31-device-integration-usb-signage.md`. The original analysis
> below is kept for history.

The signed BOM lists **3× DM-NVX-E30 + 5× DM-NVX-D30, and NO DM-NVX-384.** The code
(`NvxRoutingService.cs`) models **4 encoders (3× E30 + 1× NVX-384 for "Laptop") + 4
decoders (D1–D4)**. (The auto-memory note claiming the NVX-384 is correct contradicts
the signed xlsx — trust the xlsx.) Two things to reconcile with the rack drawing /
integrator:

1. **How is the "Laptop" (source 4) actually delivered?** There is no 4th NVX encoder
   on the BOM. Candidates: the **USB/BYOD path** (HD-CONV-USB-300 + USB-SW-400, *not*
   NVX), an HDMI feed into an existing encoder, or a direct display input. If Laptop is
   NOT an NVX stream, `_encHdmiUsbc = new DmNvx384(...)` and the source-4 routing are
   modeling hardware that isn't there.
2. **What are the 5 D30 decoders?** Only 4 displays exist (2 projectors, Newline,
   podium confidence = D1–D4). The **5th D30** may feed the USB capture / recording /
   streaming path, or be a spare. Map it before claiming routing is complete.

Until this is resolved, `NvxRoutingService` source-4 + decoder count are **assumptions.**

---

## Concrete tasks for the next session

### 1. Sony projectors (highest value — they're the main displays)
- Get the two projectors' real IPs (BOM says OFE Sony, TCP/IP control). Replace stub
  `.191/.192` and set enabled via the debug tool:
  `POST /cws/aa140/debug/devices/sony-1?host=<ip>&enabled=true` (same for `sony-2`).
- `SonyVplService` already implements `PowerOn/Off`, `SelectHdmi1/2` (Sony PJ Talk-style
  `power "on"` / `input "hdmi1"` text protocol — verify the exact command syntax + port
  against the actual projector model; constant is `SonyVplService` top).
- Test from the debug panel's Sony buttons; confirm power + input switching.
- Wire projector power into `SystemPowerController.PowerUpSequence`/`PowerDownSequence`
  if not already (room on/off should drive projectors).

### 2. Newline interactive display
- Get real IP, enable `newline`. `NewlineService` implements power/input/volume/mute.
- Verify command protocol + port against the actual Newline model.
- Decide which display "slot" the Newline maps to (it's one of D1–D4 sink-side).

### 3. USB-SW-400 BYOD decision
- Determine whether BYOD USB routing (laptop ↔ room camera/audio/DSP) must be
  **controlled** or is auto/manual. If controlled, add a `UsbSwitchService` (check
  Crestron SDK support for USB-SW-400; it may be a Cresnet/IP device or
  REST-controlled). This ties to the Laptop-source question above.

### 4. Shure MXA920 depth
- Confirm scope: the P300 handles conferencing DSP/AEC; the MXA arrays typically need
  only mute + preset recall + LED/identify. `ShureMxaService` currently does **mute
  only**. Add preset/LED if the room design calls for it.

### 5. Verify the already-enabled devices on real hardware
- P300, MXA, cameras, AirMedia are enabled with stub-ish IPs. Once the gear is on the
  network, confirm each connects (debug `/events` stream shows connect lifecycle) and
  the feedback is real (not stale).

---

## How to work device config (no redeploy needed for IP/enable changes)

Device IPs + enabled flags are runtime, persisted to `/user/aa140/devices.json`
(`DeviceConfigStore`). Change them live via the CWS debug tool:

```bash
# enable a device + set its IP (service rebinds immediately):
curl -k -s -X POST --data "" \
  "https://192.168.2.198/cws/aa140/debug/devices/sony-1?host=192.168.2.x&enabled=true"
# read current config:
curl -k -s "https://192.168.2.198/cws/aa140/debug/devices"
```
Device keys: `p300, mxa-a, mxa-b, sony-1, sony-2, newline, airmedia, cam-1, cam-2`.
Defaults baked in `DeviceConfigStore.Defaults()`. `enabled=false` keeps a service idle
(no connect spam) until the real device is on the net.

Code changes (new service, command tweaks) DO need a rebuild + redeploy:
```bash
# processor
cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=192.168.2.198 python scripts/deploy.py MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz
# panel (if UI changes)
cd MCCCD-AA140 && npm run deploy:both
```

---

## Open questions to take to the integrator / rack drawing
1. Laptop source path: NVX-384 (not on BOM), USB/BYOD, or direct HDMI? (Drives NVX code.)
2. What does the 5th DM-NVX-D30 feed?
3. Real IPs for: 2× Sony projectors, Newline display, and confirm P300/MXA/camera/AirMedia IPs.
4. Is USB-SW-400 BYOD routing control-driven or manual?
5. MXA920 control scope beyond mute?
6. Does room power on/off need to drive the projectors + Newline (CEC/IP)?

---

## Quick pickup recipe
```bash
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle"
git checkout main && git log --oneline -1   # expect 753e638 merge
# confirm processor + panels reachable
curl -kI https://192.168.2.198/cws/aa140/debug/   # debug tool (device status)
curl -k  https://192.168.2.198/cws/aa140/debug/devices   # current device config
# read this handoff + the BOM
start MCCCD_AA140_Equipment_List.xlsx
```
Start with **the NVX discrepancy** (it gates routing correctness), then **Sony
projectors** (main displays, quickest win once IPs are known).

## Reference
- BOM: `MCCCD_AA140_Equipment_List.xlsx` (3 sheets: Full / Crestron-only / Notes)
- Contract fix post-mortem: `docs/Handoffs/2026-05-31-feedback-rootcause-lessons.md`
- Device config: `MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DeviceConfigStore.cs`
- Services: `*Service.cs` in `MCCCD-AA140-SIMPL/MCCCD-AA140/`
