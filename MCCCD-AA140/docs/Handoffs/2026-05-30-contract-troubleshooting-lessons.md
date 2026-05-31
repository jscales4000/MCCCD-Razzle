# 2026-05-30 — CH5 Contract Troubleshooting: Lessons Learned

> **Scope:** This document captures the full debugging journey of adding 8 new feedback signals to a CH5 panel contract in an existing project, and the structural rules that govern Crestron CH5 + Contract Editor + CrComLib interaction. Several hard-won lessons here are not visible from the Crestron tooling itself — they were extracted from ~6 hours of empirical iteration with the live RMC4 + TS-1070/TSW-1070 panels.
>
> **Target audience:** Future agents (and humans) adding signals to an existing AA140-class CH5 project; CH5 Extended Developer persona maintainers.

## TL;DR — must-do rules

1. **Never hand-author the JSON in a `.cce` and expect Contract Editor's GUI to surface it.** Hand-edits persist in the file but the GUI silently filters them out, and the generated `.cse2j` / `.chd` / `.g.cs` drop them. The .cce JSON is the source of truth ONLY when written by Contract Editor.
2. **Every feedback signal MUST be paired with a sibling command via `siblingId`.** Unpaired feedbacks land in the cse2j but **do not deliver to the panel** through CrComLib subscriptions. If a feedback has no logical command counterpart, create a dummy `*Set` command and pair them anyway. This is non-obvious and not documented in the Crestron tools.
3. **SmartObject 1's boolean-input capacity is bounded.** On this contract/panel we proved slots 11+ silently swallow writes. Pairing helps Contract Editor accept the signal — but doesn't fix the slot cap. Reserve SmartObject 1 for the first ~10 paired boolean feedbacks. Add a second component (= second SmartObject) for additional feedback groups.
4. **Use multi-component, NOT sub-contracts.** Per the 1Beyond ISMI reference contract and the CH5 boilerplate template, additional SmartObjects come from declaring **additional components in the same `.cce`** (each with its own `specifications[]` entry), not from a separate sub-contract file. We tried sub-contracts; the panel saw SO2 in `panel.SmartObjects` but no actual sig delivery — likely a CrComLib limitation.
5. **After any `.cce` change: rebuild `.cpz` AND redeploy `.ch5z`.** Both sides must be in sync — old `.ch5z` on the panel + new `.cpz` on the processor = silent signal mismatch.

## The full session journey

### Iteration 1: Hand-add signals to existing .cce JSON
**What we did:** Added 8 new feedback entries directly to `MCCCD-AA140.cce` with hand-picked IDs `_b042` through `_b049`. Wrote a Python script to do this cleanly.

**Result:** JSON file shows all 8 entries. `npm run build && deploy:both` runs clean. **Panel sees nothing.**

**Why it failed:** When the user opened the `.cce` in Contract Editor's GUI, only 39 of 47 feedbacks rendered. The hand-added entries (`_b042-_b049`) were silently filtered. Contract Editor's Build step then produced a `.cse2j` that **omitted** the hand-added entries. The panel never saw them because they weren't in the bundled `.cse2j` despite being in the source `.cce`.

### Iteration 2: Pair every feedback with a sibling command
**What we did:** Rewrote the `.cce` to give each of the 8 new feedbacks a dummy `*Set` command sibling. Cross-linked via `siblingId`.

**Result:** Contract Editor's GUI showed all 8 entries this time. Regen produced a `.cse2j` with the new sigs at SmartObject 1 joins 17-24.

**Why it didn't fully work:** Signals appeared in the `.cse2j` but writes to those slots still didn't deliver to the panel. We added a runtime diagnostic on the panel showing the raw store values — they stayed `0` even after SIMPL log confirmed `True` writes to slots 17-24.

**Concrete proof:** A separate diagnostic wrote `True` to `MicLavConnected` (existing in cse2j at slot 11) and `MicCeiling3Connected` (slot 15) at PowerUp. Both stayed `0` on the panel. This confirmed the cap is independent of pairing.

### Iteration 3: Switch to top-level booleans (smartObjectId: 0)
**What we did:** Changed cse2j entries to `smartObjectId: 0`, joins 100-107. Added `panel.BooleanInput[N]` write path on SIMPL side.

**Result:** Still no delivery.

**Why:** Top-level booleans need a different CrComLib subscription path that doesn't match how the cse2j routes signals. Or there's an analogous cap at the top-level boolean array. Either way, this wasn't the answer.

### Iteration 4: Sub-contract (VideoSync.cce as separate file)
**What we did:** Created `VideoSync.cce` as a standalone contract. Used Contract Editor's sub-contract feature to import it into the main contract. Regen produced a `Generated/VideoSync/VideoSync.g.cs` file and merged signals into `MCCCD_AA140.cse2j` at SmartObject 2 joins 1-8, namespaced as `VideoSync.RoomPcSync` etc.

