# DM-NVX Routing — Lessons Learned

**Scope.** Lessons from bringing up DM-NVX routing on the MCCCD-AA140 RMC4 across two sessions (2026-05-26 → 2026-05-27). Covers transmitter/receiver wiring, the contract bridge that gets panel taps to the routing service, and the multicast → unicast switch.

**Hardware in play.**
- 3× DM-NVX-E30 encoders (Room PC IPID 0x11, Ext PC 0x12, AirMedia 0x13)
- 1× DM-NVX-384 combo encoder (HDMI + USB-C, IPID 0x14)
- 3× DM-NVX-D30 decoders (Display 1/2/3, IPID 0x21/0x22/0x23)
- RMC4 4-Series processor (192.168.2.198) running net6.0 SIMPL# Pro
- 1× TS-1070 tabletop panel + 1× TSW-1070 wall panel (duplicates, IPIDs 0x03 / 0x04)

**Source files referenced.**
- [NvxRoutingService.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/NvxRoutingService.cs)
- [ControlSystem.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/ControlSystem.cs)
- [Generated/Main.g.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/Main.g.cs) (Contract Editor output)
- [package.json](../../package.json) (archive script)

---

## TL;DR — what actually works

1. **Encoders**: set `Control.DeviceMode = Transmitter` + `Control.MulticastAddress.StringValue` inside `OnlineStatusChange`. No NullSig issues on the TX side. Works first try.
2. **Decoders (D3x family)**: in `OnlineStatusChange`, call **`Control.EnableAutomaticInitiation()`** — that single call is what allocates the receiver-side string sigs (`ServerUrl`, `MulticastAddress`) from NullSig. Then write only `Control.ServerUrl.StringValue`. **Do not write `DeviceMode`, `SessionInitiation`, or `VideoSource` on a D3x** — they break sig allocation or no-op.
3. **Panel ↔ Processor binding**: ship the Contract Editor `.cse2j` inside the `.ch5z` archive via `ch5-cli archive -c <path-to-cse2j>`. Without `-c`, the panel falls back to name-hashed direct joins and the C# `SmartObject_SigChange` handler never fires.
4. **Encoder IP for unicast RTSP**: read `enc.ConnectedIpList[0].DeviceIpAddress` in `OnlineStatusChange`. May be empty on the first event — schedule a one-shot 5-second `CTimer` retry. Cache the result and re-apply any routes that landed on the multicast fallback in the meantime.

---

## What we got wrong (and how we found out)

### 1. The receiver NullSig rabbit hole (Session 1)

**What we tried (all failed).** Every Crestron-published recipe for D30 receiver-side routing:

```csharp
dec.Control.DeviceMode = eDeviceMode.Receiver;
dec.Control.SessionInitiation = eSessionInitiation.ByReceiver;
dec.Control.EnableAutomaticInitiation();
dec.Control.ServerUrl.StringValue = "rtsp://...";
dec.Control.MulticastAddress.StringValue = "239.8.0.0";
dec.Control.VideoSource = eVideoSource.Stream;
```

Variants tried (each over 30+ minutes of test-and-redeploy):

- Set inside `OnlineStatusChange` after `DeviceOnLine == true`
- Set before `Register()`
- Set inside a `BaseEvent` retry loop gated on `DeviceModeFeedback == Receiver`
- Set via `CTimer` with 5s backoff
- Reflection-based `SessionInitiation` write to dodge a CS0433 type collision

Every one threw `InvalidOperationException: Attempt to set StringValue of NullSig` (or `UShortValue of NullSig` for `SessionInitiation`).

**What actually worked.** PepperDash's open-source `epi-crestron-nvx` plugin had the right pattern: skip `DeviceMode`, skip `SessionInitiation`, skip `VideoSource`, **call `EnableAutomaticInitiation()` once and just write `ServerUrl`**. PepperDash explicitly guards `if (device is not DmNvxD3x)` around the `DeviceMode` write — D3x devices are hardware-locked as receivers and writing this confuses the SDK proxy so badly that the receiver-side sigs never get allocated.

**What we did right.** When the documented recipes failed, we went to open source for a working production reference rather than burning more time on Crestron docs.

**What we did wrong.** We trusted the `Crestron SIMPL# Engineer` FRED persona's NVX guidance over actually-running open-source code. The persona's rule "set ServerUrl + MulticastAddress + VideoSource=Stream" is correct for some NVX variants but wrong for D3x. Memory updated: when the persona says "triple-set", verify against PepperDash before trusting it for D3x receivers.

**Future fix.** Update the SIMPL# Engineer persona's NVX section to call out the D3x exception and cite PepperDash as the canonical source for receiver-side routing.

---

### 2. The "panel publishes, nothing happens" mystery (Session 2)

