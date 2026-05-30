# 2026-05-30 — Source FB Optimistic Pattern + Page-Suite Mockups

**State at end of session.** Source-routing feedback is now visually working
on the panel via an optimistic local-mirror pattern; the underlying CrComLib
analog-subscribe bug remains unfixed but boxed-in. Marker visual stripped
back to D# badge + source name in lemon yellow. Contract Editor rerun
landed a clean Main.g.cs + cse2j. Fifteen page-suite mockups (5 each of
advanced routing, cameras, audio mixer) generated under a locked design
system as reference for the next round of page restyling.

All work landed on `main`. Tip: `<set after handoff commit>`.
Both panels (TS-1070 .80 + TSW-1070 .78) and the RMC4 processor (.198)
are running the latest binaries.

## Restore points (in landed order)

| Tag | Commit | What it delivers |
|---|---|---|
| `v-2026-05-29-rcp-routing` | `1ae01e8` | Merge of the 2026-05-29 arc (RCP routing, D4 podium, reusable AppFooter) into main. Pre-this-session restore point. |
| `v-2026-05-30-source-fb-page-suite` | `<set after handoff>` | Optimistic source FB + marker overhaul + 15-mockup page suite. End-of-this-session restore point. |

## Architecture changes worth knowing about

### Source feedback now uses an optimistic local-mirror

`src/lib/stores/router.ts` is the centralized source-routing API:

- `routeSource(sourceId, displayId)` — publishes `Display{N}Source` to SIMPL **and** writes the same numeric value into `display{N}SourceFb` locally.
- `clearDisplay(displayId)` — publishes 0 + clears local store.
- `routeSourceToAll(value)` — applies a single source to all four displays.

`DisplayRouting.svelte` (popover select + clear) and `Home.svelte`
(select-source-for-all) both route through these helpers. There are no
remaining direct `publishAnalog(SIGNALS.display{N}Source, ...)` calls in
panel UI code; the optimistic mirror is unmissable for any new caller.

The marker / sidebar continue to read from the `display{N}SourceFb`
Svelte stores. From their perspective nothing changed — they just see
the value update immediately on tap rather than after a (broken) SIMPL
roundtrip.

### Why optimistic — the underlying CrComLib analog bug

Confirmed via empirical diagnosis on the real panels + processor:

