# 2026-05-29 — Reflected Ceiling Routing + D4 Podium + Reusable AppFooter

**State at end of session.** Major panel work: replaced the matrix Display
Routing page with a reflected-ceiling plan + inline source popover, wired
the full route loop through SIMPL#, added a 4th display (D4 podium
confidence monitor), polished the room layout to match a physical-room
sketch, rebuilt the bottom-bar footer with borderless mics + bold +/- vol,
and refactored the footer into a reusable MCCCD-standard module ready for
the next room project.

All work landed on `feat/drag-drop-router-mockup`. Branch tip: `be89cba`.
Both panels (TS-1070 .80 + TSW-1070 .78) and the RMC4 processor (.198)
are running the latest binaries.

## Restore points (in landed order)

| Tag | Commit | What it delivers |
|---|---|---|
| `checkpoint-footer-shipped` | `a4b92b0` | Working footer: V4 waveform mics + V2 chip power + F bold +/- vol. Monolithic single-file `AppFooter.svelte`. Pre-refactor restore point. |
| `checkpoint-footer-reusable` | `be89cba` | AppFooter extracted to `src/components/AppFooter/` (reusable MCCCD standard) + `Aa140Footer.svelte` (room-specific adapter). README has API + adoption checklist for the second room project. |

`feat/drag-drop-router-mockup` is at `be89cba`. `main` is still at the
2026-05-28 handoff (`2dff061`) — this session's arc hasn't been merged
yet; merge when convenient.

## Architecture changes worth knowing about

### Display routing is now a top-down ceiling plan

`pages/DisplayRouting.svelte` was a source-list + matrix layout. It's now
a top-down room schematic with tappable display markers. Tap → inline
popover anchored to the marker; pick a source → routes via NVX and the
marker goes solid orange when SIMPL echoes feedback.

Layout matches the user's reference image (2026-05-29): front of room is
the *bottom* of the diagram. D1/D2 projection surfaces on the bottom wall
flank Cam1; D3 Newline at top-left aligned with a rear-wall speaker bar;
D4 podium confidence monitor + a small podium frame at the room center,
shifted toward the front. Speakers on the front wall align horizontally
with the D1/D2 markers so each display reads as a unit. No projector
diamonds, no conference table, no rear-right speaker (all removed per
user spec).

### D4 podium decoder fully wired

A 4th DM-NVX-D30 decoder at IPID `0x24`, treated as a "podium confidence
monitor" — defaults to D3's source on PowerUp so the presenter sees the
rear-of-room display, but is independently routable at runtime.

Contract changes (`.cce` + `.cse2j` patched by hand — Contract Editor
GUI not available in this terminal flow):
- `Display4Source` → SO 1 join 20 (panel publish, UShortOut)
- `Display4SourceFb` → SO 1 join 23 (SIMPL writes back, UShortIn)
- `Display4PowerFb` → SO 1 join 16 (SIMPL writes back, BoolIn)

`PanelJoins.cs` updated with all three. `Main.g.cs` was NOT regenerated
— we go through `PanelDispatcher` for D4 like every other working service,
so the Contract Editor wrapper isn't needed. **Heads up:** next time
Contract Editor is run, it will regenerate `Main.g.cs` (adding D4
event/method definitions) and may reshuffle join numbers if it picks
its own assignments. Re-sync `PanelJoins.cs` from the regenerated
`.cse2j` if that happens.

### Feedback fix: write Display{N}SourceFb via PanelDispatcher

`NvxRoutingService.RouteSourceToDisplay()` drives the "active source"
feedback via `_c.AA140.Display{N}Source((sig, m) => ...)` — a Contract
Editor wrapper. That wrapper has misaligned joins per the `PanelJoins`
doc and in practice the panel never saw the input update for these
displays — routes happened but markers stayed gray.

