# MCCCD AA140 — Device IP Address Plan

**Room:** AA140 Main Conference Room
**Project:** MCCCD-AA140 Touchpanel (Archon `c1937681`)
**Source:** Equipment List (`MCCCD_AA140_Equipment_List.xlsx`, Proposal 10297 / PO DSTOF-100135103) cross-referenced against the IP/IPID values actually configured in the SIMPL# Pro backend and the CH5 panel.
**Generated:** 2026-06-05 — fill in the **Assigned IP (field)** column at commissioning, then reconcile the code.

> Scope: this is the **AA140 main room** only. The two satellite rooms are a separate Archon project ("MCCCD Conference Room") and have their own (much smaller) IP footprint — see the bottom note.

---

## How to read this

- **In-Code IP** = the value currently hard-coded in the backend service / panel config. It is the *intended* address, not a guarantee the device is actually there.
- **IPID** = how the Crestron processor binds the device over the control subnet. IPID devices (panels, NVX, USB-SW) **still need a real network IP** — the IPID is the logical binding, the IP is the L3 address.
- **Assigned IP (field)** = blank, for you to fill during commissioning.
- 🔴 = a conflict between two places in the code that must be resolved before commissioning.

---

## 1. Control System

| # | Device / Role | Model | Qty | IPID | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Control processor | RMC4 | 1 | host | **192.168.1.191** | | 🔴 `deploy.py` uses `.1.191`; `NVX-Routing-Lessons.md` says `.2.198`. Pick one. |
| 2 | Tabletop touchpanel | TS-1070-B-S | 1 | 0x03 | **192.168.2.80** | | Drifted over time: `.2.53` → `.1.175` → `.2.80` (`package.json deploy:tabletop`). |
| 3 | Wall touchpanel | TSW-1070-B-S | 1 | 0x04 | **192.168.2.78** | | Historically `.2.123` (`package.json deploy:wall`). |
| 4 | Room scheduling panel | TSS-1070-B-S | 1 | — | *(none)* | | Standalone scheduler — needs an IP; not referenced in code. |

> IPID note: `ControlSystem.cs` comment says `0x03 = TSW-1070 wall` / `0x04 = future panel`, which contradicts the panel memory (`0x03 = tabletop`, `0x04 = wall`). Confirm physical IPID strapping at the panel.

## 2. AV-over-IP — DM NVX (bound to processor by IPID, each needs an IP on the AV VLAN)

| # | Device / Role | Model | Qty | IPID | Multicast (video) | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|---|
| 5 | Encoder — Room PC (src 1) | DM-NVX-E30 | 1 | 0x11 | 239.8.0.0 | | |
| 6 | Encoder — Ext/Laptop PC (src 2) | DM-NVX-E30 | 1 | 0x12 | 239.8.0.4 | | |
| 7 | Encoder — AirMedia (src 3) | DM-NVX-E30 | 1 | 0x13 | 239.8.0.8 | | |
| 8 | Encoder — HDMI + USB-C combo (src 4) | DM-NVX-384 | 1 | 0x14 | 239.8.0.12 | | 🔴 Modeled in code as source 4 but **not on the equipment list** (BOM has 3× E30, no 384). Confirm it exists / is OFE. |
| 9 | Decoder — Display 1 | DM-NVX-D30 | 1 | 0x21 | — | | |
| 10 | Decoder — Display 2 | DM-NVX-D30 | 1 | 0x22 | — | | |
| 11 | Decoder — Display 3 | DM-NVX-D30 | 1 | 0x23 | — | | |
| 12 | Decoder — Display 4 (podium confidence) | DM-NVX-D30 | 1 | 0x24 | — | | |
| 13 | Decoder — Display 5 (signage, outside room) | DM-NVX-D30 | 1 | 0x25 | — | | Independently routable signage feed. |

## 3. Audio — Shure

| # | Device / Role | Model | Qty | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|
| 14 | Audio conferencing DSP | P300-IMX | 1 | **192.168.2.151** | | Consistent across `ShureP300Service.cs` + `DeviceConfigStore.cs`. |
| 15 | Ceiling array mic A | MXA920W-S | 1 | **192.168.2.181** | | |
| 16 | Ceiling array mic B | MXA920W-S | 1 | **192.168.2.182** | | |

## 4. Cameras — 1Beyond (VISCA over TCP 5500 + RTSP 554)