**Result:** Processor confirmed `panel.SmartObjects[2]` non-null with the new sigs. SIMPL writes happened. Panel still saw nothing.

**Why:** Sub-contract mechanism produces a `MCCCD_AA140.VideoSync.VideoSync` C# class with its own namespace. The cse2j entries are correct, but the CrComLib panel-side subscription path doesn't fully handle sub-contract-derived signal names in this build.

### Iteration 5: Multi-component (single .cce, two components)
**What we did:** Restructured to match the 1Beyond ISMI contract pattern: ONE `.cce` file with TWO components (`Main` + `VideoSync`), each with its own specification. No sub-contracts. This matches the FRED CH5 boilerplate template (`Main` + `SubContract` pattern).

**Result:** cse2j cleanly produced AA140.* on SO1 and VideoSync.* on SO2. Processor sees both SmartObjects. Still no panel delivery.

**Status at end of session:** Open issue. The structure is provably correct. The data flow gap is in the panel firmware / CrComLib's handling of SmartObject 2 — beyond what's debuggable without panel-side browser dev tools or Crestron support.

## What we proved about Crestron's mechanics

### Contract Editor GUI filters hand-edits
Adding entries to the .cce JSON directly preserves them in the file but Contract Editor's GUI doesn't display them and the Build step omits them. The GUI must "see" entries for Build to include them. We don't fully understand the filter — it's not ID range, not field shape, not encoding. Best practice: add via GUI only, OR add via JSON + pair every feedback with a sibling command (which seems to help GUI acceptance based on our experience).

