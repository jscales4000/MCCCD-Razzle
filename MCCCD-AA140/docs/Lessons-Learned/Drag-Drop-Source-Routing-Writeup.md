# How We Built Drag-and-Drop Source Routing — A Retrospective

**Project:** MCCCD-AA140
**Period:** 2026-05-01 → 2026-05-02 (one driving session, multiple sub-sessions)
**Driver:** Jordan Scales
**Implementer:** Claude Opus 4.7 (1M context) via Claude Code
**Branch:** `feat/drag-drop-router-mockup`
**Status at write time:** Stage 2 deployed and running on TS-1070; the UX has been parked as an un-linked sub-page pending chrome realignment to Mockup 10
**Audience:** future agents (esp. FRED) and developers replicating this approach on other panels

---

## What this document is

A narrative, step-by-step retrospective of how the drag-and-drop source-routing UX was conceived, prototyped, ported to Svelte, deployed to hardware, tuned for capacitive touch, and ultimately parked. It reads like a logbook with rationale at each fork.

For the distilled patterns and anti-patterns (without the narrative), see [`Drag-Drop-Source-Routing-Lessons.md`](./Drag-Drop-Source-Routing-Lessons.md).

---

## The original ask

User said:
> I want to branch off and play with a drag-and-drop concept. I want to see if we could have a drag-and-drop control surface on the touchpanel to drag a source button onto a display location.

That sentence implied:
- **Branch off** — exploration, not main-track work
- **Play with** — exploratory, low commitment
- **Drag-and-drop … onto a display location** — replace the existing 12-button per-display source grid (4 buttons × 3 displays) with a unified drag-target gesture

Three things were *not* decided: artifact (mockup vs Svelte vs both), gesture style (long-press vs immediate vs tap-to-arm), or palette location (rail, dock, floating).

---

## Stage 1: brainstorm to mockup

### Why mockup-first

The first thing the agent surfaced was the choice of artifact:

> **What artifact do you want out of this branch?**
>
> 1. Static mockup only — `mockups/11-drag-drop-router.html`. Pure HTML/CSS/JS. Fast to iterate. Throwaway if it doesn't feel right.
> 2. Working Svelte prototype — wire to real `display{N}Source` analogs and deploy to TS-1070.
> 3. Both, in order — mockup first, then promote to Svelte if it survives.
>
> My recommendation: #3. Drag-and-drop on a 24" capacitive touchscreen has a lot of failure modes that don't show up in a mouse demo (accidental long-presses, palm rejection, no hover state, finger occlusion of the drop target). A 30-minute static mockup tells you whether the layout even makes sense before you spend a session wiring it up.

User picked #3. This decision saved real time later — Stage 1 caught the metaphor before any framework code was written, and Stage 2 was implementing a known-good design rather than discovering it.

### Six brainstorming forks

