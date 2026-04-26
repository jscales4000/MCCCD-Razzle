# Crestron Contract Editor — Hand-Authoring Constraints & Lessons

**Document type:** Lessons Learned
**Applies to:** Crestron Contract Editor (Windows GUI tool, ships with Crestron Toolbox), `.cce` source files, generated `.cse2j` / `.chd` / `.g.cs` build outputs
**Tags:** crestron, ch5, contract, contract-editor, cce, lessons-learned, build, simpl-sharp
**Date:** 2026-04-26
**Origin incident:** MCCCD-AA140 panel project — `Errors.Contract.min` rejection on first Contract Editor build

---

## TL;DR — the must-haves and must-nots when hand-editing a `.cce`

### MUST

- Keep `description` field **short** (≤ 60 chars is safe; longer values trigger `Errors.Contract.min`)
- Keep **all string fields strict ASCII** — no em-dash (—, U+2014), en-dash (–, U+2013), curly quotes (", ", ', '), or other non-ASCII punctuation
- Use **plain hyphen** (`-`) and ASCII quotes (`"`, `'`) only
- Set `schemaVersion: 1`
- Each command/feedback object must have ALL of: `Errors`, `name`, `siblingId`, `dataType`, `notes`, `id`, `parentId`, `attributeType` (no shortcuts)
- For paired command/feedback: each entry's `siblingId` points at the OTHER's `id` (mutual reference)
- For pulse commands without a feedback (e.g. `VolumeUp`): set `siblingId` to empty string `""`
- `dataType`: `1` = Bool, `2` = Analog, `3` = String
- `attributeType`: `0` = command (panel→processor), `1` = feedback (processor→panel)
- Component / signal **names must be valid C# identifiers** — PascalCase, no spaces, no special chars (the generated `.g.cs` uses these as property names)
- Specifications array entries must reference an existing `componentId`

### MUST NOT

- **Never hand-author `.cse2j`, `.chd`, or `.g.cs`.** These are Crestron Contract Editor build outputs. Hand-written `.cse2j` silently crashes CrComLib on the panel — no error, just dead joins.
- Don't use em-dash, en-dash, or any character above U+007F in any string field
- Don't put long marketing copy in `description` — it's used as a contract-level identifier, not documentation
- Don't reuse `id` values across entries (must be unique within the file)
- Don't break siblingId pairings when refactoring — both sides must still resolve

---

## Workflow Doctrine

The `.cce` is the **source of truth**. Edit it; let Contract Editor produce build outputs.

```
┌─────────────────┐     edit     ┌─────────────────┐
│ MCCCD-AA140.cce │ ────────────►│ Contract Editor │ (Windows GUI)
└─────────────────┘              │     Build       │
                                 └────────┬────────┘
                                          │ generates
                          ┌───────────────┼───────────────┐
                          ▼               ▼               ▼
                 ┌────────────────┐ ┌──────────┐ ┌─────────────────┐
                 │ <Name>.cse2j   │ │ <Name>.  │ │ <Name>.g.cs     │
                 │ (panel join    │ │ chd      │ │ (SIMPL# Pro     │
                 │  map)          │ │          │ │  contract class)│
                 └────────┬───────┘ └────┬─────┘ └────────┬────────┘
                          │              │                │
                          ▼              ▼                ▼
                 public/config/    public/config/   SIMPL# Pro project's
                 (panel project)   (panel project)  Generated/ folder
```

After **any** `.cce` edit:
1. Open in Contract Editor → click Build
2. Drop `.cse2j` + `.chd` into `public/config/` of the panel project
3. Drop `.g.cs` into the SIMPL# Pro project's `Generated/` folder
4. Rebuild + redeploy **both** the `.cpz` and the `.ch5z`. Mismatched outputs → silent join failures on the panel.

---

## Confirmed Failures and Fixes

### Failure: `Errors.Contract.min` on the description field

**What happened (MCCCD-AA140, 2026-04-26):**
First Build attempt rejected the contract with:
```
Contract MCCCD_AA140 contains the following error:
Contract with Property description has the invalid value:
"MCCCD room AA140 — CH5-Svelte panel + SIMPL# Pro control system. 3 NVX D200 displays, 5 logical sources via 3xE30+1xNVX-384 (HDMI/USB-C combo), 3 1Beyond cameras (Front i20 + 2x Back i12), Q-SYS Nano DSP, PoE occupancy."
Reason: Errors.Contract.min
```
Original was **270 characters with em-dashes**.

**Fix:** trim to ≤ 60 chars and strip non-ASCII:
```
"description": "AA140 panel: 3 displays, 5 sources, 3 cameras, Q-SYS audio."
```
Worked on retry.

