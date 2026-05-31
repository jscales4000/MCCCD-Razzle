# Contract Rebuild → Name-Based Signals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate every hardcoded SmartObject join number from the SIMPL# processor and drive all panel I/O through the generated, name-based `Contract` API — fixing the dead processor→panel feedback at its root.

**Architecture:** The root cause is two-fold: (1) the `.cce` is non-canonical — all 38 commands carry names (schema requires empty), and ~half the signals have no valid bidirectional `siblingId`, so feedback bindings fail silently per the CH5 Contract Workflow Doctrine; (2) a prior session worked around the broken feedback by hand-poking `SmartObjects[1].BooleanInput[<number>]` via `PanelDispatcher`/`PanelJoins` (87 raw constants), violating the "never hardcode joins" rule and writing the wrong sig slots. Fix: regenerate a canonical `.cce`, rebuild via Contract Editor, rewire all 7 services to the name-based `Contract`, and delete the raw-join layer + the optimistic-mirror and SO2 workarounds.

**Tech Stack:** Crestron 4-Series (RMC4 @ 192.168.2.198), SIMPL# Pro (.NET 6, `Crestron.SimplSharpPro`), Contract Editor (Windows GUI), CH5 + Svelte panel (TS-1070 @ .80 / TSW-1070 @ .78), CWS DebugServer at `https://192.168.2.198/cws/aa140/debug/`.

**Authoritative references (read before editing the `.cce`):**
- FRED doc "Crestron .cce Contract File Generation Guide" (`c0532a8d-7b96-4e73-a0e3-d1e677b6ecfc`) — canonical schema + generator pattern.
- FRED doc "CH5 Contract Workflow Doctrine" — lifecycle + silent-failure catalog.

**Non-negotiable rules carried into this plan:**
- NEVER hand-author `.cse2j` / `.chd` / `.g.cs` — only Contract Editor Build produces them.
- NEVER put join numbers in the `.cce` — joins are auto-allocated at Build time.
- In the `.cce`: command `name` is `""`, feedback `name` carries the signal name, `dataType` matches across the pair, `siblingId` is bidirectional (cmd.siblingId→fb.id AND fb.siblingId→cmd.id), `parentId` = component id.
- After any `.cce` change: rebuild Contract Editor → resync → redeploy BOTH `.cpz` and `.ch5z`.

---

## Pre-flight: restore point

- [ ] **Step 1: Tag current state for rollback**

```bash
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle"
git tag v-2026-05-31-pre-contract-rebuild
git log --oneline -1
```

Expected: tag created at current HEAD (`7fd13c9` or later).

---

## Phase 0 — Ground-truth: prove name-based feedback works on ONE signal

Purpose: settle the open schema question (single bidirectional name vs. separate `Foo`/`FooFb` names) empirically on a throwaway 2-signal contract BEFORE regenerating 40 signals. This is the step the prior sessions skipped.

### Task 0.1: Build a minimal canonical test contract

**Files:**
- Create: `MCCCD-AA140/contracts/GroundTruth.cce`

- [ ] **Step 1: Author a 2-signal canonical `.cce` by hand (JSON), one component `Main`, two paired signals**

Signals (both as canonical command+feedback pairs):
- `TestPower` — dataType 1 (Boolean): command half (panel→proc press) + feedback half (proc→panel lamp).
- `TestLevel` — dataType 2 (Numeric): command half + feedback half.

Use the generator pattern from the FRED guide: each signal = `{command: name:"", attributeType:0, siblingId:<fbId>, dataType}` + `{feedback: name:"TestPower", attributeType:1, siblingId:<cmdId>, dataType}`. Copy the structural skeleton (ids, parentId, specifications) from `MCCCD-AA140.cce` so Contract Editor accepts it.

- [ ] **Step 2: Hand off to user for Contract Editor Build**

Tell the user: open `GroundTruth.cce` in Contract Editor → review (2 signals, each shows a paired command+feedback) → Build → export `.cse2j` + generate `.g.cs`. Confirm no "unpaired signal" warnings appear.

### Task 0.2: Minimal panel + processor that exercise the test contract by name

**Files:**
- Create: `MCCCD-AA140-SIMPL/MCCCD-AA140/GroundTruthProbe.cs` (temporary)
- Modify: panel — add a tiny test page OR reuse the existing Home test overlay to publish/subscribe `Main.TestPower` / `Main.TestLevel` by name.

- [ ] **Step 1: Processor — instantiate the generated test Contract and, on a CWS debug command, SET the feedback by name**

