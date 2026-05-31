# 2026-05-30 — Handoff: SmartObject 2 sigs don't deliver to panel (OPEN)

**State at end of session.** Goal was 8 video-sync FBs lighting tri-state dots on the Home source cards. After ~6 hours and 5 architectural iterations, **the panel does not receive SmartObject 2 sigs from SIMPL — in either direction.** SIMPL writes are confirmed via err log; panel-side stores stay False. Both buttons on the bidirectional contract-test overlay produce nothing in the processor log when tapped. The cse2j is correctly bundled. The panel exposes 2 SmartObjects to the processor. The break is somewhere in the panel's CrComLib runtime — not in our code, not in our contract structure.

All work landed on `main`. Tip: `cd0b58e`.
Both panels (TS-1070 .80 + TSW-1070 .78) and the RMC4 processor (.198) are running the latest binaries (sync-dot UI deployed; SO2 sigs silent).

## Locked-in decisions (do not re-investigate)

- **CH5z archive uses `@crestron/ch5-utilities-cli` (`ch5-cli archive`).** This is the project standard. Do not pivot to `@crestron/ch5-shell-utilities-cli`. The fact that 1Beyond's deployed bundle has shell-template artifacts (`project-config.json`, `component.js`) is an observation, NOT a prescribed fix.
- **`.cce` is the source of truth.** Never hand-author `.cse2j`, `.chd`, or `.g.cs` directly — Contract Editor's Build step is the only path that produces working outputs.

## What landed this session (all on `main`)

| Commit | What |
|---|---|
| `755e5e9` | Sub-contract architecture (VideoSync.cce → subContracts/) — superseded |
| `cd0b58e` | rev3 multi-component pattern (VideoSync as second component in `MCCCD-AA140.cce`) — current |

### Architecture

- **`MCCCD-AA140.cce`** is the source contract. Two components: `Main` (existing 38 cmds + 39 fbs) and `VideoSync` (8 paired fbs + 8 dummy `*Set` cmds). Two `specifications[]` entries — instance names `AA140` and `VideoSync`.
- **`MCCCD-AA140-rev3.cce`** is the file the user ran through Contract Editor to produce the current Generated/ output. Keep as the working reference; deprecate rev2 + standalone VideoSync.cce.
- Contract Editor regen produced clean `Main.g.cs`, `Contract.g.cs`, `ComponentMediator.g.cs`, `UIEventArgs.g.cs`, `VideoSync.g.cs` (all in `MCCCD-AA140/contracts/output/MCCCD_AA140/programming/SIMPLSharp/MCCCD_AA140/`). All five copied into `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/`. Build clean.
- **Panel-side** (`MCCCD-AA140/src/lib/contract.ts`): the 8 new signal names use the `VideoSync.*` prefix (e.g. `SIGNALS.roomPcSync = 'VideoSync.RoomPcSync'`). Subscriptions in `signals.ts` are unchanged structurally.
- **Processor-side** (`PanelDispatcher.cs`): new `WriteBoolSO2(uint join, bool value)` method writes to `panel.SmartObjects[2].BooleanInput[join]`. `PanelJoins.SO2BoolIn` holds the 8 constants (joins 1-8). `NvxRoutingService` polls NVX HDMI sync at 1Hz and dispatches via `WriteBoolSO2`. `AirMediaService` dispatches the 3 AM-3200 sharing-method booleans via `WriteBoolSO2`.
- **Bidirectional contract test overlay** is currently live on the Home page (yellow bar above source cards). Two buttons + per-signal readouts. Leave in place for the next session's diagnostic work.

### Confirmed-working pieces

- `cse2j` bundled inside `.ch5z` has all 8 VideoSync signals on `smartObjectId: 2` joins 1-8 (panel-IN events) and matching `VideoSync.*Set` commands on `smartObjectId: 2` joins 1-8 (panel-OUT states). Verified via Python unzip.
- Processor logs at boot:
  ```
  PanelDispatcher: panel IPID 0x03 has 2 SmartObject(s)
    SO 1: present
    SO 2: present
  PanelDispatcher: SmartObjects[2] (VideoSync) = non-null
  ```
- Processor logs polling writes:
  ```
  NVX ExtPC: sync -> True (join 2)
  NVX AirMedia: sync -> True (join 3)
  ```
  These are `panel.SmartObjects[2].BooleanInput[2/3].BoolValue = true` — no exception thrown.