`SystemPowerController` now writes `Display{N}SourceFb` directly via
`PanelDispatcher.WriteUShort` in the source-select handlers and the
PowerUp/PowerDown sequences. The Contract Editor write in
`NvxRoutingService` is left in place — harmless if dead, redundant if
alive. **Pattern to follow:** for any new SIMPL→panel write, prefer
`_panel.WriteUShort`/`WriteBool` via `PanelJoins.UShortIn`/`BoolIn`
constants over the Contract Editor wrappers.

### SystemPowerController owns the runtime routing handlers

The OnUShort handlers for `Display{1,2,3,4}Source` live in
`SystemPowerController.Initialize()`, not in `NvxRoutingService`. Each
handler:
1. Updates `_lastDN` (D1, D2, D3 tracked; D4 doesn't need state because
   PowerUp seeds D4 from D3).
2. Calls `_nvx.RouteSourceToDisplay(value, N)`.
3. Writes the matching `Display{N}SourceFb` via PanelDispatcher.

`PowerUpSequence()` now restores all four displays, with D3 = `_lastD2`
(one-shot mirror) and D4 = `_lastD3`. `PowerDownSequence()` clears all
four routes + writes 0 feedbacks.

### AppFooter is now a reusable MCCCD-standard module

`src/components/AppFooter/` is the standard. Pure presentational — no
imports from `lib/contract`, `lib/CrComLib`, or `lib/stores/signals`.
Takes props (`power`, `mics`, `volume`, `shutdownItems`, plus action
callbacks) and renders the locked visual: V2 inline-chip power +
V4 waveform mics + F bold +/- vol. Owns the `ConfirmShutdownModal` and
`VolumePopup` internally — those are part of the standard UX.

`src/components/Aa140Footer.svelte` is the room-local adapter: it
imports the AA140 stores + `SIGNALS` + `CrComLib` and wires them to
`<AppFooter>` props. Both `Home.svelte` and `DisplayRouting.svelte`
just render `<Aa140Footer />` — the wiring lives in one place.

`src/components/AppFooter/README.md` is the source of truth for the
public API + integration recipe + adoption checklist. **Hard rule
documented there:** visual styling (sizes, colors, animations,
proportions) is NOT props-configurable. A future room wanting a
visually different footer builds a variant component rather than
parameterizing this one. The standard only stays meaningful if the
boundary holds.

Two new CSS tokens were added to `global.css`:
- `--color-mic-live` (`#4ade80`)
- `--color-mic-muted` (`#fca5a5`)

Total token surface for a room to define: 8 (the 6 existing accent/copy
tokens + these 2).

### Mockup archive

Three rounds of mockups produced this session live under
`MCCCD-AA140/docs/mockups/`:
- `rcp-routing/` — 3 takes on the reflected-ceiling routing page (Mockup
  B chosen)
- `footer-variants/index.html` — 5 borderless-mic variants (V4 picked
  for mics, V2 for power)
- `footer-variants/vc-variants.html` — 5 vol variants on the "65 readout"
  language
- `footer-variants/vc-clarity.html` — 7 vol variants focused on up/down
  clarity (F bold +/- picked)

Static HTML, viewable from any local Python server. Useful reference
when designing the next standard component.

## What's deferred / next-session pickup list

1. **Merge `feat/drag-drop-router-mockup` to `main`.** The branch has
   accumulated this whole arc (RCP routing UI, route-loop wiring, D4,
   feedback fix, AppFooter extraction). Tip: `be89cba`. Clean to merge.

2. **`RoutingMode` / `AutoRouteEnable` / `MirrorAllSame` not in contract.**
   These three signals are declared in `contract.ts` and the
   `DisplayRouting.svelte` header toggles them, but the `.cce` has no
   matching signals — publishing them is currently a no-op. Either add
   them to the `.cce` and wire SIMPL handlers, or remove the UI controls.
   No SIMPL logic implements auto-route / mirror-all yet either way.