```csharp
// GroundTruthProbe.cs — wire ONLY through the generated Contract, no raw joins.
// _gt = new GroundTruthContract(new BasicTriListWithSmartObject[]{ _tswPrimary, _tswSecondary });
// Expose a method the DebugServer can call:
public void PushTestPower(bool v) => _gt.Main.TestPower(sig => sig.BoolValue = v);
public void PushTestLevel(ushort v) => _gt.Main.TestLevel(sig => sig.UShortValue = v);
// And subscribe the command (panel press) by name:
// _gt.Main.TestPower += (s,e) => DebugTrace.Command("groundtruth","TestPower-press");
```

If `_gt.Main.TestPower(...)` as a SETTER does not exist (only an event), that is the decisive datum: it means the canonical pattern needs the name on the OTHER half — adjust Task 1/2 accordingly and re-run Phase 0.

- [ ] **Step 2: Deploy processor + panel, then inject by name via debug tool**

```bash
# processor
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140-SIMPL"
dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release
PROC_HOST=192.168.2.198 python scripts/deploy.py MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz
# panel
cd ../MCCCD-AA140 && npm run deploy:both
```

- [ ] **Step 3: Verify feedback arrives BY NAME**

Trigger `PushTestPower(true)` (add a `/groundtruth/power?v=1` route to DebugServer, or call from InitializeSystem). Watch the panel test element bound to `Main.TestPower`.
Expected: panel element reflects true. **If it lights up, name-based feedback works and the contract pattern is proven.** Record which `.cce` shape produced it (which half carried the name).

- [ ] **Step 4: Commit the proven pattern as the template**

```bash
git add MCCCD-AA140/contracts/GroundTruth.cce
git commit -m "test(contract): ground-truth canonical 2-signal contract proves name-based feedback"
```

> **Gate:** Do not proceed to Phase 2 until Phase 0 feedback lights up on a real panel. The exact canonical shape proven here parameterizes the full `.cce` generation.

---

## Phase 1 — Signal reference (source of truth for regeneration)

### Task 1.1: Generate the signal reference doc from the panel contract

**Files:**
- Create: `MCCCD-AA140/contracts/signal-reference.md`

- [ ] **Step 1: Derive every signal (name, dataType, direction) from `src/lib/contract.ts` + the live cse2j**

The complete current inventory (this is the spec — every signal must appear in the regenerated `.cce`):

**Commands (panel→processor):**
- Boolean: DisplayPower, D1MirrorToD3, D2MirrorToD3, VolumeUp, VolumeDown, MuteAll, MicLavMute, MicHandheldMute, PtzUp, PtzDown, PtzLeft, PtzRight, CamSendToVtc, ZoomIn, ZoomOut, MicCeiling1Mute, MicCeiling2Mute, MicCeiling3Mute
- Numeric: Display1Source, Display2Source, Display3Source, Display4Source, AudioOutputSelect, CameraSelect, ShotPresetRecall, ShotPresetSave, ShotPresetDelete, CamTrackingMode, MicLavTrim, MicHandheldTrim, MicCeiling1Trim, MicCeiling2Trim, MicCeiling3Trim, MicLavLineOut, MicHandheldLineOut, MicCeiling1LineOut, MicCeiling2LineOut, MicCeiling3LineOut

**Feedbacks (processor→panel):**
- Boolean: PanelOnline, SystemPowerFb, Display1PowerFb, Display2PowerFb, Display3PowerFb, MicLavMuteFb, MicHandheldMuteFb, MicCeiling1MuteFb, MicCeiling2MuteFb, MicCeiling3MuteFb, MicLavConnected, MicHandheldConnected, MicCeiling1Connected, MicCeiling2Connected, MicCeiling3Connected
- Numeric: Display1SourceFb, Display2SourceFb, Display3SourceFb, Display4SourceFb, AudioOutputSelectFb, CamTrackingModeFb, OccupancyState, ShutdownCountdown, MicLavTrimFb, MicHandheldTrimFb, MicCeiling1TrimFb, MicCeiling2TrimFb, MicCeiling3TrimFb, MicLavLineOutFb, MicHandheldLineOutFb, MicCeiling1LineOutFb, MicCeiling2LineOutFb, MicCeiling3LineOutFb, MicLavLevel, MicHandheldLevel, MicCeiling1Level, MicCeiling2Level, MicCeiling3Level

**VideoSync (fold into Main per Phase 5 decision — these are plain proc→panel feedbacks):**
- Boolean feedback: RoomPcSync, ExtPcSync, AirMediaSync, AirMediaMiracast, AirMediaAirPlay, AirMediaTx3, LaptopHdmiSync, LaptopUsbcSync

- [ ] **Step 2: Apply the Phase-0 naming decision**

Record per the Phase-0 result whether each logical signal is one bidirectional name or a `Foo`/`FooFb` pair, and lock the convention here so the generator is deterministic.