**Hypothesis on the error name:** "min" likely refers to a min/max validation rule failing the upper bound — Crestron's validation framework uses `Errors.Contract.min` and `Errors.Contract.max` symmetrically. Confusingly, the rule name "min" was triggered by being too long. Don't get hung up on the label; treat any `Errors.Contract.*` on a string field as **trim and ASCII-clean**.

### Likely Failure (preemptive): Notes fields with non-ASCII

The `notes` field on each command and feedback **probably** has the same constraints as `description`. Strip em-dashes preemptively even if the file currently builds — they may surface as errors after a Contract Editor version bump.

In the MCCCD-AA140 fix, all `notes` em-dashes were converted to hyphens preemptively. The build then succeeded.

---

## Required `.cce` Structure (cheat sheet)

Top-level shape:
```json
{
  "Errors": [],
  "id": "_<unique>",
  "name": "<ContractName>",
  "description": "<short ASCII description>",
  "company": "<short>",
  "client": "<short>",
  "author": "<short>",
  "version": "1.0.0.0",
  "schemaVersion": 1,
  "subContractLinks": [],
  "subContracts": [],
  "specifications": [
    {
      "Errors": [],
      "parentId": "<contract id>",
      "id": "_<unique>",
      "componentId": "<existing component id>",
      "instanceName": "<PascalCase>",
      "numberOfInstances": 1
    }
  ],
  "components": [
    {
      "Errors": [],
      "parentId": "<contract id>",
      "id": "<used as componentId in specifications>",
      "name": "<PascalCase>",
      "commands": [ /* see below */ ],
      "feedbacks": [ /* see below */ ],
      "specifications": []
    }
  ],
  "allComponentsForAllContracts": []
}
```

Command / feedback entry:
```json
{
  "Errors": [],
  "name": "<PascalCaseSignalName>",
  "siblingId": "<paired entry's id, or empty string>",
  "dataType": 1,
  "notes": "<short ASCII notes>",
  "id": "<unique within file>",
  "parentId": "<containing component id>",
  "attributeType": 0
}
```

Where:
- `dataType`: `1` = Bool, `2` = Analog (UShort/UInt), `3` = String (Serial)
- `attributeType`: `0` = command (down to processor), `1` = feedback (up to panel)
- `siblingId`: pair commands with feedbacks for two-way state (Set + Fb pattern). Empty string for pulse commands without paired feedback.

---

## Pairing Patterns

### Pattern 1 — Set + Feedback pair (state with confirmation)

Use for: source select, audio routing, tracking mode, volume level, etc. Anything where the panel sends a desired value and the processor confirms by publishing the actual value.

```json
{ "name": "Display1Source",   "siblingId": "_b002", "dataType": 2, "attributeType": 0, "id": "_a002", ... },
{ "name": "Display1SourceFb", "siblingId": "_a002", "dataType": 2, "attributeType": 1, "id": "_b002", ... }
```

### Pattern 2 — Toggle level + Feedback (boolean state with feedback)

Use for: mic mute, light state, etc.

```json
{ "name": "MicLavMute",   "siblingId": "_b006", "dataType": 1, "attributeType": 0, "id": "_a011", ... },
{ "name": "MicLavMuteFb", "siblingId": "_a011", "dataType": 1, "attributeType": 1, "id": "_b006", ... }
```

### Pattern 3 — Pulse command (no feedback)

Use for: vol up/down, mute toggle, ptz directional, mirror-to-X, "send to VTC" — any fire-and-forget action.

```json
{ "name": "VolumeUp", "siblingId": "", "dataType": 1, "attributeType": 0, "id": "_a008", ... }
```

### Pattern 4 — Feedback-only (processor publishes, panel listens)

Use for: occupancy state, shutdown countdown, panel-online status, NVX active-input, mic level meters.

```json
{ "name": "OccupancyState", "siblingId": "", "dataType": 2, "attributeType": 1, "id": "_b009", ... }
```

---

## SIMPL# Pro Side: What Contract Editor Generates

After Build, `<Name>.g.cs` exposes a class typically named `MainContract` (matches `instanceName`). Each command/feedback becomes a property:

| `.cce` entry                        | Generated C# property                              |
|------------------------------------|----------------------------------------------------|
| Command, dataType 1 (Bool)         | `BoolInputSig <Name>` with `OnDigitalChange`, `OnDigitalRise`, `OnDigitalFall` events |
| Command, dataType 2 (Analog)       | `UShortInputSig <Name>` with `OnAnalogChange` event |
| Command, dataType 3 (String)       | `StringInputSig <Name>` with `OnSerialChange` event |
| Feedback, dataType 1 (Bool)        | `BoolOutputSig <Name>` — set `.BoolValue` to publish |
| Feedback, dataType 2 (Analog)      | `UShortOutputSig <Name>` — set `.UShortValue` to publish |
| Feedback, dataType 3 (String)      | `StringOutputSig <Name>` — set `.StringValue` to publish |