3. **NVX hardware ↔ code mismatch.** `NvxRoutingService.cs` still
   instantiates 3 E30 encoders + 1 NVX-384, but the BOM (`reference_mcccd_aa140_equipment.md`)
   is 3 E30 + 5 D30 + *no* NVX-384. The D4 decoder addition this session
   brings the D30 count to 4 — still one short. Reconcile when the
   hardware is on site. Source 4 (`Laptop`) currently routes through
   the NVX-384 stream URL which doesn't exist physically.

4. **Contract Editor regeneration of `Main.g.cs`.** When someone re-runs
   the Contract Editor GUI, it'll regenerate `Main.g.cs` and pick its
   own join numbers. The hand-patched `cse2j` already has Display4*
   joins assigned (20 / 23 / 16) but those may shift. Mitigation:
   re-sync `PanelJoins.cs` constants from the regenerated `cse2j`
   immediately after any Contract Editor run.

5. **Command replay (carried over from 2026-05-28 handoff item 1).**
   Re-fire a previous event by correlationId via a `/replay?corr=cN`
   endpoint server-side + a "replay" button in each `command` row's
   expanded view in the debug UI.

6. **`nvx-nvx-384` double-prefix cosmetic** (carried over from 2026-05-28
   handoff item 2). Trivial special-case fix in `NvxRoutingService`'s
   prefix construction.

7. **Filter state persistence / Light mode toggle / Dynamic Sony/Newline/
   AirMedia badges / Per-MXA card** — all carried over from the
   2026-05-28 handoff items 3, 4, 5, 6. No new work this session.

8. **Worktree cleanup.** No new worktrees created this session. The six
   from the 2026-05-28 arc are still on disk under `.claude/worktrees/`.
   Safe to remove per the previous handoff.

9. **Second room project pickup.** This session set up the reusable
   `AppFooter` specifically because a second MCCCD room project is
   coming. The README in `src/components/AppFooter/` is written for
   that audience — give them the link plus the 3-step adoption
   checklist when they spin up the new repo.

10. **Equipment list xlsx files in working tree** (carried over from
    2026-05-28 handoff item 7). `MCCCD_AA140_Equipment_List.xlsx` is
    modified + auto-recover copy untracked. Same as last session — not
    part of any arc, leave alone or commit if appropriate.

## Process notes from this session

- The brainstorming → static HTML mockup → user picks → build flow
  worked very well for the footer iteration. Three rounds of mockups
  (5 + 5 + 7 = 17 footer variants) produced before any production code
  changed. The mockups are archived for design-system reference.

- The hand-edit-the-cse2j approach for adding D4 worked but is fragile.
  If MCCCD is going to add more signals over time, get the Contract
  Editor GUI into the flow on a Windows box, or write a small CLI that
  parses `.cce` and outputs `.cse2j` + `Main.g.cs` deterministically.

- The "reusable component + room-local adapter" pattern (`AppFooter/`
  + `Aa140Footer.svelte`) is the right shape for school-wide standards.
  Apply it to anything else that needs to look identical across rooms
  (header bar, splash, audio mixer chrome, etc.) as those become
  candidates for standardization.

- `npm run deploy:both` is now the default (set in memory). Both panels
  always get pushed simultaneously per user spec.

## File index of work added this session

### New
- `MCCCD-AA140/docs/superpowers/specs/2026-05-29-rcp-routing-design.md` — RCP routing spec
- `MCCCD-AA140/docs/mockups/rcp-routing/` — 4 HTML files (index + 3 alternatives)
- `MCCCD-AA140/docs/mockups/footer-variants/` — 3 HTML mockup rounds
- `MCCCD-AA140/src/components/routing/RoomPlan.svelte` — reflected-ceiling-plan composite
- `MCCCD-AA140/src/components/routing/DisplayMarker.svelte` — tappable display rectangle
- `MCCCD-AA140/src/components/routing/SourcePopover.svelte` — anchored picker with flip-above + arrow tracking
- `MCCCD-AA140/src/components/routing/DisplayStatusCard.svelte` — sidebar status row
- `MCCCD-AA140/src/components/AppFooter/` — reusable MCCCD-standard module (AppFooter.svelte + types.ts + index.ts + README.md)
- `MCCCD-AA140/src/components/Aa140Footer.svelte` — room-local adapter