- [ ] **Step 3: Commit**

```bash
git add MCCCD-AA140/contracts/signal-reference.md
git commit -m "docs(contract): signal reference for canonical .cce regeneration"
```

---

## Phase 2 — Generate the canonical `.cce`

### Task 2.1: Write a generator that emits a schema-valid `.cce`

**Files:**
- Create: `MCCCD-AA140/contracts/scripts/build_cce.py`
- Create (output): `MCCCD-AA140/contracts/MCCCD-AA140-canonical.cce`

- [ ] **Step 1: Implement the generator using the doctrine's pattern**

Per the FRED guide `generatorPattern`: for each `[name, dataType]` create a command `{id, name:"", parentId:<componentId>, siblingId:<fbId>, dataType, attributeType:0}` + feedback `{id, name, parentId:<componentId>, siblingId:<cmdId>, dataType, attributeType:1}`. Single component `Main`, instanceName `AA140`. NO join numbers. IDs globally unique (`_c001`, `_f001`, …). Seed structural fields (`Errors`, `specifications`, `schemaVersion`, root ids) from the existing `MCCCD-AA140.cce`.

- [ ] **Step 2: Validate pairing integrity before handoff**

```bash
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140/contracts"
python scripts/validate_cce.py MCCCD-AA140-canonical.cce
```

Expected: `0 named commands`, `0 unpaired signals`, `all dataTypes matched`. (Write `validate_cce.py` mirroring the checks already used this session: every command `name==""`, every signal `paired` bidirectionally.)

- [ ] **Step 3: Commit**

```bash
git add MCCCD-AA140/contracts/scripts/build_cce.py MCCCD-AA140/contracts/scripts/validate_cce.py MCCCD-AA140/contracts/MCCCD-AA140-canonical.cce
git commit -m "feat(contract): generate canonical .cce (paired cmd+fb, empty cmd names, bidir siblingId)"
```

---

## Phase 3 — Contract Editor Build (human-in-the-loop)

### Task 3.1: Build + regenerate outputs

- [ ] **Step 1: User builds in Contract Editor**