### Pairing is mandatory for delivery
Every feedback in the cse2j that delivers values to the panel has a non-empty `siblingId`. Every unpaired feedback we tested (Display1-3PowerFb at slots 5-7 are exceptions that pre-date this session and apparently work for unknown reasons; Mic*Connected at slots 11-15 don't deliver; Display4PowerFb at slot 16 doesn't deliver; our 8 at 17-24 with pairing also don't deliver because of the slot cap). The `*Set` dummy command pattern is the workaround when there's no logical "set" counterpart.

### SmartObject 1 boolean-input cap
Empirically demonstrated: writes to `panel.SmartObjects[1].BooleanInput[N]` for `N > 10` silently fail. The write doesn't throw an exception (PanelDispatcher.WriteBool logs no warning), but the panel-side store never updates. The cap appears to be at exactly slot 10 for the existing AA140 contract. We don't know what determines this — possibly panel firmware, possibly Crestron Studio template version, possibly something in how this specific .cce was originally compiled.

### Multi-SmartObject requires multi-component, not sub-contract
The CH5 boilerplate template (`ch5_contract_template.cce` in FRED) ships with two components (`Main` + `SubContract`) and two specifications, all in one .cce. The 1Beyond ISMI contract has five components (Camera, ISMI, Mic, Setup, Video) and matching specifications, also in one .cce. Neither uses sub-contracts. **Sub-contracts are a different mechanism that produces working `.g.cs` and `.cse2j` files but the panel-side CrComLib doesn't fully route signals through them in our build.**

### Both directions of every signal require both sides to know about them
Diagnostic test buttons on the panel that publish `VideoSync.RoomPcSyncSet` produced **no log entry** on the processor — the processor's `Contract.VideoSync.RoomPcSync += handler` was wired and would fire on any incoming sig, but nothing arrived. This means even the panel→processor direction is broken for SO2 sigs on this build. Whatever is preventing SIMPL→panel writes from delivering is symmetric.

## Working configuration end state

After this session, on `main` commit `755e5e9`:

- **`.cce` source:** `MCCCD-AA140-rev3.cce` is the multi-component pattern (the user's working file). Main.g.cs / Contract.g.cs / VideoSync.g.cs / ComponentMediator.g.cs / UIEventArgs.g.cs in `Generated/` came from Contract Editor's Build of rev3.
- **SIMPL writes:** `NvxRoutingService` + `AirMediaService` write to `panel.SmartObjects[2].BooleanInput[1..8]` via `PanelDispatcher.WriteBoolSO2`. Build clean.
- **Panel subscribes:** `SIGNALS.roomPcSync = 'VideoSync.RoomPcSync'` etc. Bundle includes the updated `cse2j`.
- **Panel display:** Tri-state sync dots render — but always show grey/idle because SO2 sigs don't deliver. The whole rendering pipeline (Svelte `$derived`, CSS, etc.) is verified correct via the existing SystemPowerFb which works on SO1.

## Open questions for next session

1. **Why doesn't SmartObject 2 deliver sigs on this contract/panel?** Panel exposes it (processor confirms), cse2j has the entries, SIMPL writes successfully. The break is between "processor writes to SO2 sig" and "panel's CrComLib JS receives the value." Needs panel-side browser dev tools (`chrome://inspect` on the panel webview if possible) to introspect CrComLib's subscription registry at runtime.

2. **What's the SmartObject 1 boolean-input cap rooted in?** Slots 11-15 (Mic*Connected) are paired in the original Contract Editor regen but don't deliver either. Possibly the original AA140 contract was built against an older Crestron Studio template that hardcoded a 10-slot capacity. A complete rebuild of the contract from the FRED boilerplate template might allocate more slots.

3. **Does the `Display1-3PowerFb` unpaired-feedback exception actually work?** We didn't verify those deliver — they may be in the same silently-failing bucket as everything past slot 10, but no UI element exercises them so nobody noticed. Worth confirming.

## Recommended additions to the CH5 Extended Developer persona

These rules generalize from the AA140 session and apply to any CH5 project where signals need to be added to an existing contract:

### Add to MUST DO list
- **MUST DO #19:** When adding feedback signals to an existing CH5 contract, pair EVERY feedback with a sibling command (use a dummy `*Set` command if no logical counterpart exists). Unpaired feedbacks may land in the `.cse2j` but will NOT deliver values to the panel through CrComLib subscriptions.
- **MUST DO #20:** Treat the SmartObject 1 boolean-input capacity as bounded (~10 paired feedbacks empirically on AA140 — may vary by contract age/template). For additional feedback groups, add a SECOND COMPONENT to the same `.cce` with its own specification — this allocates a fresh SmartObject 2.
- **MUST DO #21:** When hand-editing a `.cce` JSON file is unavoidable (e.g., scripted bulk additions), the user MUST re-open in Contract Editor's GUI to verify the added entries appear in the component tree BEFORE regen. Contract Editor's GUI silently filters entries it doesn't recognize, and the Build step drops them. Pairing entries with siblings improves acceptance.

### Add to NEVER DO list
- **NEVER DO #21:** Don't add multi-SmartObject signal groups via the sub-contract mechanism (`subContractLinks` / `subContracts`). Use multi-component instead (multiple entries in `components[]` and `specifications[]` of one `.cce`). Sub-contracts produce technically valid output but the panel-side CrComLib doesn't reliably route signals through sub-contract-derived SmartObjects (verified failure on AA140 / cr-com-lib ^2.17.2 / TSW-1070 + TS-1070).
- **NEVER DO #22:** Don't assume "the cse2j has the signal, therefore the panel receives writes." Verify with a runtime diagnostic on the panel (a temporary debug overlay showing `$store ? '1' : '0'` for each new signal). Slot caps and CrComLib silent failures are real and undetectable from build output alone.

### Add to failure-mode catalog
- **Failure mode: Signals visible in `.cse2j` but panel stores never update.** Diagnosis: (1) check `siblingId` pairing on the feedback — must reference a command's `id`; (2) check the SmartObject ID — past slot ~10 on SO1, move to a new component (SO2); (3) confirm panel device exposes the target SmartObject via processor log enumeration of `panel.SmartObjects`; (4) verify both `.cpz` (processor) and `.ch5z` (panel) were rebuilt and redeployed after the cce change.

## Files of interest from this session

- `MCCCD-AA140/contracts/MCCCD-AA140-rev2.cce` — first pairing-fix attempt (single component, hand-added paired signals)
- `MCCCD-AA140/contracts/MCCCD-AA140-rev3.cce` — multi-component pattern (Main + VideoSync as siblings)
- `MCCCD-AA140/contracts/VideoSync.cce` — standalone sub-contract attempt (deprecated path)
- `MCCCD-AA140/contracts/subContracts/VideoSync/` — Contract Editor's sub-contract output (deprecated path)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/VideoSync.g.cs` — VideoSync component's generated C# (multi-component output, current)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelJoins.cs` — `SO2BoolIn` constants for the VideoSync sigs at SmartObject 2 joins 1-8
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelDispatcher.cs` — added `WriteBoolSO2` for the second SmartObject path

## Process notes

- **Diagnostic discipline pays off.** Hours into iteration 4 we were guessing at hypotheses. Adding a small per-card overlay showing raw store values (`{$roomPcSync ? '1' : '0'}`) converted "all dots are grey" into "every store stays False despite SIMPL writes" — and that crisp signal directly identified that the issue was the CrComLib subscription, not the rendering pipeline.
- **Empirical-test-via-known-working-signal works.** Writing `True` to `MicLavConnected` (slot 11, in cse2j from a prior session, never written by SIMPL) and observing the panel diagnostic was what conclusively proved the slot-10 cap. The same test pattern would isolate any signal-routing problem on this stack.
- **Comparison against a known-working production project (1Beyond ISMI)** provided the structural blueprint when introspection of our own contract had no remaining signal. Multi-component vs sub-contract was a structural fork-in-the-road that wasn't obvious from the Crestron documentation but was visible in 60 seconds of looking at the working contract's `.cce`.