### Modified (panel UI)
- `MCCCD-AA140/src/pages/DisplayRouting.svelte` — full rewrite to RCP layout; later refactored to use `<Aa140Footer />`
- `MCCCD-AA140/src/pages/Home.svelte` — selectSourceForAll extended to D4; inline footer replaced with `<Aa140Footer />`
- `MCCCD-AA140/src/lib/contract.ts` — added display4Source / display4SourceFb / display4PowerFb keys
- `MCCCD-AA140/src/lib/stores/signals.ts` — added display4SourceFb / display4PowerFb stores + subscriptions
- `MCCCD-AA140/src/lib/stores/router.ts` — DisplayId widened to include 'd4'; FB_BY_DISPLAY and SET_SIGNAL_BY_DISPLAY extended
- `MCCCD-AA140/src/global.css` — added `--color-mic-live`, `--color-mic-muted` tokens

### Modified (contract + SIMPL#)
- `MCCCD-AA140/contracts/MCCCD-AA140.cce` — added Display4* signals
- `MCCCD-AA140/contracts/output/MCCCD_AA140/interface/mapping/MCCCD_AA140.cse2j` — hand-patched with new D4 joins
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelJoins.cs` — added 3 D4 constants
- `MCCCD-AA140-SIMPL/MCCCD-AA140/NvxRoutingService.cs` — added 4th D30 @ IPID 0x24, GetDecoder case 4, arrays grown to 5, ReapplyRoutesForSource loop to 4
- `MCCCD-AA140-SIMPL/MCCCD-AA140/SystemPowerController.cs` — Display{1,2,3,4} OnUShort handlers + PanelDispatcher feedback writes; `_lastD3` added; PowerUp seeds D4 from D3; PowerDown clears D4

### Removed
- `MCCCD-AA140/src/components/AppFooter.svelte` — monolithic single-file footer (replaced by `AppFooter/` folder + `Aa140Footer.svelte` adapter)

## Quick pickup recipe for the next session

```bash
# Confirm you're on the right tip
cd "C:\Users\scale\CascadeProjects\Archon-Tests\MCCCD Razzle"
git checkout feat/drag-drop-router-mockup
git log --oneline -1   # expect be89cba

# Verify panels + processor are running the latest
curl -sI https://192.168.2.78    # TSW-1070 wall
curl -sI https://192.168.2.80    # TS-1070 tabletop
curl -sI https://192.168.2.198   # RMC4 processor

# Re-deploy from clean if needed
cd MCCCD-AA140 && npm run deploy:both           # panel UI to both panels
cd ..
cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=192.168.2.198 python scripts/deploy.py \
    MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz   # .cpz to RMC4

# Read this handoff + the AppFooter README before touching code
less MCCCD-AA140/docs/Handoffs/2026-05-29-rcp-routing-d4-footer.md
less MCCCD-AA140/src/components/AppFooter/README.md
```

## Where to start next session

In rough priority order:

1. **Merge `feat/drag-drop-router-mockup` to `main`** if the user
   confirms the panel state looks right. Branch has been the working
   tip for a while; main is stale at the 2026-05-28 handoff commit.
2. **Reconcile the NVX hardware count** (pickup list item 3) if/when
   the actual hardware is on site. The IPID map needs to match the
   real BOM (5 D30 decoders, no NVX-384).
3. **Decide on RoutingMode / AutoRouteEnable / MirrorAllSame** (pickup
   list item 2). Either implement them on the SIMPL side + add to
   `.cce`, or strip the UI controls.
4. **Spin up the second-room project** using the `AppFooter/` standard
   when that work starts. Follow the README's 3-step adoption checklist.
5. Items 5–7 (debug-panel polish from 2026-05-28) remain available but
   were not touched this session.

If switching focus entirely (Q-SYS, contract editor, classroom layouts,
etc.), this branch / tip is the right baseline to fork from.