- SO1 sigs work fine: `SystemPowerFb` (join 4) drives the power icon visually; `MicLavMuteFb` (join 2) drives mic-mute icons. Existing established pattern, no regression.

### Confirmed-NOT-working

- Panel-side stores for all 8 VideoSync.* signals stay False even when SIMPL writes True (visible in the test overlay's `SO2.RoomPc / ExtPc / AirMedia / Laptop` readouts — all show `0`).
- Tapping the test overlay's **"Pulse SO2.RoomPcSyncSet"** button produces no entry in the processor err log even though `_contract.VideoSync.RoomPcSync += handler` is registered in `NvxRoutingService.Initialize()`. So **SO2 panel→SIMPL direction is also broken**, not just SIMPL→panel.
- Same negative result with sub-contract structure (rev2 / VideoSync.cce as separate file) — neither direction works for SO2 sigs.

## What we tested and ruled out

| Hypothesis | Test | Result |
|---|---|---|
| Hand-edits to .cce JSON survive into cse2j | Added 8 fbs directly in JSON | Contract Editor GUI filtered them; cse2j missing them. **Confirmed dead end.** |
| Pairing fbs with sibling commands fixes GUI filtering | Added 8 dummy `*Set` cmds | Contract Editor GUI accepted them. **Confirmed fix for the GUI-display issue.** |
| Pairing also fixes panel-side subscription | Wrote to SO1 joins 17-24 (paired) | Still no panel-side receipt. **Pairing is necessary but not sufficient.** |
| SmartObject 1 boolean inputs cap at slot 10 | Wrote True to existing slots 11, 13, 15 (`MicLavConnected` etc.) | Panel-side stores stayed 0. **Cap confirmed empirically.** |
| Top-level booleans (`smartObjectId: 0`) bypass the cap | Changed cse2j to SO 0 / joins 100-107 | Still no panel receipt. **Dead end.** |
| Sub-contract (separate VideoSync.cce) gives a fresh SmartObject | rev2 sub-contract structure | Processor saw 2 SmartObjects; panel still got nothing on SO2. **Dead end for THIS panel/firmware.** |
| Multi-component in one .cce (1Beyond pattern) | rev3 with Main + VideoSync components | Processor sees 2 SmartObjects; panel still gets nothing on SO2 in EITHER direction. **Dead end with current toolchain — root cause unknown.** |

## Where to start next session

In rough priority order:

1. **Get panel-side runtime introspection working.** The fastest path to root cause is dropping into the panel's browser dev tools (Chromium webview on the TSW-1070) and inspecting CrComLib's internal subscription registry — does it have entries for `VideoSync.RoomPcSync`? When SIMPL writes, does the native bridge fire any event for SO2? Options to investigate:
   - Crestron Toolbox webview debugger (if accessible against this panel firmware)
   - Remote Chromium debug protocol (`chrome://inspect` from a host on the same network targeting the panel's webview port)
   - Adding `console.log` instrumentation INSIDE the panel JS bundle that introspects `window.CrComLib` internals
   - A panel-side periodic `console.log` of each store's current value to confirm the JS sees `subscribeState` callbacks firing at all
2. **Once you can introspect CrComLib state**, the diagnostic is: tap the "Pulse SO2.RoomPcSyncSet" button on Home → check if `CrComLib.publishEvent('b', 'VideoSync.RoomPcSyncSet', true)` was called on the JS side → if yes, the panel-side publish chain is fine and the break is at the CIP layer; if no, the panel's CrComLib didn't recognize the signal name.
3. **Compare CrComLib version behavior.** Our project uses `@crestron/ch5-crcomlib ^2.17.2`. 1Beyond uses `^2.8.0`. If next-session investigation shows CrComLib silently rejects multi-SmartObject contracts in newer versions, downgrading to 2.8.0 is a candidate test. (Don't downgrade speculatively — confirm a behavior delta first.)
4. **Open a support ticket with Crestron** if introspection confirms the SO2 sigs aren't routing despite a correct cse2j. Symptoms: "Multi-component CH5 contract, SmartObject 2 sigs declared in cse2j, processor sees SmartObject 2 on the panel device, writes succeed without exception, but panel-side CrComLib never fires subscribe callbacks for SO2 sigs."

## What NOT to do (or you'll waste a session)

- Don't redo any of the architecture iterations 1-5 from the lessons doc. The contract structure is correct.
- Don't pivot the archive tooling to `ch5-shell-utilities-cli`. User has ruled this out explicitly. CH5z must use `ch5-utilities-cli` (`ch5-cli archive`).
- Don't add more `.cce` revs without first proving (via panel-side introspection) what's actually broken. The contract structure is not the problem — we've cycled through all the reasonable variants.
- Don't claim "lessons learned" or close the investigation until the dots actually light up on a real panel with real sources connected.

## Current panel/processor state

- Panel `dist/` bundle: includes the rev3-regen'd cse2j with both SO1 + SO2 signals (verified).
- Processor `.cpz`: built from current `Generated/` output, contains `VideoSync.g.cs`, writes to SO2 via PanelDispatcher.WriteBoolSO2.
- Diagnostic test bar is on Home above the source cards — leave it for next session's verification.
- Sync dots render grey always (always-show-dot UI is correct; just no True data arriving).

## Files added this session

- `MCCCD-AA140/contracts/MCCCD-AA140-rev2.cce` — superseded sub-contract attempt, kept for reference
- `MCCCD-AA140/contracts/MCCCD-AA140-rev3.cce` — **current working .cce (multi-component)**
- `MCCCD-AA140/contracts/VideoSync.cce` — superseded sub-contract attempt
- `MCCCD-AA140/contracts/subContracts/VideoSync/VideoSync.cce` — Contract Editor's sub-contract output dir, superseded
- `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/VideoSync.g.cs` — current Contract Editor regen output for the VideoSync component
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelDispatcher.cs` — added `WriteBoolSO2`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelJoins.cs` — added `SO2BoolIn` class + `VideoSyncSmartObjectId` constant
- `MCCCD-AA140-SIMPL/MCCCD-AA140/NvxRoutingService.cs` — sync polling + event hooks for SO2 incoming diagnostics
- `MCCCD-AA140-SIMPL/MCCCD-AA140/AirMediaService.cs` — REST poll dispatches Miracast/AirPlay/TX3 to SO2
- `MCCCD-AA140-SIMPL/MCCCD-AA140/SystemPowerController.cs` — no change in final state (diagnostic writes removed)
- `MCCCD-AA140/src/lib/contract.ts` — `SIGNALS.*` map updated to `VideoSync.*` names
- `MCCCD-AA140/src/lib/stores/signals.ts` — 8 new writable booleans + subscribeDigital wiring
- `MCCCD-AA140/src/pages/Home.svelte` — tri-state sync dot UI + bidirectional test overlay
- `MCCCD-AA140/docs/Handoffs/2026-05-30-contract-troubleshooting-lessons.md` — investigation log (NOT a closed playbook)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-30-source-video-sync-badge-design.md` — original spec from earlier in the session
- `MCCCD-AA140/docs/superpowers/plans/2026-05-30-source-video-sync-badge-plan.md` — original plan from earlier in the session
- `MCCCD-AA140/docs/Handoffs/2026-05-30-handoff-so2-open-issue.md` — this doc

## Cleanup pickup list (low priority — don't do during investigation)

- Strip the bidirectional test overlay from `Home.svelte` once SO2 works.
- Remove `MCCCD-AA140-rev2.cce`, `VideoSync.cce`, `subContracts/` once it's clear we're sticking with rev3 multi-component.
- The 8 hand-added `_b042-_b049` feedbacks may still be in `MCCCD-AA140.cce` from earlier iterations even though rev3 doesn't have them — re-verify after the next regen.

## Quick pickup recipe for the next session

```bash
# Confirm tip
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle"
git checkout main
git log --oneline -3   # expect cd0b58e at HEAD

# Verify panels + processor reachable
curl -sI https://192.168.2.78    # TSW-1070 wall
curl -sI https://192.168.2.80    # TS-1070 tabletop
curl -sI https://192.168.2.198   # RMC4 processor

# Re-read the handoff + companion lessons doc before touching code
cat MCCCD-AA140/docs/Handoffs/2026-05-30-handoff-so2-open-issue.md
cat MCCCD-AA140/docs/Handoffs/2026-05-30-contract-troubleshooting-lessons.md

# Pick up the investigation at "Where to start next session" #1 above.
```