1. SIMPL receives the panel's `Display{N}Source` publish — `PanelDispatcher: ushort join=N dispatched=True` appears in `err`. Routes happen.
2. SIMPL writes `Display{N}SourceFb` back via `PanelDispatcher.WriteUShort` — the sig is non-null, the write throws no exception.
3. We patched `WriteUShort` to write to **both** `SmartObjects[1].UShortInput[join]` and **top-level** `UShortInput[join]`. Neither lights the panel-side subscriber. (Patch then reverted — it's a confirmed non-fix, no need to ship it.)
4. **Booleans on the same SmartObject deliver fine** — `subscribeDigital(SIGNALS.systemPowerFb)` reflects state, mic mutes toggle visually.
5. Therefore the break is on the panel side, specifically in CH5 CrComLib's `subscribeState('n', ...)` not firing for SmartObject UShortInput updates. Booleans use a different path.

**Action queued:** see "Deferred / next-session pickup" #1. Don't waste
debugging time re-confirming the SIMPL write path; that's settled.

### Marker visual is now badge + source line only

`src/components/routing/DisplayMarker.svelte` shows: `[D1] ▸ Source Name`.

Removed: location label ("Front Left"), spec text ("Sony VPL · 100" Projection"). Both still exist as props for callers' future use but render nowhere; the `label` prop is retained inside `aria-label` for screen readers.

All marker text — D# badge + source line + the "— No Source" placeholder — uses lemon yellow `#fde047`. The active accent for routed sources, applied uniformly so the eye doesn't have to switch between two yellows. The `.off` state still drops everything to muted grey for powered-off displays.

### Contract Editor regen landed cleanly — but stripped Display4PowerFb

Ran Contract Editor on the .cce. Output replaces the hand-patched cse2j
and Main.g.cs from the 2026-05-29 arc. D4 entries (`Display4SourceFb`
at joinId 23, `Display4Source` at joinId 20) are present and match
the previous hand-patch numerically.

**Regression to fix:** Contract Editor dropped `Display4PowerFb` from
the cse2j. The signal is declared in the .cce but its `siblingId` is
empty string (every other paired signal references a sibling id);
Contract Editor seems to skip unpaired entries. The 2026-05-29 arc
hand-patched it back at joinId 16. **Pickup-list item below.**

`PanelJoins.cs` was not updated this session — its constants still match
the regenerated cse2j (no join shuffling). Re-sync only needed if Contract
Editor is re-run and joins shift.

### Page-suite mockup set

`docs/mockups/page-suite-2026-05-29/` holds 15 static-HTML mockups (5 each
of advanced routing, cameras, audio mixer) built against a locked
shared design system (`design-system.md`). One variant axis per page:
Spacious / Dense / Asymmetric / Card-Grid / Sidebar — same direction
in each file across pages so the suite reads as a unified product, not
15 random takes.

These are **reference designs only** — not wired in. Use them to decide
the direction of each page restyle, then implement against the chosen
variant. Browse via `index.html`.

Touch target floor: 64×64 (with deliberate exceptions noted per variant —
e.g. Cameras V3 zoom mini-buttons at 40px to support the asymmetric
direction).

## What's deferred / next-session pickup list

1. **Underlying CrComLib analog-subscribe bug.** This is the real bug. The
   optimistic pattern is a workaround. Investigation path: either restructure
   the .cce so feedback signals live at top-level (`smartObjectId: 0`) instead
   of as SmartObject 1 children — and rebuild contract + panel + SIMPL — or
   replace panel-side `subscribeAnalog` with a lower-level CrComLib API that
   bypasses whatever's broken. The audio mixer is currently impacted (next
   item) and any future analog FB will be too, so this can't stay deferred
   forever.

2. **Apply optimistic pattern to AudioMixer.** Mic trim sliders, line-out
   faders, master fader, audio output select all currently read from
   `*Fb` stores that never update (per the same CrComLib bug). User
   observation 2026-05-29: trim/line-out are zero'd and greyed; master
   stuck at -60dB. Same fix pattern as `router.ts`: mirror every publish
   into the local store. Easier than the source FB because AudioMixer
   is panel-driven anyway (the user IS the source of truth) — no external
   state to sync.

3. **Restore Display4PowerFb in the .cce.** Pair it with a sibling so
   Contract Editor preserves it on the next regen. Currently the signal
   has empty `siblingId` and gets dropped. Until fixed, D4 power feedback
   in `PanelDispatcher.WriteBool(PanelJoins.BoolIn.Display4PowerFb=16, ...)`
   from `SystemPowerController` writes to a slot the panel doesn't know
   about. Workaround: write Display4PowerFb to a top-level boolean for now,
   or just leave it dark since D4 always powers with the room.

4. **Page-suite mockup → implementation.** When ready, pick one variant
   per page from `docs/mockups/page-suite-2026-05-29/` and execute the
   restyle. Each variant explores a distinct direction so the choice
   should be deliberate (spacious for high-touch / dense for at-a-glance
   / asymmetric for one-focal-action / card-grid for equal-weight /
   sidebar for narrow-nav + main-canvas).

5. **`RoutingMode` / `AutoRouteEnable` / `MirrorAllSame`** (carried over
   from 2026-05-29 handoff item 2). Still declared in `contract.ts` with
   UI controls in the routing header, still no SIMPL handlers. User
   parked this with "wait on these". Decision still pending.

6. **NVX hardware reconciliation.** Per memory correction this session:
   actual room hardware is 3× E30 + 1× NVX-384 (as 4th TX) + 4× D30.
   Code is correct. Strike the 2026-05-29 handoff item 3.

7. **Command replay + nvx-nvx-384 cosmetic + 2026-05-28 debug-panel
   carryovers** — all still in the queue. No new work this session.

8. **Worktree cleanup** — six worktrees from 2026-05-28 still on disk
   under `.claude/worktrees/`. Safe to remove.

9. **Equipment xlsx files in working tree** — same ambient noise as
   prior handoffs. Leave alone unless committing.

## Process notes from this session

- Empirical-diagnostic-via-deploy worked cleanly for the analog FB bug:
  deploy SIMPL with `ErrorLog.Error`-prefixed diagnostic lines → tap on
  panel → `err` over SSH from paramiko → narrow possible causes one at
  a time. ~20 minutes of iteration to confirm SIMPL writes succeed and
  the bug is panel-side. Revert diagnostics before commit.

- "Optimistic mirror as workaround for unreliable FB" is a useful
  pattern to keep in the toolbox. Document the fact that it's a
  workaround in the helper itself (comment in `router.ts`) so future
  readers don't think it's the canonical design — they'll otherwise
  remove the SIMPL writes thinking they're dead code.

- Three parallel UI Designer agents (one per page) with a shared
  `design-system.md` spec produced 15 consistent mockups in ~15 minutes
  of wall-clock. The spec includes the variant axis (V1 Spacious / V2
  Dense / V3 Asymmetric / V4 Card-Grid / V5 Sidebar) explicitly so each
  agent picks one direction per file and the suite avoids redundancy.
  Re-use this pattern for future mockup rounds — design system file +
  one agent per page in parallel.

## File index of work added this session

### New
- `MCCCD-AA140/docs/Handoffs/2026-05-30-source-fb-optimistic-page-suite.md` — this doc
- `MCCCD-AA140/docs/mockups/page-suite-2026-05-29/design-system.md`
- `MCCCD-AA140/docs/mockups/page-suite-2026-05-29/index.html`
- `MCCCD-AA140/docs/mockups/page-suite-2026-05-29/{advanced-routing,cameras,audio-mixer}/V{1-5}-*.html` (15 files)

### Modified (panel UI)
- `MCCCD-AA140/src/lib/stores/router.ts` — added `clearDisplay`, `routeSourceToAll`; `routeSource` mirrors publish into FB store
- `MCCCD-AA140/src/pages/DisplayRouting.svelte` — `onClearFromPopover` uses `clearDisplay`
- `MCCCD-AA140/src/pages/Home.svelte` — `selectSourceForAll` uses `routeSourceToAll`
- `MCCCD-AA140/src/components/routing/DisplayMarker.svelte` — badge + source line layout, all text lemon yellow, removed `.m-name` (location) and `.m-spec` (model) renders

### Modified (contract + SIMPL)
- `MCCCD-AA140/contracts/output/MCCCD_AA140/interface/mapping/MCCCD_AA140.cse2j` — Contract Editor regen output
- `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/Main.g.cs` — Contract Editor regen output (adds D4 event + method)

### Not changed (intentionally)
- `PanelJoins.cs` — constants still align with the regenerated cse2j; no re-sync needed
- `SystemPowerController.cs` + `NvxRoutingService.cs` — SIMPL write paths kept in place even though they don't currently reach the panel UI; they'll start working automatically when CrComLib bug is fixed

## Quick pickup recipe for the next session

```bash
# Confirm tip
cd "C:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle"
git checkout main
git log --oneline -1   # expect the handoff commit

# Verify panels + processor reachable
curl -sI https://192.168.2.78    # TSW-1070 wall
curl -sI https://192.168.2.80    # TS-1070 tabletop
curl -sI https://192.168.2.198   # RMC4 processor

# Re-deploy if anything's drifted
cd MCCCD-AA140 && npm run deploy:both           # panel UI to both panels
cd ..
cd MCCCD-AA140-SIMPL && \
  dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=192.168.2.198 python scripts/deploy.py \
    MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz

# Read this handoff + browse mockups before touching code
less MCCCD-AA140/docs/Handoffs/2026-05-30-source-fb-optimistic-page-suite.md
start MCCCD-AA140/docs/mockups/page-suite-2026-05-29/index.html
```

## Where to start next session

In rough priority order:

1. **Apply optimistic FB pattern to AudioMixer** (pickup #2). Same approach as
   `router.ts`, mirror every publish into the local store on the panel side.
   Once landed, mic trims / line-out faders / master / output-select all
   visibly work, even though the underlying CrComLib bug remains. Quick win.
2. **Pick a page-suite variant and implement.** Pick the strongest direction
   per page from `docs/mockups/page-suite-2026-05-29/`. Header restyle was
   the original ask (more touch real estate) — likely V1 Spacious or V3
   Asymmetric on routing.
3. **CrComLib analog-subscribe bug investigation** (pickup #1). The real fix.
   Probably a multi-hour arc — restructure the .cce or replace `subscribeAnalog`
   internals. Plan with the user before starting.
4. **`Display4PowerFb` siblingId fix** (pickup #3). Five-minute fix in the .cce
   + Contract Editor rerun + Generated/*.g.cs copy.
