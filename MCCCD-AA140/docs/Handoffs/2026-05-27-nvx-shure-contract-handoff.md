# 2026-05-27 — NVX bring-up + Shure swap + Contract Editor rebuild

**State at end of session**: program runs cleanly on RMC4 192.168.2.198. All 9 CIPNet IPIDs ONLINE. Encoders streaming on multicast. Decoders initialize correctly. **Panel source-button taps don't trigger route changes yet** because the panel publishes via raw direct joins, not via the SmartObject-based Contract Editor output we just generated.

Revert point: git tag `checkpoint-nvx-live-contract-rebuild` (commit `fed4a38c`).

---

## What we learned about NVX

### The receiver-side sig allocation problem

When we first wired `DmNvxD30` decoders, every write to `Control.ServerUrl.StringValue`, `Control.MulticastAddress.StringValue`, and `Control.SessionInitiation` threw `InvalidOperationException: Attempt to set StringValue of NullSig` (or `UShortValue of NullSig` for `SessionInitiation`).

Encoders worked: `_enc.Control.DeviceMode = Transmitter` and `_enc.Control.MulticastAddress.StringValue = "239.8.0.x"` set cleanly in `OnlineStatusChange`. Decoders failed even when `DeviceModeFeedback` confirmed the device was in Receiver mode and `BaseEvent` had fired 8+ times.

### What didn't work (recipes from Crestron docs / agent research)

Both major research passes pointed at the same recipe, sourced from Crestron's published help.crestron.com and the SIMPL# Engineer persona:

```csharp
dec.Control.DeviceMode = eDeviceMode.Receiver;
dec.Control.SessionInitiation = eSessionInitiation.ByReceiver;
dec.Control.EnableAutomaticInitiation();
// triple-set: ServerUrl + MulticastAddress + VideoSource=Stream
```

Every variation of this failed:
- Set after `OnlineStatusChange` — NullSig
- Set before `Register()` — NullSig
- Set in `BaseEvent` retry loop, gated on `DeviceModeFeedback == Receiver` — NullSig forever
- Set via CTimer with 5-second backoff — NullSig
- Reflection-based `SessionInitiation` write to dodge a CS0433 type-collision — NullSig on the underlying UShort sig

### What actually works — PepperDash's pattern

Found via the PepperDash open-source NVX plugin (`github.com/PepperDash/epi-crestron-nvx`, file `src/NvxEpi/Services/Utilities/DeviceDefaults.cs`):

```csharp
dec.OnlineStatusChange += (device, args) => {
    if (!args.DeviceOnLine) return;
    dec.Control.Name.StringValue = "NVX-D" + displayNum;
    dec.Control.EnableAutomaticInitiation();   // <-- THE MISSING CALL
    // Now ServerUrl, MulticastAddress, etc. become valid sigs
    var pending = _pendingUrl[displayNum];
    if (!string.IsNullOrEmpty(pending)) {
        dec.Control.ServerUrl.StringValue = pending;
    }
};
```

Three things both Crestron and the persona had wrong:

1. **Do NOT write `Control.DeviceMode = Receiver` on a D3x.** The D30 is hardware-locked as a receiver. PepperDash explicitly skips this with `if (device is not DmNvxD3x)`. Writing it confuses the SDK proxy and prevents sig allocation.
2. **Do NOT touch `SessionInitiation`.** PepperDash never writes it. The canonical replacement is `EnableAutomaticInitiation()` — that method is what allocates the receiver-side string sigs from NullSig.
3. **Do NOT write `Control.VideoSource = Stream`** on a D3x. It's a TX-side selector (HDMI / DM-NAX / stream-source for an encoder). On a stream-only D30 it throws or no-ops; lumping it into the same try block was masking other failures.

The fix landed in `NvxRoutingService.WireDecoderOnline()`. Log proof:
```
NVX D1: ONLINE — receiver initialized via EnableAutomaticInitiation()
NVX route: D1 <- rtsp://239.8.0.0:554/live.sdp
```

### Other NVX-side learnings

