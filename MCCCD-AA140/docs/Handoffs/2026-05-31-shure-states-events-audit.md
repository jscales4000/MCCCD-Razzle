# 2026-05-31 — Shure States/Events Audit (P300-IMX + MXA920W-S)

**Purpose:** Inventory every control the AA140 Shure gear *exposes* vs what the
processor *currently uses*, so we can decide which gaps are worth wiring. **Doc
only — no code changes in this pass** (per the device-integration spec, §6).

**Scope:** `ShureP300Service.cs` (273 ln, deep) and `ShureMxaService.cs` (mute/
identify/preset only). Both speak the Shure ASCII control protocol — `< ... >`
frames, TCP **2202**, `REP` push on change, `SAMPLE_*` meters when subscribed.

> **Token caveat:** the **Used** columns are exact (read from the code). The
> **Available** lists are the documented Shure IntelliMix command-string
> *categories*; confirm exact parameter tokens against the **Shure Command
> Strings PDF for the installed firmware** before wiring any of them.

---

## 1. Shure P300-IMX — used today

Channel map in code: `01` lav · `02` handheld · `03` MXA ceiling A · `04` MXA
ceiling B · `09/10` NVX D1/D2 audio (declared, not yet cross-pointed) · `17`
program out (analog 1 / room amp) · `21` automix out (declared).

| Command / event | Direction | Used for |
|---|---|---|
| `SET <ch> AUDIO_MUTE ON\|OFF` | out | mic mutes (01–04) + master (17) |
| `SET <ch> AUDIO_GAIN_HI_RES <v\|INC n\|DEC n>` | out | mic trim, line-out, program volume |
| `GET 00 ALL` | out | full state sync on connect |
| `SET METER_RATE_IN 00100` | out | 10 Hz input meters |
| `REP <ch> AUDIO_MUTE ON\|OFF` | in | mute feedback → panel |
| `REP <ch> AUDIO_GAIN_HI_RES <v>` | in | gain feedback → panel sliders |
| `SAMPLE_IN <ch> <0-100>` | in | input level meters → panel |

### P300 — available but NOT used
- **Matrix mixer cross-points** (`MATRIX_MUTE` / `MATRIX_GAIN <in> <out>`, exact
  token per Designer routing) — **this is the one functional gap**: the panel's
  `AudioOutputSelect` (D1 vs D2 audio → program bus) currently only **echoes**
  state; the real cross-point write is deferred "until the Shure Designer config
  is finalized" (`ShureP300Service.WirePanelSignals`). Until wired, selecting D1
  vs D2 audio does nothing on the DSP.
- **Output metering** (`METER_RATE_OUT` + `SAMPLE_OUT`) — a program-bus VU for the
  AudioMixer page.
- **Automix gate status** (`AUDIO_GATE_OUT` / automixer activity) — which mic is
  active → talker indication / camera hint.
- **Presets** (`PRESET` recall/store) — scene snapshots.
- **Device-level mute** (`DEVICE_AUDIO_MUTE`) — single hardware mute vs per-channel.
- **AEC / noise-reduction enable + status**, **`ENCRYPTION`**, **`FLASH`/identify**.
- **Inventory / health**: `MODEL`, `FW_VER`, `SERIAL_NUM`, `DEVICE_ID`, Dante /
  clock status (`DANTE_*`).

---

## 2. Shure MXA920W-S (×2) — used today

Most MXA audio (mute/gain/metering) flows through the **P300** since the arrays'
Dante outputs route there; the direct MXA link is for commissioning + source-side
concerns the P300 can't reach.

| Command / event | Direction | Used for |
|---|---|---|
| `SET 00 AUDIO_MUTE ON\|OFF` | out | global array mute (source-side) |
| `SET FLASH ON` | out | identify (commissioning) |
| `SET PRESET nn` | out | preset recall |
| `GET 00 ALL` / `GET MODEL` / `GET FW_VER` / `GET SERIAL_NUM` | out | baseline + inventory |
| `REP MODEL\|FW_VER\|SERIAL_NUM` | in | logged for inventory |

All other `REP` frames are **ignored**. No MXA-specific contract signals exist
yet (panel UI is P300-centric).

### MXA920 — available but NOT used
- **Coverage-lobe gate activity** (`AUDIO_GATE_OUT_<lobe>`) — per-zone "who's
  talking, where." High value for **camera auto-steer** (ties to 1Beyond VX
  AutoSwitch / tracking) and talker maps.
- **LED control** (`LED_STATE` / `LED_COLOR` / `LED_BRIGHTNESS`) — mute-state ring
  on the array; physical in-room mute indication.
- **Per-lobe / per-channel** `AUDIO_MUTE` / `AUDIO_GAIN` (vs the global `00`).
- **Auto-positioning / IntelliMix** enable + coverage status.
- **Per-lobe metering** (`METER_RATE` + `SAMPLE`), preset readback, encryption,
  `DEVICE_ID`.

---

## 3. Recommendations (priority — for a later decision)

| # | Gap | Device | Effort | Value | Notes |
|---|---|---|---|---|---|
| 1 | **Matrix cross-point for `AudioOutputSelect`** | P300 | M | **High** | Closes a real functional gap — D1/D2 audio select currently no-ops. Needs the finalized Shure Designer matrix block id. |
| 2 | **MXA LED mute indication** | MXA | L | Med-High | Physical mute feedback on the array ring; cheap UX win. |
| 3 | **MXA gate activity → camera hint** | MXA | M | High (VTC) | `AUDIO_GATE_OUT` per lobe feeds 1Beyond auto-switch/tracking. |
| 4 | **Program-bus VU** (`SAMPLE_OUT`) | P300 | L | Med | Master meter on AudioMixer page. |
| 5 | Presets / per-lobe gain / inventory surfacing | both | L–M | Low | Nice-to-have; defer. |

**Wiring cost note:** items 2–4 that need *panel* controls/feedback require new
`.cce` signals → Crestron Contract Editor build (same flow used for Display5 /
UsbHost this pass). Item 1 is processor-only (no contract change) but is blocked
on the Shure Designer cross-point id from the DSP programmer.

---

## 4. Sources
- Code: `MCCCD-AA140-SIMPL/MCCCD-AA140/ShureP300Service.cs`, `ShureMxaService.cs`,
  `ShureTcpClient.cs`.
- Protocol: Shure IntelliMix / MXA command-strings reference (confirm exact tokens
  against the PDF for the installed firmware before implementing).