**Symptom.** Source-button taps on the panel showed up in the `err` log as raw `TSW CHANGE: type=UShort join=17219 val=...` lines, but the `_c.AA140.Display1SourceFb +=` event subscription in `NvxRoutingService.cs` never fired. Logged it: `TSW PRIMARY: 0 SmartObject slot(s) discovered`.

**Root cause.** The Contract Editor's generated `Main.g.cs` calls `ComponentMediator.HookSmartObjectEvents(device.SmartObjects[1].SigChange)` — it listens on **SmartObject slot 1** of the panel. But the panel was publishing via raw `CrComLib.publishEvent('n', 'AA140.Display1Source', value)`, which CH5 routes via a name-hashed direct join (~17219), not via the SmartObject slot. The two paths never met.

**Three options on the table.**

| | What it is | Risk | Verdict |
|---|---|---|---|
| **A** | Hard-wire `_tswPrimary.SigChange += ...` to listen on the raw hashed joins (discover via tapping + log) | Brittle (hash could change) | Rejected once B was viable |
| **B** | Ship the `.cse2j` inside the `.ch5z` so `cr-com-lib` routes name publishes to SmartObject slot 1 | Mechanism unverified for our project | **Picked** after seeing 1Beyond do it |
| **C** | Replicate CH5's name-hash algorithm in C# to subscribe to the same hashed joins | Couples to unpublished hash; medium effort | Eliminated — non-viable without intercepting the hash |