- D30s need their own IP table populated (RMC4 IP at each NVX's IPID slot 0x21/0x22/0x23). User did this via D30 web UI; took ~5 min total. Once done, CIP came up immediately.
- Encoder transmitters: `Control.MulticastAddress.StringValue` works the moment `OnlineStatusChange` fires. No `EnableAutomaticInitiation` needed.
- The `DmNvx384` combo unit is registered as a transmitter at IPID `0x14` and emits at `239.8.0.12`. Auto-switches between HDMI and USB-C inputs internally; the "both plugged → user picks" UX is not yet wired (needs new contract signals).
- Multicast block: `239.8.0.{0,4,8,12}` for encoder video. AES67 NAX audio rides the adjacent ODD addresses (`.1,.5,.9,.13`) but the property path for NAX TX hasn't been wired yet (`Control.SecondaryAudio.MulticastAddress` is the candidate).
- D30 web-UI subscriptions work without any C# routing — useful as a fallback for testing.

---

## What we resolved this session

| Issue | Resolution |
|---|---|
| RMC4 192.168.2.198 had firmware 2.8000.00003 — too old for net6.0 SIMPL# Pro (looking for `SimplSharpPro.exe`, not `.dll`) | User upgraded to 2.8006.00284 via Toolbox |
| Forced Auth Mode + FIPS Mode were on — would have blocked NVX CIP joins | (Still on per `ver -all`, but TSW connects fine; NVX devices connected too once their IP tables pointed at the new RMC4) |
| `DmNvxD30` receiver-side sigs NullSig forever despite all documented recipes | PepperDash pattern — single `EnableAutomaticInitiation()` call in `OnlineStatusChange`, skip `DeviceMode`/`SessionInitiation`/`VideoSource` for D3x |
| Q-SYS → Shure P300-IMX pivot | Replaced `QsysAudioService` with `ShureP300Service` + `ShureMxaService` + reusable `ShureTcpClient`. TCP transport works, panel wiring stubbed |
| AirMedia AM-3200 had no Crestron CIPNet class | New `AirMediaService` using `HttpsClient` against `https://192.168.1.177/Device/...` (TLS handshake from Windows is finicky — `Crestron.SimplSharp.Net.Https` works on-processor) |
| Occupancy sensor removed (2 MXA920 design, no PoE OIR) | Deleted `OccupancyController.cs` and all wiring |
| Panel UI showed 5 mic strips, only 4 mics in equipment list | `AudioMixer.svelte` rebuilt with 4 strips: Lav + Handheld + Array A (MXA920W-S) + Array B |
| `MainContract.cs` was a stub (Contract Editor never built) | User built the `.cce`. Generated `Contract.g.cs` / `Main.g.cs` / `ComponentMediator.g.cs` / `UIEventArgs.g.cs` swapped into `Generated/` directory. Build clean. |
| TSW-1070 model was logged as "TS-1070" (tabletop) in memory | Corrected to TSW-1070 (wall mount), IP 192.168.2.78, IPID `0x03` |

---

## What is still broken

### Panel source-row buttons don't trigger route changes — the central remaining problem

**Symptom**: User taps source buttons on the TSW panel. Displays don't change. The `err` log shows `TSW CHANGE` events firing on raw join numbers (17219, 18358, 29731–29733) but **no** `NVX route:` line is ever logged because my `_c.AA140.Display1SourceFb +=` subscription never fires.

**Root cause**: the rebuilt `.cce` generates a **SmartObject-based** contract. The Contract Editor's `Main.g.cs` constructor calls `ComponentMediator.HookSmartObjectEvents(panel.SmartObjects[1].SigChange)` to listen on the panel's SmartObject collection. But **the panel's `contract.ts` doesn't publish through SmartObjects** — it uses CH5's `publishAnalog(SIGNALS.display1Source, value)` which writes to direct device joins with name-hashed integer joins (~17219 etc.).

Two layers of mismatch:
1. The `.cce` defines signal names in a different direction than the panel's `contract.ts` expects. E.g., the panel side writes to `"AA140.Display1Source"` (a non-Fb name) and reads `"AA140.Display1SourceFb"`. The .cce has those in INPUT vs OUTPUT direction respectively, but the actual integer join hashes don't line up with what Contract Editor assigned (1, 2, 3 vs the panel's 17219, 18358, etc.).
2. The panel doesn't bind a SmartObject at slot 1 (it uses direct joins), so `SmartObject_SigChange` never fires for these.

**Diagnostic evidence** (in `ControlSystem.cs` `_tswPrimary.SigChange += ...`):
- Joins **29731, 29732, 29733** publish `UShort val=0` on a heartbeat — probably the panel's local "current source" feedback values mirroring back, not user-input signals.
- Join **17219** publishes `UShort val=4..35` — looks like a slider (volume / level / trim).
- Join **18358** publishes `Bool true/false` — some toggle (probably mute).
- The `Name` field comes back as `"UShort Sig Number N"` — Crestron's default when no signal name is bound. **Confirms no contract name table loaded on the panel side.**

### Three viable fixes for this

**Option A — Skip Contract Editor entirely; hard-wire direct joins.**
Empirically discover which raw join the panel uses for `display1Source` by tapping the button and watching the log. Then in `NvxRoutingService.Initialize()`:
```csharp
_tswPrimary.SigChange += (dev, args) => {
    if (args.Sig.Type != eSigType.UShort) return;
    switch (args.Sig.Number) {
        case <hash-of-display1Source>: RouteSourceToDisplay(args.Sig.UShortValue, 1); break;
        case <hash-of-display2Source>: RouteSourceToDisplay(args.Sig.UShortValue, 2); break;
        case <hash-of-display3Source>: RouteSourceToDisplay(args.Sig.UShortValue, 3); break;
    }
};
```
Fastest. Brittle: hardcoded join numbers, lost if signal names ever change.

**Option B — Deploy the `.cse2j` to the panel.**
Put `MCCCD-AA140/contracts/output/MCCCD_AA140/interface/mapping/MCCCD_AA140.cse2j` somewhere the panel reads at boot (likely `public/config/` or in the .ch5z root). The `.cse2j` is the panel's join-name-to-number map; with it loaded the panel publishes on the same small numbers (1, 2, 3...) that Contract Editor assigned. Need to update `build.mjs` to copy `.cse2j` into `dist/` and verify the panel's `cr-com-lib.js` actually loads it. Cleanest if it works.

**Option C — Compute the CH5 name-hash on the C# side and subscribe to that integer.**
Crestron's `cr-com-lib.js` uses a deterministic hash from the signal string. Replicate the same algorithm in C# (or use Crestron's `EthernetParametersBase.GetJoinFromName(...)` helper if it exists on `BasicTriListWithSmartObject`), subscribe to that integer join, no contract file needed. Medium effort; couples to the hash algo (which has been stable for years but is unpublished).

My recommendation: **A for tonight, B for production.** A gets routing working in 10 minutes; B is the correct long-term answer matching what the rebuilt `.cce` is for.

### Stubbed services that need re-wiring to the new Contract API

These compile but their panel inputs are inert (TODO comments mark each):

- **`ShureP300Service.WirePanelSignals()`** — empty body. Master volume / mute / mic mute / trim / fader / audio-follows-display buttons don't drive the P300. TCP transport still runs against the stub IP `192.168.2.151`.
- **`ShureP300Service.HandleReport()` and `HandleSampleIn()`** — P300 REP and SAMPLE_IN frames received but not forwarded to panel feedbacks.
- **`CameraService.Initialize()`** — empty body. PTZ / preset save/recall/delete / Send-to-VTC / tracking-mode / zoom buttons don't drive cameras. REST methods still callable from console for testing.
- **`SystemPowerController.Initialize()`** — empty body. The `DisplayPower` button on the panel doesn't toggle on/off. The initial `PowerUpSequence()` at boot still runs (so D1/D2/D3 get routed to Source 1 at startup).
- **`SystemPowerController.PowerUpSequence()`** and **`PowerDownSequence()`** — no longer drive `SystemPowerFb` / `MuteAll` / `AudioOutputSelectFb` panel feedbacks. Routing still happens.

All of these depend on resolving the panel-publish path first. Once we pick Option A or B for routing, the same pattern applies to every other panel-input wiring.

### Other open items (not blocking)

- **Shure P300 + MXA920×2 IPs** — `192.168.2.151` (P300), `.181`, `.182` (MXAs) are stubs. Reconnect loops fire `SOCKET_STATUS_NO_CONNECT` warnings every 5 seconds. Replace with real IPs when commissioning the audio rack.
- **AirMedia HTTPS handshake** from a Windows test client fails (`The underlying connection was closed`). Likely a TLS cipher mismatch; the on-processor `Crestron.SimplSharp.Net.Https.HttpsClient` may behave differently — verify after panel routing works. Default creds (admin/admin) also unconfirmed.
- **Sony VPL + Newline IP-series IPs + models** — stubs in `SonyVplService.cs` and `NewlineService.cs`. Sony assumes ADCP NOKEY auth (disable "Requires Authentication" on the projector web UI); Newline assumes STV+ command bytes for HDMI 2 (`0x0B`) — change to `0x52` for DV-series. Power-on for Newline requires Wake-on-LAN to the panel's MAC address (currently the placeholder `00:00:00:00:00:00` in the service).
- **AES67 audio multicast** — encoders are configured for video multicast only. The Shure P300 will pull NAX audio from the encoders via Dante AES67 once we set `Control.SecondaryAudio.MulticastAddress` (or the equivalent NAX property) on each encoder.
- **NVX-384 HDMI vs USB-C selector** — the 384 is registered and emitting on `239.8.0.12`. Auto-switch between its HDMI and USB-C inputs should "just work" via the device's internal logic. The "both plugged → user picks" panel UI requires new contract signals (`Nvx384HdmiConnected`, `Nvx384UsbCConnected`, `Nvx384SelectInput`) — not present in the rebuilt `.cce` yet.
- **Forced Auth Mode + FIPS Mode** on the RMC4 are still ON. The TSW and NVX boxes connected anyway (probably because none of them require explicit credentials to register). Worth turning both off for dev unless there's a security reason.
- **Mirror buttons** (`D1MirrorToD3`, `D2MirrorToD3`) — the rebuilt `.cce` has these only on the INPUT (SIMPL→panel) direction. There's no panel-publish equivalent in the contract, so mirror taps can't be received. Either add `D1MirrorToD3Fb` / `D2MirrorToD3Fb` to the `.cce` and rebuild, or wire via the same direct-join path as Option A.

---

## File-level changes since last commit (`2c867d4`)

- **Added**: `AirMediaService.cs`, `NewlineService.cs`, `ShureMxaService.cs`, `ShureP300Service.cs`, `ShureTcpClient.cs`, `SonyVplService.cs`
- **Added (generated from `.cce`)**: `Generated/Contract.g.cs`, `Main.g.cs`, `ComponentMediator.g.cs`, `UIEventArgs.g.cs`
- **Deleted**: `OccupancyController.cs`, `QsysAudioService.cs`, `Generated/MainContract.cs` (the old stub)
- **Modified**: `ControlSystem.cs` (Contract instantiation + raw signal diagnostic), `NvxRoutingService.cs` (PepperDash pattern + new Contract API), `SystemPowerController.cs` (stubbed), `CameraService.cs` (stubbed), `MCCCD-AA140/src/pages/AudioMixer.svelte` (4 strips instead of 5)
- **Added (reference only)**: `MCCCD_AA140_Equipment_List.xlsx`

## Resuming next session

1. Pick Option A or B above and unblock panel routing.
2. Re-wire the four stubbed services to the new Contract API using the same pattern.
3. Replace stub IPs (P300, MXAs, Sony × 2, Newline, AirMedia creds).
4. Add `Nvx384*` selector signals + mirror Fb signals to the `.cce` and rebuild.
5. Configure AES67 NAX audio multicast on the encoders.
