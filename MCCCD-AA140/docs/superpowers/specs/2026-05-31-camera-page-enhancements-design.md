# 2026-05-31 — Design: Camera Page Enhancements (framing, USB switch, zoom, coordinates)

**Status:** approved design (brainstorming output) — ready for implementation plan.
**Builds on:** the VISCA `CameraService` rewrite (cameras live-verified: PTZ/zoom/tracking/presets).
**Mockup:** `mockups/20-camera-page.html`.

---

## 0. Context & camera roles

Two Crestron 1Beyond IV-CAMs, live and controllable over VISCA TCP 5500:

| Panel name | Device | IP | Role |
|---|---|---|---|
| **Presenter** | IV-CAM-**I20** | `192.168.2.174` (cam-1) | Pointed at the presenter; does **presenter tracking + preset zones + tracking profiles** (I20-only features). |
| **Group** | IV-CAM-**I12** | `192.168.2.173` (cam-2) | **USB host** to the room PC/codec; frames **participants** and does **Intelligent Switching** (I12-only). Does NOT do group *tracking*. |

Authoritative feature/slot map: Crestron docs 9440 (mirrored at `1Beyond/docs/Reverse-Engineering/Crestron-IVCAM-Reserved-Presets.md`). Pollability empirically confirmed (`IVCAM-Inquiry-Probe-Findings.md`).

---

## 1. Goal

Rebuild the Cameras page so an operator can: see/switch both streams (done), drive PTZ + **prominent zoom**, recall/save shots, engage **presenter tracking** (I20) with live state, drive the **Q&A / USB-output switch** (I12), pick **preset zones / tracking profiles** (I20), and read **live Pan/Tilt/Zoom coordinates** of the selected camera.

---

## 2. Feature → VISCA command mapping

All feature commands are `CAM_Memory Recall` (`81 01 04 3F 02 NN FF`) to a reserved slot, targeted at a **specific camera by role** (not the "selected" camera, except PTZ/zoom/user-presets/home/tracking-shot/coords which follow selection).

| Control | Slot(s) | Target cam | State source |
|---|---|---|---|
| **Presenter Tracking** on/off | `80` / `81` | **I20** (cam-1) | **Live poll** `CAM_TrackingInq` → `02`=on / `03`=off |
| **USB Output: Presenter** | `86` | **I12** (cam-2) | cached (no VISCA readback) |
| **USB Output: Group** | `85` | **I12** | cached |
| **USB Output: Auto (Q&A / IS)** | `84` | **I12** | cached |
| **Preset Zone 1–4** | `101`–`104` | **I20** | cached (radio) |
| **Tracking Profile 1–4** | `105`–`108` | **I20** | cached (radio) |
| **Home Shot** | `0` | selected cam | momentary recall |
| **Tracking Shot** | `1` | selected cam | momentary recall |
| User shot presets 1–3 (existing) | `109`–`111` | selected cam | recall/save |

Group Tracking (`82`/`83`, I20-only) is **available but not surfaced** by default — the I20's role here is presenter tracking. Easy to add later. OSD (`95`) / Reboot (`99`) intentionally excluded.

**Coordinates (selected cam):** `PanTiltPosInq` `81 09 06 12 FF` → signed pan/tilt (4-nibble each); `ZoomPosInq` `81 09 04 47 FF` → unsigned zoom 0x0000–0x4000. Displayed as **raw VISCA values** (exact; degree tables for IV-CAM are undocumented).

---

## 3. ViscaCameraClient — add inquiry support

The streamlined `ViscaCameraClient` is fire-and-forget today. Add **minimal serial inquiry polling** (cameras allow 1 socket; inquiries must not pipeline):

- **Poll loop** (CTimer, ~333 ms) when connected: send `PanTiltPosInq`, on reply send `ZoomPosInq`, on reply send `CAM_TrackingInq` (the latter every ~Nth cycle, ~1 Hz is plenty). One inquiry in flight at a time; a `_pendingInquiry` enum tags which reply to expect.
- **Reply parsing** (port from ISMIv3 `ViscaProtocol`): `ParsePanTiltPosPayload` (8 bytes → short pan, short tilt), `ParseZoomPosPayload` (4 bytes → ushort zoom), `ParseTrackingActive` (1 byte → bool). The reply categorizer already exists; extend it to expose `InquiryResponse` payload bytes.
- **Exposed properties:** `short PanPosition`, `short TiltPosition`, `ushort ZoomPosition`, `bool TrackingActive` (updated from replies under the state lock). Keep the existing connect-lifecycle + reply-logging.
- A command (PTZ/preset) sent mid-poll just enqueues ahead — but since the panel is human-paced, the simplest correct approach is: inquiries and commands share one "send + await reply" path. Commands already fire-and-forget; to avoid colliding with an in-flight inquiry, gate command sends to "no inquiry pending, else send anyway and drop the stale inquiry reply." (Detailed in the plan.)

---

## 4. New Contract signals

The framing/USB/zones/profiles/coords controls need new name-based signals. **Retire** `CamTrackingMode` / `CamTrackingModeFb` (People/Group/VX — superseded).