**What we did right.** Before implementing A, we asked "is there a working reference in the repo?" That led to inspecting [1Beyond/](file:///C:/Users/scale/CascadeProjects/1Beyond/) which had a complete working CH5↔SIMPL# contract integration. Pattern-matched four pieces:

1. `.cse2j` lives at `<panel-project>/config/contract.cse2j` (or referenced directly from contract output)
2. `build.mjs` copies it into `dist/config/` (or `ch5-cli` handles it via `-c`)
3. `package.json` archive script: `ch5-cli archive -p <name> -d dist -o output -c <path-to-cse2j>`
4. C# csproj `<Compile Include="..\..\output\.../*.g.cs"><Link>...</Link>` to pull generated bindings without copy-paste

**What we did wrong.** Initially proposed a `build.mjs` change to copy `.cse2j` into `dist/config/` (mirroring 1Beyond's belt-and-suspenders approach). Cleaner: pass `-c <cse2j-source-path>` directly to `ch5-cli archive` — one-line `package.json` edit, no build.mjs changes. We almost over-engineered it.

**The actual fix.** One line:

```diff
-"archive": "npm run build && ch5-cli archive -p MCCCD-AA140 -d dist -o output",
+"archive": "npm run build && ch5-cli archive -p MCCCD-AA140 -d dist -o output -c contracts/output/MCCCD_AA140/interface/mapping/MCCCD_AA140.cse2j",
```

**Persona moment.** When asked "what personas do you have working with? You should know how to assemble the ch5z using crestron cli tools," consulting the **Crestron CH5 Extended Developer** persona returned the verbatim authoritative rule: `ch5-cli archive` from `@crestron/ch5-utilities-cli` — **never** `ch5-shell-cli export:project` (different package, won't produce valid `.ch5z`). That rule and the `-c` flag together were the entire answer. Lesson: **for any non-trivial Crestron task, consult the project's attached FRED personas first.** Six are attached to this project — check them before guessing.

---

### 3. Multicast → unicast and the `ConnectedIpList` empty-on-online gotcha

**Symptom (first attempt).** After switching the code to read `enc.ConnectedIpList[0].DeviceIpAddress` and build `rtsp://<ip>:554/live.sdp`, the log STILL showed multicast URLs. The user said "I still get multicast."

**Diagnostic plan.** Added explicit `Notice` for the success case and `Warn` for "ConnectedIpList empty — staying on multicast" so we could see exactly which branch hit.

**Root cause.** `ConnectedIpList` was empty when `OnlineStatusChange` fired for at least some encoders. CIP join completion ≠ peer-info table populated. The encoders sometimes report online a beat before their IP info propagates into the processor's local table.

**Failed exploration.** Tried `enc.IpAddressFeedback` as a fallback — doesn't exist on `DmNvxBaseClass` (compiler error CS1061). Brain search showed it lives on a nested `DmNvxBaseClass.DmNvx35xNetwork` class (path likely `enc.Network.IpAddressFeedback`), but rather than fish for the exact nested path I dropped that branch and went with a retry.

**What worked.** Schedule a one-shot 5-second `CTimer` after a failed initial read. The retry pattern lives in `ScheduleEncoderIpRetry()` — re-runs `TryDiscoverEncoderIp` and, if it finds an IP this time, swaps `_sourceStreamUrls[sourceIndex]` and re-applies any decoder routes still pointing at the multicast fallback via `ReapplyRoutesForSource`.

**What we did right.**
- Started the diagnostic round before guessing at a fix.
- Built an explicit "discovery source" string into the log line (`via ConnectedIpList` / `via IpAddressFeedback`) so future timing issues are diagnosable in one read.
- Kept the multicast URL as the initial fallback in `Initialize()` so routes attempted before encoder-online still got a working stream.

**What we did wrong.**
- The first version's logging was vague — only logged "ConnectedIpList empty" not enough to distinguish "empty list" from "list with empty string" from "0.0.0.0" placeholder. Tightened the second pass.
- Tried to write a multi-source fallback (`ConnectedIpList → IpAddressFeedback → ServerUrlFeedback`) without verifying each property compiles. Wasted a build cycle.

**Outcome.** Log on the second deploy:

```
NVX route: D1 <- rtsp://192.168.2.167:554/live.sdp
NVX route: D2 <- rtsp://192.168.2.167:554/live.sdp
NVX route: D3 <- rtsp://192.168.2.167:554/live.sdp
NVX route: D1 <- rtsp://192.168.2.166:554/live.sdp
...
```

All four encoders discovered, all routes unicast.

---

## Smaller lessons (incidental but worth recording)

### Encoder vs Decoder side asymmetry

Encoders don't need `EnableAutomaticInitiation()`. The TX-side string sigs are available the moment `OnlineStatusChange` fires. The asymmetry is genuine — receiver-side and transmitter-side use different internal sig allocation paths. Don't assume what works for one works for the other.

### Multicast spacing — even/odd convention

Per the **SIMPL# Engineer** persona: NVX video streams always occupy EVEN multicast addresses; the immediately-following ODD address is reserved for AES-67 (NAX) audio. Always space encoders by 4: `239.8.0.0` (Room PC), `239.8.0.4` (Ext PC), `239.8.0.8` (AirMedia), `239.8.0.12` (NVX-384). Crowding closer than 4 risks audio collisions with adjacent video streams.

### D30 IP table population

Each D30 needs its own IP table populated pointing back at the RMC4's CIP IPID slot (0x21/0x22/0x23). On a fresh D30 this is done via the D30 web UI, not via processor-side code. Took ~5 minutes per box. Once done, CIP joins came up immediately. **The processor cannot push this IP table to the decoder — it has to be configured on the decoder first.**

### Forced Auth Mode + FIPS Mode on RMC4

Both were ON on the upgraded RMC4 firmware (2.8006.00284). The TSW panels and NVX boxes still connected fine — neither requires explicit CIP credentials for register. Didn't need to disable them. Worth turning off for dev unless there's a specific security reason.

### Panel routing UX: one-to-all by design

When taps on a source button in `Home.svelte` produced `D1/D2/D3 route` lines all at once, it looked like a fan-out bug. It wasn't — `selectSourceForAll(value)` publishes all three display source signals deliberately for the "one source mirrored across all displays" common-case UX. Per-display routing lives on the Advanced Routing page. Always read the call site before assuming the C# is fanning out.

### `proginfo -p:01` is broken after SIMPL# Pro load

Documented in the processor memory note already, but worth restating: on this firmware, `proginfo -p:01` returns `Bad or Incomplete Command` after a SIMPL# Pro program loads. Use `progregister` and `progcomments` instead. Doesn't indicate a deploy failure.

---

## What to do next time we touch NVX

1. **Always start the encoder-online handler with**:
   ```csharp
   enc.Control.DeviceMode = eDeviceMode.Transmitter;
   enc.Control.MulticastAddress.StringValue = mcastVideo;
   ```
   in that order. No retries needed on the TX side.

2. **Always start the decoder-online handler with `EnableAutomaticInitiation()`** before any other `Control.*` write. Skip `DeviceMode`, `SessionInitiation`, `VideoSource` on D3x.

3. **For unicast routing, read `enc.ConnectedIpList[0].DeviceIpAddress` with a 5s retry.** If you need this on multiple device families (not just NVX), the same pattern should apply but verify per family.

4. **Panel-side contract embedding**: any time you regenerate `.cse2j` from Contract Editor, the embedded copy in the `.ch5z` is now stale. Re-archive + redeploy `.ch5z` AND `.cpz`. Both, or signals will mismatch silently.

5. **Routing fallback chain**:
   - `_sourceStreamUrls[srcIndex]` populated at `Initialize()` with multicast URL (always available)
   - Overwritten on encoder online with unicast URL once IP is known
   - `_pendingUrl[displayNum]` tracks per-display intent; survives encoder-late-online and decoder-late-online
   - `ReapplyRoutesForSource(old, new)` migrates routes when the source URL changes

---

## Restore points

- `checkpoint-nvx-live-contract-rebuild` (commit `fed4a38c`) — Session 1 end. Multicast routing working, panel routing broken (no contract embedded).
- `checkpoint-nvx-unicast-routing` (commit pending this session) — Session 2 end. Panel routing fixed (`.cse2j` embedded), multicast → unicast switch via `ConnectedIpList` + retry.