> **Note:** exact event accessor names vary across Crestron Contract Editor versions. If your generated `.g.cs` has slightly different names (e.g. `OnUShortChange` vs `OnAnalogChange`, `UShortValueChanged` vs `OnAnalogChange`), adapt to match.

The constructor signature for `MainContract` depends on how many panels register against the contract:
- Single-panel: `new MainContract(panel)`
- Multi-panel: `new MainContract(panel1, panel2, ...)`
- Some Contract Editor versions generate an `AddDevice(panel)` method instead of variadic ctor

Adjust your SIMPL# `ControlSystem.cs` to match. The generated file is the authority.

---

## Panel Side (TypeScript / Svelte)

The `.cse2j` file (generated alongside `.g.cs`) is loaded by the panel runtime. It maps signal names like `${ROOM_NAME}.Display1Source` to the join numbers the panel uses internally.

In CH5 + Svelte projects:
- Hand-maintain a `src/lib/contract.ts` `SIGNALS` object whose keys mirror the `.cce` entries — **two-place maintenance** but type-safe in panel code.
- Every signal added to the `.cce` MUST also be added to `SIGNALS` in `contract.ts`.
- Drift between the two = silent join failures on the panel.

Tip: name the TS keys camelCase versions of the `.cce` PascalCase names, e.g. `display1Source` for `Display1Source`. The string value should be `\`${ROOM_NAME}.Display1Source\`` (template literal with the room namespace prefix).

---

## Symptoms of Common Mistakes

| Symptom on the panel                         | Likely cause                                                    |
|----------------------------------------------|------------------------------------------------------------------|
| Buttons render but nothing happens on tap    | Mismatched `.cse2j` and `.cpz` (forgot to redeploy one side)    |
| Panel shows "Offline" indefinitely           | Processor program not loaded, or PanelOnline feedback missing from `.cce` |
| Some signals work, others don't              | Signal name in `contract.ts` doesn't match `.cce` exactly (case, spelling) |
| All signals dead, no log errors              | Hand-edited `.cse2j` (silent CrComLib crash) — rebuild from `.cce` |
| Pulse commands don't fire                    | Sent only the `true` edge — must follow with `false` (or use a `pulseDigital` helper) |
| Contract Editor `Errors.Contract.min`        | Description / notes too long or contains non-ASCII chars         |
| Generated `.g.cs` has no events              | `.cce` siblingId pairings broken — Contract Editor builds but skips event generation |

---

## Build Cycle Discipline (one-line summary)

> **Edit `.cce` → Build in Contract Editor → drop outputs in their two homes → rebuild + redeploy BOTH `.cpz` and `.ch5z`.** Skipping any step produces silent failures.

---

## Persona Prompt Seed

If/when this gets distilled into a Crestron Contract persona, the must-have content for the persona's instructions:

1. The TL;DR section above (must / must-not lists)
2. Workflow doctrine diagram + redeploy rule
3. The four pairing patterns (Set+Fb, Toggle+Fb, Pulse, Feedback-only)
4. The "Symptoms of Common Mistakes" troubleshooting table
5. Plain ASCII enforcement is non-negotiable; em-dashes are the #1 trap when copy-pasting from Markdown specs into the `.cce`

The persona should:
- Refuse to hand-author `.cse2j`, `.chd`, or `.g.cs`
- Always ask: "have you re-deployed BOTH the `.cpz` and the `.ch5z` since this `.cce` change?"
- Cross-reference the panel's `contract.ts` SIGNALS object whenever editing the `.cce` (and vice versa)
- Generate IDs systematically (e.g. `_a001` for commands, `_b001` for feedbacks) for hand-edited `.cce` files even though Contract Editor will regenerate them at Build time

---

## Provenance

This document was created from direct experience hand-editing the `MCCCD-AA140.cce` for the MCCCD AA140 panel project on 2026-04-26. The `Errors.Contract.min` failure on first Build, the em-dash hypothesis, and the trim-to-60-char fix were all observed and verified in that session. The remaining sections (pairing patterns, generated property names, symptoms table) reflect Crestron Contract Editor v2 conventions known at the time.

**Sources to consult for canonical / current behavior:**
- Crestron Contract Editor's own validation messages (treat them as authoritative for length caps)
- Generated `.g.cs` files for the exact property names your installed Contract Editor produces
- FRED CH5 Contract Workflow Doctrine document (`b9d287cb-6bce-4911-8049-65aa6ef7f77d`) referenced by the ch5-svelte-v2 scaffold's TEMPLATE-README.md
