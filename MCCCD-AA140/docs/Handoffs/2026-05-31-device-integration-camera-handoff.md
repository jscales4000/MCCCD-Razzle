# 2026-05-31 — Handoff: Device Integration + Camera Page (branch `feat/device-integration-usb-signage`)

**Worktree:** `.worktrees/device-integration-usb-signage/` (git worktree, branch `feat/device-integration-usb-signage`, branched from `main` @ `0f463cd`). 33 commits, nothing uncommitted. **Not merged.**

**Processor:** RMC4 @ `192.168.2.198` (admin/password). **Panels:** TS-1070 `.80`, TSW-1070 `.78`.
**Debug tool:** `https://192.168.2.198/cws/aa140/debug/` (device config, ping/health, per-device command routes, `/events` stream).

**Build/deploy status (2026-06-01):** Contract Editor gate **DONE** — `CamGroupFraming/Fb`, `CamActiveOutput/Fb`, `CamPanSpeed/Tilt/ZoomSpeed/Fb` all built (direction verified), processor **builds 0 errors**, processor + both panels **deployed**. Everything below is live except the one open mapping item.

---

## ⏭ IMMEDIATE NEXT STEP — fix the multicam Front/Back ↔ camera mapping

**Symptom (reported):** on the unified left **Multicam selector** (Front/Back), selecting a feed does not give control of the right camera / show the right feed. The multicam VISCA commands themselves are verified working (SetCameraOutput 1↔2 + GetCameraOutput feedback all ACK) — this is purely a **mapping config** in `MCCCD-AA140/src/lib/cameras.ts`, panel-only (no contract/processor change, no Contract Editor pass).

**Two independent mappings to verify (run the identify test, then set in `cameras.ts`):**

1. **Control/preview (`selectIndex` + `ip` + label):** which physical camera each button controls (PTZ/preset/coords) and previews. Today: `Front = .2.174 (I20, selectIndex 1)`, `Back = .2.173 (I12, selectIndex 2)`.
2. **USB output number (`outputIndex`):** which feed the I12 host's `SetCameraOutput(N)` puts on USB. Today: `Front → outputIndex 1`, `Back → outputIndex 2` — **host internal numbering may be reversed.**