Per the [brainstorming skill](https://docs.anthropic.com/skills/superpowers/brainstorming), each fork was presented as 2–3 options with trade-offs and a recommendation. The user accepted my recommendation 4/6 times — but the two times they overrode were design-shaping. Worth listing:

1. **Replaces button grids vs coexists vs adds new moves** → user picked "replaces" (cleanest test)
2. **Long-press-arm vs immediate drag vs drag-only** → user picked "long-press + tap-to-arm fallback"
3. **Palette location: below tiles vs left rail vs right rail vs floating** → user picked "left rail"
4. **Routed-source representation: text only vs landed chip vs chip moves** → user picked "landed chip"
5. **What's draggable: rail chips only vs rail and landed chips** → user picked "rail only"
6. **Drop-on-already-routed: animation vs no-op snap-back** → I recommended "lower-emphasis no-op + snap-back"; spec'd it

### Spec: what we wrote down

The Stage 1 spec at `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md` captured:
- The two-stage prototype approach (Stage 1 = mockup, Stage 2 = Svelte port deferred)
- Layout: 96px left rail with 4 chips, three display tiles as drop zones, header + footer unchanged
- Interaction state machine: IDLE → ARMED (tap) → ROUTED (tap tile); IDLE → DRAGGING (long-press 250ms) → ROUTED (drop on tile) or back to IDLE (snap-back)
- Five visual states per chip and three per tile, with class-toggle CSS animations
- Three-phase drop animation (snap → thunk → flash, ~280ms total) and 220ms snap-back
- Multi-touch ignored, 250ms long-press threshold, 10px move-cancel

The spec was committed before the implementation plan, before any code.

### Plan: 8 tasks, each a commit

The [writing-plans skill](https://docs.anthropic.com/skills/superpowers/writing-plans) produced an 8-task plan:

1. Branch + skeleton file
2. Static visuals (header, rail chips IDLE, display tiles with initial routing, footer)
3. Tap-to-arm + tap-to-route flow
4. Disarm on tap-outside
5. Long-press → drag flow with chip clone
6. Drop animation + snap-back
7. Add gallery card
8. Final review against spec success criteria

Each task ended in a commit. Each task had verifiable steps. The plan did NOT use TDD because the artifact is a static HTML file with no test framework — instead, each task ended with "open in browser, verify X."

### Self-review caught two real bugs in the plan itself

Before dispatching implementers, the planner reviewed the plan with fresh eyes:

> Self-review found a real bug in Task 5 — pointer-move cancellation uses `e.movementX/Y` which is unreliable on touch, and there's a click-after-drag race that could re-arm a chip after a snap-back.

Both were fixed in the plan before the implementer ever saw it:
- `e.movementX/Y` was unreliable on touch → switched to comparing `e.clientX/Y` against stashed origin coordinates
- Click-after-drag race → introduced `appState.suppressNextClick` flag

If these had shipped, they'd have presented as flaky drag behavior on the panel.

### Subagent-driven execution

Each of the 8 plan tasks was executed by a fresh general-purpose subagent (no shared context with the planner). After each implementer reported DONE, two more subagents reviewed in sequence:

- **Spec compliance reviewer:** "did the implementer build what was asked, nothing more, nothing less?"
- **Code quality reviewer:** "is the implementation well-built, with no race conditions, leaks, or NPE risks?"

The two-reviewer pattern caught five real bugs across Stages 1 and 2:

| Bug | Found by | Fix commit |
|---|---|---|
| `pointercancel` listener leak (`{once:true}` only removes the firing listener) | Code quality (Task 5) | `281aafd` |
| Multi-touch not ignored (Pointer Events default doesn't suppress 2nd finger) | Code quality (Task 5) | `281aafd` |
| `pointercancel` committing accidental drops | Code quality (Task 5) | `281aafd` |
| Snap-back clone visually pops at end (no opacity fade) | Code quality (Task 6) | `8093145` |
| Latent NPE on `originChip.getBoundingClientRect()` | Code quality (Task 6) | `8093145` |

Each finding triggered a re-dispatch of the implementer with explicit fix instructions, then a re-review. Loop until approved. Cost: ~3 subagent calls per task on top of the one implementation. Worth it.

### Mockup outcome

The Stage 1 mockup at `mockups/18-drag-drop-router.html` (renamed mid-flight from slot 11 when the user populated 11–17 in parallel) was 665 lines, self-contained HTML+CSS+JS. The user gut-checked it in browser. Verdict: promote to Stage 2.

---

## Stage 2: Svelte port to TS-1070

### One brainstorm question, then plan

By the time Stage 2 started, most of the design was settled — the mockup was the spec for the UX. The only Stage-2-specific fork was:

> **Replace the current Home routing UX outright, or add a toggle so you can A/B feel between the classic 12-button grid and the drag-drop view?**

User picked "replace" (#1). No toggle, no fallback. Drag-drop becomes the only routing UX.

The Stage 2 spec at `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md` was 237 lines. No separate plan doc; implementation was driven directly from the spec.

### Implementation order

Seven file changes, executed sequentially via subagents:

1. `src/lib/stores/router.ts` (NEW, 305 lines) — state machine + actions
2. `src/global.css` (modified) — three keyframes added globally
3. `src/components/SourceRail.svelte` (NEW, 132 lines) — left rail with 4 chips
4. `src/components/DragCloneOverlay.svelte` (NEW, 79 lines) — chip clone overlay
5. `src/components/DisplayTile.svelte` (rewritten, 263 lines) — drop zone with landed chip
6. `src/pages/Home.svelte` (modified) — grid layout grew rail column
7. `src/App.svelte` (modified) — mount DragCloneOverlay at root

Plus an a11y fix (`onkeydown` handler on the tile) and the same five-bug fix loop from Stage 1.

### Source-of-truth shift

The mockup tracked routing in a local `appState.routing` object. The Svelte port was tempted to mirror that, but the project already has authoritative state — the `display{N}SourceFb` feedback signal stores. Whatever the processor echoes back IS the routing.

The router store therefore tracks ONLY UI state:
- `armedSource` (which chip is "armed" for tap-to-route)
- `draggingSource` (which chip is currently being dragged)
- `cloneCoords` (pointer position for the drag clone)
- Various module-level imperative bookkeeping (timer IDs, press origin, etc.)

On drop, `routeSource()` calls:
```ts
publishAnalog(SIGNALS[`display${N}Source`], SOURCES[sourceId].value);
```
…and the feedback round-trip updates the landed chip via Svelte's reactive subscription to `display{N}SourceFb`. No local routing state. Correct even if the processor rejects the route, or another panel changes it, or anything else interferes.

### Build + deploy

The first deploy attempt failed — TS-1070 was at `192.168.2.53` per memory, but firmware update had moved it to `192.168.1.175` and the network couldn't route to `.2.53` from this machine. Quick diagnostics:

```bash
ping -n 3 192.168.2.53          # Destination host unreachable
ping -n 1 192.168.2.63          # Reply from 192.168.2.63 — adjacent IPs work
arp -a | grep "192.168.2"       # Two Crestron-OUI devices, but not .53
```

User confirmed the new IP. Override:
```bash
PANEL_HOST=192.168.1.175 python scripts/deploy.py
```
Worked on the first try. `[deploy] OK in 9.7s — panel will restart UI`.

**Lesson:** The `npm run deploy:tabletop` script in `package.json` hardcodes `192.168.2.53`. It needs updating or a `PANEL_HOST` env var override. Memory at `~/.claude/projects/.../memory/reference_mcccd_aa140_panel.md` was already updated by a prior session to `.175`; the script wasn't.

### First-touch hardware testing — three glitches

User loaded the deployed panel and reported:
> the motion while dragging is jumpy and glitchy, the tile when moved doesnt always stay stuck and resets before being placed it doesnt always start the process after holding the chip in the alloted time

Three distinct problems. Diagnosis:

1. **Jumpy motion** — drag clone was 80×88 raw viewport pixels, but the panel renders at `1.5×` scale on TS-1070 so each rail chip on screen is `120×132` viewport pixels. Mismatch made the clone look small and float "off-grid" relative to the rail.
2. **Drag resets before placement** — Crestron's touch driver re-targets pointer events to other elements during gestures. Without `setPointerCapture`, mid-drag the system would behave as if pointerup fired even with the finger still down.
3. **Long-press doesn't always fire** — `MOVE_CANCEL_THRESHOLD = 10px` was tight. Capacitive jitter on the TS-1070 wobbles ±15px even with a stationary finger.

Fixes (commit `6f333a7`):
1. `transform: scale(1.08 * panelScale)` on the clone — visually matches rail chips
2. `setPointerCapture(e.pointerId)` on chip pointerdown — pins events to the chip; explicit release on every cleanup path via a `releasePointerCaptureSafely()` helper
3. `MOVE_CANCEL_THRESHOLD = 30` — tolerates capacitive jitter

User feedback after redeploy:
> OK its better but there will still be work.

The drag-drop UX was now functional on hardware but not yet polished. User asked for FRED logging and theme alignment as the next track — see the lessons doc for the touch-panel hardening recipes.

### Theme alignment to MCCCD orange

User picked Mockup 11 ("MCCCD Orange Theme ★ Campus Standard") as the canonical look. The amber `#f5a623` palette on navy `#0d1b2e` is sourced from the existing campus panel in another room.

The `.theme-orange` class was already pre-baked in `global.css` waiting for activation. Two gaps:
1. The override block didn't include `--color-accent-dim` or `--color-panel-soft` — both used by Stage 2 components.
2. Four files had hardcoded `rgba(56, 189, 248, ...)` cyan literals that wouldn't theme:
   - `global.css` (chip-arm-pulse keyframe)
   - `DragCloneOverlay.svelte` (clone glow)
   - `DisplayTile.svelte` (drop-valid slot tint)
   - `PresetButton.svelte` (pressing state)

Fix (commit `4ec9f41`):
- Added `--color-accent-dim` and `--color-accent-glow` tokens to `:root`
- Extended `.theme-orange` block with all the missing overrides
- Replaced the four hardcoded values with token references
- Added `appEl.classList.add('theme-orange')` in `main.ts` before mount

The whole UI swapped to amber on the next deploy.

---

## Pivot: align to Mockup 10, park drag-drop

After the theme alignment, user said:

> I want the panel to look like the one in the mockups #10 full synthesis - lets level set to get the project aligned with the mockups before we proceed with gui edits

Mockup 10 is "Full Synthesis": a 72px left rail with logo + Home/Cams/Setup nav buttons + Power at the bottom, a thin status bar with room name + occupancy timer + online pill, three display tiles with the original 4-button per-display source grid, and a 3-zone footer with mic state tags.

There's a structural conflict: Mockup 10's left rail is for **navigation**. Our Stage 2 left rail is the **source palette**. They can't coexist as-is.

User's directive: "I want the drag and drop section to remain as a subpage that we will redesign to align with the new design. For now it is removed from view."

Resolution:
- The drag-drop UX components (`SourceRail`, `DragCloneOverlay`, `DropZone-tile-variant of DisplayTile`) are kept in the codebase
- They're moved into a new `DragDropRouter.svelte` page
- The page is added to the `currentPage` store as a value `'dragdrop'`
- `App.svelte` routes to it but **no navigation entry is wired** — the page is reachable only by setting `currentPage = 'dragdrop'` programmatically (dev/debug or future settings toggle)
- Home / DisplayTile / App.svelte are reverted to the pre-Stage-2 state (per-display 4-button source grid, no drag overlay mounted)

This is a clean preservation: drag-drop work isn't lost, just hidden. When the new Mockup 10–style chrome is in place, a future session can re-introduce drag-drop with a redesigned interaction model.

---

## Final commit list (cumulative)

Branch: `feat/drag-drop-router-mockup`

```
4ec9f41 feat(theme): activate MCCCD orange theme (Mockup 11 — Campus Standard)
729876c docs(handoff): drag-drop Stage 1+2 session log + open polish items
6f333a7 fix(stage-2): tune drag for touch panel feel
faed4c5 fix(stage-2): add keyboard handler to tile for a11y
09c42de feat(stage-2): mount DragCloneOverlay at App root
84335af feat(stage-2): wire SourceRail into Home grid layout
8a5fbe5 feat(stage-2): convert DisplayTile to drop zone with landed chip
b489053 feat(stage-2): add DragCloneOverlay component (chip clone overlay)
d294db9 feat(stage-2): add SourceRail component (left-rail with 4 chips)
8206739 feat(stage-2): add drag-drop keyframes (arm-pulse, thunk, tile-flash)
df6da79 feat(stage-2): add router store for drag-drop UI state
e484e0c docs(spec): drag-drop Stage 2 — Svelte port to TS-1070
c04a812 fix(mockup-18): update title tag + gallery subtitle to slot 18
6a17cfb fix(mockup): rename drag-drop router to slot 18 (slot 11 taken)
7fdfa6c feat(mockup-11): add gallery card linking to drag-drop router
8093145 fix(mockup-11): snap-back fade + null-safe originChip
3531578 feat(mockup-11): drop animation + snap-back curves
281aafd fix(mockup-11): listener leak, multi-touch, pointercancel
a215886 feat(mockup-11): long-press to drag with hover/no-op states
95fb07e feat(mockup-11): disarm on tap outside chip/tile
d4c27f1 feat(mockup-11): tap-to-arm and tap-to-route flow
ff709f1 feat(mockup-11): static layout (header, rail chips, display tiles, footer)
f4f1df7 feat(mockup-11): scaffold drag-drop router skeleton
e484e0c docs(spec): drag-drop Stage 2 — Svelte port to TS-1070
b033393 docs(spec): drag-and-drop source routing mockup (Stage 1)
```

22 commits across one driving session. Two-stage workflow: 12 commits for the static mockup (Stage 1), 9 for the Svelte port (Stage 2), 1 theme commit.

---

## What we'd do differently

A few things in hindsight:

1. **Run `npm run check` BEFORE every commit, not just before deploy.** A type error from one task lingered until I re-checked at the deploy gate. Adding `svelte-check` to the implementer's verification step would have caught it at that task's commit.

2. **Hardware-test the Stage 1 mockup, not just the Stage 2 build.** The mockup's `touch-action: none` on chips, gesture timings, and clone size could have been touch-tested by deploying the mockup HTML alone (it's served as a static asset by the `mockups/index.html` gallery). Would have caught the clone-size issue before Stage 2.

3. **Track stale memory entries.** The `reference_mcccd_aa140_panel.md` memory had been updated to the new IP `.175` already, but `package.json` `deploy:tabletop` script still hardcoded `.53`. Memory was correct; the codified default wasn't. Sync these on every IP change.

4. **Decide nav-vs-source-rail conflict before implementing the rail.** The Stage 1 mockup put the source palette in the left rail without considering that Mockup 10 uses the left rail for navigation. By Stage 2 the conflict was baked in. If we'd looked at all candidate layouts together up front, we'd have either:
   - Put source chips in a top strip (and the conflict goes away)
   - Or planned a "rail with source chips on top, nav at bottom" layout from day one

This is the "level-set with the mockups before GUI edits" lesson the user pulled forward.

---

## What's worth teaching FRED

The patterns in [`Drag-Drop-Source-Routing-Lessons.md`](./Drag-Drop-Source-Routing-Lessons.md) are the teachable substrate. The narrative in this doc is the context. Both should be in FRED's knowledge base alongside the spec/plan/handoff for any future agent that needs to:
- Build a drag/swipe/long-press UX on a CH5 panel
- Port a static UI prototype to Svelte + CrComLib
- Tune touch-panel pointer events for capacitive jitter
- Handle panel-scale-aware overlay positioning
- Run subagent-driven development with two-stage review

---

## File index for replication

Specs and plans (governance):
- `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md`
- `MCCCD-AA140/docs/superpowers/plans/2026-05-01-drag-drop-source-routing-plan.md`
- `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md`

Process artifacts:
- `MCCCD-AA140/docs/Handoffs/2026-05-02-drag-drop-stage-1-2-handoff.md`
- `MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Lessons.md` ← principles
- `MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Writeup.md` ← this file (narrative)

Stage 1 artifact:
- `mockups/18-drag-drop-router.html`

Stage 2 source (post-park, lives behind the `dragdrop` page route):
- `MCCCD-AA140/src/lib/stores/router.ts`
- `MCCCD-AA140/src/components/SourceRail.svelte`
- `MCCCD-AA140/src/components/DragCloneOverlay.svelte`
- `MCCCD-AA140/src/pages/DragDropRouter.svelte` (the new sub-page wrapper)
- `MCCCD-AA140/src/components/DisplayTile.svelte` (drop-zone variant — see git history pre-revert for the live form)
- 3 keyframes in `MCCCD-AA140/src/global.css`

Tooling:
- `MCCCD-AA140/scripts/deploy.py`
- `MCCCD-AA140/build.mjs`
- `MCCCD-AA140/package.json` (note the stale `deploy:tabletop` IP)