| Signal | Direction | Type | Meaning |
|---|---|---|---|
| `CamPresenterFraming` | cmd (panel→proc) | digital | desired presenter-tracking state (true=on→80, false→81) on I20 |
| `CamPresenterFramingFb` | fb (proc→panel) | digital | **polled** tracking state (I20 `CAM_TrackingInq`) |
| `CamUsbOutput` | cmd | analog | 1=Presenter(86) · 2=Group(85) · 3=Auto(84) on I12 |
| `CamUsbOutputFb` | fb | analog | echoed (cached) 1/2/3 |
| `CamPresetZone` | cmd | analog | 1–4 → I20 101–104 |
| `CamPresetZoneFb` | fb | analog | echoed 1–4 |
| `CamTrackingProfile` | cmd | analog | 1–4 → I20 105–108 |
| `CamTrackingProfileFb` | fb | analog | echoed 1–4 |
| `CamHomeShot` | cmd | digital | pulse → recall 0 on selected cam |
| `CamTrackingShot` | cmd | digital | pulse → recall 1 on selected cam |
| `CamPanPos` | fb | analog | selected cam pan (raw 16-bit as ushort; panel reinterprets signed) |
| `CamTiltPos` | fb | analog | selected cam tilt (raw 16-bit as ushort) |
| `CamZoomPos` | fb | analog | selected cam zoom (0–16384) |

Existing kept: `CameraSelect`, `Ptz{Up,Down,Left,Right}`, `Zoom{In,Out}`, `ShotPresetRecall/Save`, `CamSendToVtc`. **One `.cce` regen + Contract Editor build** covers all of the above (manual gate; same flow as Display5/UsbHost).

Signed-position note: pan/tilt are signed 16-bit; the contract analog is unsigned 0–65535. Processor casts `(ushort)(short)pan`; the panel reinterprets values >32767 as negative (`v - 65536`).

---

## 5. Processor — CameraService changes

- **Role constants:** `CAM_PRESENTER = 1` (I20), `CAM_GROUP = 2` (I12) — framing/zone/profile commands target these regardless of `_active`.
- Wire new contract events:
  - `CamPresenterFraming` → I20 recall 80 (true) / 81 (false).
  - `CamUsbOutput` (1/2/3) → I12 recall 86 / 85 / 84; echo `CamUsbOutputFb`.
  - `CamPresetZone` (1–4) → I20 recall 100+n; echo Fb.
  - `CamTrackingProfile` (1–4) → I20 recall 104+n; echo Fb.
  - `CamHomeShot` / `CamTrackingShot` → recall 0 / 1 on `_active` cam.
- **Coordinate publisher:** a CTimer (~333 ms) writes the **active** camera's `PanPosition`/`TiltPosition`/`ZoomPosition` to `CamPanPos`/`CamTiltPos`/`CamZoomPos`. On `CameraSelect`, immediately repush the newly-active cam's values.
- **Tracking feedback:** when the I20's `TrackingActive` changes (from its poll), write `CamPresenterFramingFb`.
- Remove the old `CamTrackingMode` handler + `SetTrackingMode`.
- Debug endpoints: extend `/cam/<id>/...` with `framing?on=`, `usb?out=`, `zone?n=`, `profile?n=`, `home`, `tracking-shot` for bring-up.

---

## 6. Panel — Cameras.svelte rebuild (per mockup)

Layout: header · work-area `[camera select | preview+coords | controls]` · bottom `[shot presets | zones | profiles]`.

- **Camera select:** Presenter (I20) / Group (I12), with online + (Presenter) tracking badge.
- **Preview:** ch5-video (existing element-replacement on select) + PTZ overlay; **coordinates bar** below: `Pan / Tilt / Zoom` from `CamPanPos/CamTiltPos/CamZoomPos` (panel converts pan/tilt to signed), with a "live" indicator.
- **Controls:** **prominent Zoom** (large ＋/− press-hold) · **Presenter Tracking** toggle (`CamPresenterFraming`, lit from `CamPresenterFramingFb`) · **USB Output** segmented Presenter/Group/Auto (`CamUsbOutput`, lit from `CamUsbOutputFb`).
- **Bottom:** Shot Presets 1–3 (recall/hold-save) + **Home** + **Tracking Shot**; **Preset Zones 1–4** + **Tracking Profiles 1–4** (radio, lit from Fb) — **shown only when the Presenter/I20 is selected** (model-aware).
- New signal names in `contract.ts`; new feedback stores in `signals.ts`.
- Remove the old tracking-mode (People/Group/VX) buttons + `camTrackingModeFb` usage.

---

## 7. Testing

- **Unit-ish (build):** processor `dotnet build`; panel `npm run build`.
- **Live (debug + panel):**
  - `CAM_TrackingInq` poll: toggle Presenter Tracking → `CamPresenterFramingFb` flips (verify in `/events` + panel badge).
  - USB Output Presenter/Group/Auto → I12 receives recall 86/85/84 (ACK/Completion in `/events`); `CamUsbOutputFb` lights the segment.
  - Zones/Profiles → I20 recall 101–108 (ACK/Completion); radio lights; **hidden when Group/I12 selected**.
  - Coordinates: PTZ the camera → `CamPanPos/Tilt/Zoom` update live on the panel; values change with movement; switching cameras swaps the readout.
  - Home / Tracking Shot recalls fire on the selected cam.
- **Deadlock/contention:** confirm inquiry polling doesn't starve PTZ commands (PTZ stays responsive while coords update).

---

## 8. Deploy
`dotnet build` → `.cpz` to `192.168.2.198`; `npm run deploy:both`. (Camera signals + page; one processor + both-panel push after the Contract Editor build.)

---

## 9. Open / confirm-at-implementation
- Exact inquiry poll cadence vs PTZ responsiveness (tune in testing; start 333 ms position, 1 Hz tracking).
- Whether to also surface I20 Group Tracking (82/83) — deferred per role split.
- Coordinate display unit (raw VISCA confirmed; degrees deferred — no IV-CAM conversion table).