**Identify test (re-runnable; cameras wiggle + output cycles so you can watch):**
```python
# from repo root — wiggles each cam (returns to center) + cycles USB output 1->2
python - <<'PY'
import socket,time
def C(h): s=socket.socket();s.settimeout(3);s.connect((h,5500));return s
def drv(s,p,t,sec): s.sendall(bytes([0x81,1,6,1,0x0C,0x0A,p,t,0xFF]));time.sleep(sec);s.sendall(bytes([0x81,1,6,1,1,1,3,3,0xFF]));time.sleep(.3)
def wig(h,l): print(l);s=C(h);drv(s,1,3,1.5);drv(s,2,3,1.5);s.close();time.sleep(1)
def out(h,n): s=C(h);s.sendall(bytes([0x81,0xC2,1,8,n,0xFF]));time.sleep(.4);s.recv(32);s.close()
wig('192.168.2.174','.2.174 (I20/Front) wiggling — which physical cam moves?')
wig('192.168.2.173','.2.173 (I12/Back) wiggling — which physical cam moves?')
print('output=1'); out('192.168.2.173',1); time.sleep(4)
print('output=2'); out('192.168.2.173',2); time.sleep(4)
PY
```
**Then fix in `cameras.ts`:** if the *wrong physical camera responds to PTZ* → swap `ip`/`selectIndex`/`label` between the two `CAMERAS` entries. If the *wrong feed shows on the USB output* → swap the two `outputIndex` values. (They're independent — you may need one, the other, or both.) Rebuild panel + `npm run deploy:both` (no processor build needed).

**Contract encoding rule (for any future signal):** `attributeType 0 = State = feedback (proc→panel) = setter`; `1 = Event = command (panel→proc) = event`. Edit `build_aa140_cce.py`, re-run it, then a Contract Editor Build. **Never hand-edit `.g.cs`/`.cce`.**

---

## Status by feature

| Feature | State |
|---|---|
| **USB-SW-400 host switching** (`UsbSwitchService`, IPID 0x31) | ✅ built/deployed/command-verified. Panel host selector in `App.multi-routing`/DisplayRouting. **Field-config:** confirm SDK device class + physical host-port wiring (`ApplyRoute` TODO). |
| **D5 signage decoder** (IPID 0x25) | ✅ built/deployed/command-verified (`/nvx/route?dec=5`). Independently routable; cleared on power-down. **Field:** D5 decoder + signage display must be on the net. |
| **Debug ping/health** (`DeviceProbe`) | ✅ live. TCP-connect probe (p300/mxa/sony/newline/**cam 5500**) + HTTP probe (airmedia). Tri-state config/reachable/online + save-confirm. |
| **Shure states/events audit** | ✅ doc only (`docs/Handoffs/2026-05-31-shure-states-events-audit.md`). Gaps listed (matrix cross-point, MXA LED/gate) — not implemented. |
| **Cameras — VISCA rewrite** | ✅ **live-verified.** IV-CAMs speak VISCA TCP 5500, NOT HTTP. `Visca/ViscaProtocol.cs` + `ViscaCameraClient.cs`. PTZ/zoom/presets/tracking all ACK. |
| **Cameras — RTSP stream on panel** | ✅ working. `rtsp://admin:Password1!@<ip>:554/1.h264` (Digest; **NOT** admin/crestron). Stream switches by replacing the `ch5-video` element. |
| **Camera page v2** | ✅ built (deployed through the prior gate): 2 cameras, prominent zoom, presenter-tracking toggle (live fb), framing-output switch, live coordinates + **zoom ratio 1×–20×**, I20 zones/profiles, Home/Tracking-shot. |
| **Camera page — multicam + framing + speed** | ✅ built/deployed (gate done): **I20 Group-Tracking toggle**, reliable presenter feedback, **multicam output switch** (`SetCameraOutput`/`GetCameraOutput`), **PTZ speed sliders** (pan 1-24/tilt 1-20/zoom 0-7), zoom-ratio readout. |
| **Camera page — unified Front/Back multicam selector** | ✅ deployed; ⚠️ **mapping unverified** — left selector replaced with Front/Back; selecting switches USB output + PTZ/preset control + preview + live badge. **See IMMEDIATE NEXT STEP** to verify/fix the Front/Back↔IP↔output# mapping in `cameras.ts`. |

---

## Camera control reference (VISCA, port 5500) — what's wired

**Cameras:** Presenter = **IV-CAM-I20** `192.168.2.174` (cam-1); Group/host = **IV-CAM-I12** `192.168.2.173` (cam-2). Both moved to `.2.x` so the processor's VISCA is accepted (IV-CAMs restrict control to their local `/24`). RTSP creds `admin/Password1!`.

| Control | Command | Target | Feedback |
|---|---|---|---|
| PTZ drive | `81 01 06 01 VV WW pp tt FF` (speed VV/WW from sliders) | selected cam | — |
| Zoom | `81 01 04 07 2p/3p FF` (p from slider) | selected cam | — |
| Coordinates | `PanTiltPosInq`/`ZoomPosInq` poll | selected cam | CamPanPos/Tilt/Zoom |
| Presenter tracking | recall 80/81 | **I20** | **polled** `CAM_TrackingInq` → CamPresenterFramingFb |
| Group tracking | recall 82/83 | **I20** | cached |
| Framing output (IS) | recall 86/85/84 = Presenter/Group/Auto | **I12** | cached (CamUsbOutputFb) |
| **Active camera (multicam)** | **`81 C2 01 08 0N FF`** (N=1–5, `SetCameraOutput`) | **I12** host | **`81 C2 09 08 FF` → `90 50 00 0N FF`** polled → CamActiveOutputFb (works in Auto too) |
| Preset zones / profiles | recall 101–104 / 105–108 | **I20** | cached |
| User shots | recall/save 109–111 | selected cam | — |
| Home / Tracking shot | recall 0 / 1 | selected cam | — |
| PTZ speed | applied to drive cmds | global | CamPan/Tilt/ZoomSpeedFb |

**Key sources:** `1Beyond/docs/Reverse-Engineering/Crestron-IVCAM-{VISCA-Commands,Reserved-Presets}.md` (slot map), `IVCAM-Inquiry-Probe-Findings.md` (pollability). The **`81 C2 …` multicam command was extracted from the legacy `Simpl/ISMIv2.clz` (`SetCameraOutput`) via ildasm** and verified live — it is NOT in the public VISCA doc.

**Not pollable** (cached only): group tracking, framing-output IS mode, zones, profiles. **Pollable:** presenter tracking, pan/tilt/zoom position, active-output camera.

---

## Known field-config / open items

- **Cameras on `.2.x`:** the IV-CAMs only accept VISCA from their own `/24`. They're at `.2.174/.2.173` so the processor (`.2.198`) can reach them. If moved, processor must share their `/24`.
- **Preset Zones return `Error 0x41`** until zones are *defined on the camera* (web UI). Profiles ACK fine.
- **Up to 5 cameras:** the Active-Camera row shows buttons 1–5; only cameras physically configured on the I12 host respond. Room has 2 today.
- **Audio + displays offline:** P300/MXA/Sony/Newline are not on the net at their default IPs — audio page core is wired but untestable until present. AudioMixer master-fader/scenes/link/connected have **no processor handler** yet (see Shure audit).
- **"Send to VTC":** logged-only — it's an AV-routing action, no VISCA equivalent (needs the camera→codec routing path designed).
- **USB-SW-400 `ApplyRoute`** + exact SDK class: confirm at commissioning.

---

## Pickup recipe

```bash
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.worktrees/device-integration-usb-signage"
git log --oneline -1            # expect: feat(panel): unify left selector into Front/Back multicam switch

# IMMEDIATE: fix the Front/Back mapping (see IMMEDIATE NEXT STEP). Edit cameras.ts, then PANEL ONLY:
cd MCCCD-AA140 && npm run deploy:both     # no processor build/Contract Editor needed for the mapping fix

# Full build + deploy (only if processor/contract changed):
cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=192.168.2.198 python scripts/deploy.py MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz
cd ../MCCCD-AA140 && npm run deploy:both

# Debug routes for camera bring-up (POST with --data ""):
B=https://192.168.2.198/cws/aa140/debug
curl -k -s -X POST --data "" "$B/cam/2/output?n=1"          # multicam SetCameraOutput(1) on I12 host
curl -k -s -X POST --data "" "$B/cam/1/group-framing?on=true"
# PTZ speed: drag sliders on the panel; verify smoother/faster moves.
```

**Specs/plans:** `docs/superpowers/specs/2026-05-31-{device-integration-usb-signage,camera-page-enhancements}-design.md`, `docs/superpowers/plans/2026-05-31-{device-integration-usb-signage,camera-page-enhancements}.md`. Mockups: `mockups/19`, `mockups/20`.

**To finish the branch:** all builds green; once the gate + deploy + camera test pass, it's ready to merge to `main` (or PR) — a large, self-contained body of work (USB/D5/debug + full camera control).