Hand off: open `MCCCD-AA140-canonical.cce` in Contract Editor → confirm signal list (no unpaired warnings) → Build. Produces `*.g.cs` (SIMPL#) + `.cse2j` (panel).

- [ ] **Step 2: Copy generated outputs into both projects**

```bash
# .g.cs → processor
cp MCCCD-AA140/contracts/output/MCCCD_AA140/programming/SIMPLSharp/MCCCD_AA140/*.g.cs \
   MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/
# .cse2j → panel
cp MCCCD-AA140/contracts/output/MCCCD_AA140/interface/mapping/MCCCD_AA140.cse2j \
   MCCCD-AA140/dist/config/contract.cse2j
```

- [ ] **Step 3: Verify the generated contract now exposes feedback setters by name**

```bash
grep -E "void (SystemPowerFb|Display1SourceFb|PanelOnline)\(" MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/Main.g.cs
```

Expected: setter methods exist for feedback signals (or the Phase-0-proven shape). If feedbacks are still events-only, STOP — the `.cce` direction/pairing is still wrong; return to Phase 2.

- [ ] **Step 4: Commit**

```bash
git add MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/ MCCCD-AA140/dist/config/contract.cse2j MCCCD-AA140/contracts/output/
git commit -m "chore(contract): Contract Editor build of canonical contract (g.cs + cse2j)"
```

---

## Phase 4 — Rewire processor to name-based Contract; delete raw-join layer

### Task 4.1: Rewire each service to the generated `Contract`

**Files (modify):** `NvxRoutingService.cs`, `ShureP300Service.cs`, `CameraService.cs`, `SystemPowerController.cs`, `AirMediaService.cs`, `ControlSystem.cs`, `Debug/DebugServer.cs`

- [ ] **Step 1: Replace command handlers — subscribe by name**

For every `_panel.OnBool(PanelJoins.BoolOut.X, h)` → `_contract.Main.X += (s,e) => h(e.Sig.BoolValue);`. For every `_panel.OnUShort(PanelJoins.UShortOut.X, h)` → `_contract.Main.X += (s,e) => h(e.Sig.UShortValue);`.

- [ ] **Step 2: Replace feedback writes — set by name**

For every `_panel.WriteBool(PanelJoins.BoolIn.XFb, v)` → `_contract.Main.XFb(sig => sig.BoolValue = v);`. For every `_panel.WriteUShort(PanelJoins.UShortIn.XFb, v)` → `_contract.Main.XFb(sig => sig.UShortValue = v);`. (Exact setter signature per Phase-0/Phase-3.)

- [ ] **Step 3: Build, fix all references until clean**

```bash
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140-SIMPL"
dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release
```

Expected: build succeeds with zero references to `PanelJoins` or `PanelDispatcher.Write*`.

- [ ] **Step 4: Commit**

```bash
git add MCCCD-AA140-SIMPL/MCCCD-AA140/*.cs
git commit -m "refactor(simpl): drive all panel I/O through generated Contract by name"
```

### Task 4.2: Delete the raw-join layer

**Files:** Delete `PanelJoins.cs`, `PanelDispatcher.cs`. Update `ControlSystem.cs` (remove `_panel`), `DebugServer.cs` (re-point `/signal` to `_contract` name-based setters or remove).

- [ ] **Step 1: Delete files + remove all references**

```bash
git rm MCCCD-AA140-SIMPL/MCCCD-AA140/PanelJoins.cs MCCCD-AA140-SIMPL/MCCCD-AA140/PanelDispatcher.cs
```

- [ ] **Step 2: Rebuild clean**

Run: `dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release` — Expected: PASS, zero `PanelJoins`/`PanelDispatcher` symbols remain (`grep -rc "PanelJoins\|PanelDispatcher" --include=*.cs | grep -v ':0'` returns nothing outside Generated/).

- [ ] **Step 3: Commit**

```bash
git commit -am "refactor(simpl): delete PanelJoins + PanelDispatcher raw-join layer"
```

---

## Phase 5 — Remove the workarounds (optimistic mirror + SO2 split)

### Task 5.1: Remove the optimistic mirror from the panel

**Files (modify):** `MCCCD-AA140/src/lib/stores/router.ts`, `src/pages/DisplayRouting.svelte`, `src/pages/Home.svelte`

- [ ] **Step 1: Make `routeSource` publish ONLY (stop mirroring into the Fb store)**

In `router.ts` remove the local `display{N}SourceFb.set(...)` writes; keep only `publishAnalog(SET_SIGNAL_BY_DISPLAY[...], value)`. The marker now reads real `Display{N}SourceFb` feedback from SIMPL.

- [ ] **Step 2: Deploy panel + processor; verify the marker still updates — now via REAL feedback**

```bash
cd MCCCD-AA140 && npm run deploy:both
```

Tap a source; the marker should move only because SIMPL echoes `Display{N}SourceFb`. Confirm via debug tool that the processor wrote it by name.

- [ ] **Step 3: Commit**

```bash
git commit -am "refactor(panel): remove optimistic mirror; markers now read real feedback"
```

### Task 5.2: Fold VideoSync sync FBs into the Main contract (drop SO2)

**Files:** `src/lib/contract.ts` (8 `VideoSync.*` → `AA140.*` names), `src/lib/stores/signals.ts`, `Home.svelte` (sync dots), remove `MCCCD-AA140-rev2/rev3/VideoSync.cce`, `WriteBoolSO2`, `SO2BoolIn`.

- [ ] **Step 1: The 8 sync signals are already in the Phase-1 reference as Main feedbacks** — confirm they regenerated into Main, then update panel `SIGNALS.*` to the `AA140.*` names and re-point `signals.ts` subscriptions.

- [ ] **Step 2: Deploy both; verify a sync dot lights via debug inject by name**

- [ ] **Step 3: Commit**

```bash
git commit -am "refactor(contract): fold VideoSync feedbacks into Main; drop SO2 split"
```

---

## Phase 6 — Full verification

### Task 6.1: Verify every feedback round-trips by name

- [ ] **Step 1: Inject each feedback by name via the debug tool and confirm on the panel**

For SystemPowerFb, MicLavMuteFb, Display1PowerFb, Display1SourceFb, a trim, a sync dot — trigger the processor to set it by name (debug route or simulated device) and confirm the panel element reflects it. Expected: all reflect.

- [ ] **Step 2: Verify commands still reach SIMPL**

Tap Power, a mic mute, a source select; confirm `_contract.Main.X` events fire (DebugTrace). Expected: all dispatch.

- [ ] **Step 3: Confirm zero hardcoded joins remain**

```bash
grep -rnE "BooleanInput\[|UShortInput\[|SmartObjects\[[0-9]" MCCCD-AA140-SIMPL/MCCCD-AA140 --include=*.cs | grep -v Generated/
```

Expected: no matches outside `Generated/`.

- [ ] **Step 4: Final commit + restore tag**

```bash
git commit -am "feat(contract): name-based signal layer complete; feedback verified end-to-end"
git tag v-2026-05-31-name-based-contract
```

---

## Rollback

If any phase fails irrecoverably: `git reset --hard v-2026-05-31-pre-contract-rebuild`, redeploy the prior `.cpz` + `.ch5z`. The pre-flight tag is the safety net.