| # | Device / Role | Model | Qty | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|
| 17 | PTZ camera — Front | IV-CAM-I20-B (20x) | 1 | **192.168.2.172** | | 🔴 `CameraService.cs` = `.2.172`; panel `cameras.ts` = `.1.172`; memory = `.1.174`. Reconcile. Camera /24 control-source limit drove a `.1.x` plan. |
| 18 | PTZ camera — Back | IV-CAM-I12-B (12x) | 1 | **192.168.2.173** | | 🔴 `CameraService.cs` = `.2.173`; panel `cameras.ts` = `.1.172`; memory = `.1.173`. |

## 5. Wireless Presentation — AirMedia

| # | Device / Role | Model | Qty | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|
| 19 | AirMedia receiver | AM-3200-WF | 1 | **192.168.1.177** | | Consistent across `AirMediaService.cs` + `DeviceConfigStore.cs`. |
| — | Connect endpoint (table) | AM-TX3-200 | 1 | — | | Pairs to the AM-3200 receiver; no separate static IP normally required. |
| — | Connect adaptor | AM-TX3-100 | 1 | — | | Pairs to receiver; no separate static IP normally required. |

## 6. USB Routing

| # | Device / Role | Model | Qty | IPID | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|---|
| 20 | BYOD USB matrix switcher | USB-SW-400 | 1 | 0x31 | *(none)* | | Controlled SimplSharpPro IP device (`UsbSwitchService`). Hosts: Room PC / AirMedia / Laptop. Needs an IP. |

## 7. Network Infrastructure

| # | Device / Role | Model | Qty | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|
| 21 | Managed AV PoE switch | Netgear M4250 (GSM4230P) | 1 | *(none)* | | Needs a management IP; not referenced in code. |

## 8. OFE — Owner-Furnished but network-controlled (IPs live in code)

| # | Device / Role | Model | Qty | In-Code IP | Assigned IP (field) | Notes |
|---|---|---|---|---|---|---|
| 22 | Projector 1 | OFE (Sony VPL, per service) | 1 | **192.168.2.161** | | 🔴 `SonyVplService.cs` = `.2.161`; `DeviceConfigStore.cs` sony-1 = `.2.191` (disabled). |
| 23 | Projector 2 | OFE (Sony VPL, per service) | 1 | **192.168.2.162** | | 🔴 `SonyVplService.cs` = `.2.162`; `DeviceConfigStore.cs` sony-2 = `.2.192` (disabled). |
| 24 | Newline interactive display | OFE | 1 | **192.168.2.171** | | 🔴 `NewlineService.cs` = `.2.171`; `DeviceConfigStore.cs` newline = `.2.195` (disabled). |

---

## Devices that do NOT need an IP (for completeness)

- **PSU-MIDSPAN-USB-1** — passive USB PoE injector.
- **HD-CONV-USB-300** — USB converter (HDMI/analog-audio in).
- **C2G54265 / cables / extenders** — passive.
- **OFE amplifier, speakers, wireless mic system** — analog / owner-managed (unless the wireless mic system is networked — confirm with MCCCD).

---

## ⚠️ Action items surfaced by this audit

1. **Processor IP**: `.1.191` vs `.2.198` — pick the authoritative value and fix the loser.
2. **Camera IPs**: three different values across `CameraService.cs`, `cameras.ts`, and memory. The camera control-source `/24` limit means the processor and cameras must share a subnet — settle the `.1.x` vs `.2.x` decision first.
3. **Projector + Newline IPs**: dedicated service files disagree with `DeviceConfigStore.cs`. Align them.
4. **DM-NVX-384** (source 4) is in code but absent from the BOM — confirm the device exists or remove the source.
5. **Subnet split**: gear is spread across `192.168.1.x` (processor, AirMedia) and `192.168.2.x` (most AV) — confirm this is intentional (VLAN design) and not accidental drift.

---

## Satellite rooms (separate project, for reference)

Each of the 2 satellite rooms (tracked in Archon project "MCCCD Conference Room") needs IPs for: **RMC4** processor, **TS-770-B-S** touchpanel, **AM-3100-WF** AirMedia receiver, **UC-SB2-CAM-A-T** Videobar 70 (MTR). The AM-TX3-200 endpoint pairs to the receiver. No NVX/matrix (direct connections only).
