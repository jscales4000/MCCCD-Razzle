# Lessons Learned — Drag-and-Drop Source Routing on a Crestron CH5 Panel

**Project:** MCCCD-AA140
**Branch where this work lives:** `feat/drag-drop-router-mockup`
**Period:** 2026-05-01 → 2026-05-02
**Stage 1 (HTML mockup):** validated in browser
**Stage 2 (Svelte + CrComLib port to TS-1070):** deployed to hardware, working but parked as a sub-page pending UX redesign to align with Mockup 10
**Audience:** future Claude/Copilot sessions, the FRED knowledge base, developers picking this up cold

---

## Why this document exists

We built a drag-and-drop source-routing UX for the AA140 touchpanel in two stages and learned a lot of non-obvious things about Crestron CH5, Svelte 5, and capacitive touch panels. The work has been parked for now (we're realigning the entire panel chrome to Mockup 10 first), but the patterns and gotchas are reusable. This doc captures **what to repeat** and **what to avoid** the next time someone — human or agent — does pointer-driven UX on a CH5 panel.

This is the principle/pattern doc. The narrative retrospective lives in the companion file [`Drag-Drop-Source-Routing-Writeup.md`](./Drag-Drop-Source-Routing-Writeup.md).

---

## Process lessons

### L1 — Two-stage prototype (mockup before Svelte) is worth the day

We split the work into:
1. **Stage 1** — static HTML/CSS/JS mockup at `mockups/18-drag-drop-router.html`. No framework, no signals, no build step. Self-contained file in the gallery.
2. **Stage 2** — Svelte port wired to real `display{N}Source` analogs, deployed via `.ch5z` to TS-1070.

Stage 1 took roughly half the time but answered the load-bearing questions:
- Does the metaphor (chip-onto-tile) feel right at all?
- What's the right gesture (long-press vs immediate drag vs tap-to-arm)?
- What animation timings feel "satisfying" vs "slow"?

If we had skipped to Stage 2, we'd have wasted hours on visual tuning inside Svelte before the metaphor itself was validated. The mockup also became the **source of truth** for component CSS — we lifted hardcoded values verbatim into Svelte.

**Recipe to repeat:** when an idea has UX risk *and* implementation risk, prototype the UX first in the cheapest medium possible (static HTML works for almost any CH5 panel concept since the panel browser renders standard HTML). Promote to the framework only after the gut-check.

### L2 — Subagent-driven development with two-stage review caught real bugs

Stage 2 used the [`subagent-driven-development` superpower](https://docs.anthropic.com/skills/superpowers): one subagent per task, then **two reviewers** per task in sequence — first a spec-compliance reviewer (does the code match what was asked?), then a code-quality reviewer (is the implementation well-built?). The reviewers found and forced fixes for **five real bugs** that the implementer's self-review missed:

- `pointercancel` listener leak (each new drag added a stale orphan)
- Multi-touch wasn't actually ignored (pointer events don't ignore multi-touch by default — that was a wrong assumption in the plan)
- `pointercancel` was committing accidental drops instead of cancelling
- Snap-back clone visually "popped" at the end (no opacity fade)
- Latent NPE on `originChip.getBoundingClientRect()` if chip was ever null

Without the review pass, all five would have shipped to hardware and presented as flaky drag behavior — exactly the kind of issue that's hardest to root-cause from "it feels glitchy."

**Recipe to repeat:** for any task with ≥2 lines of pointer/timer/animation code, run the spec-then-quality review pair. The cost is ~3 subagent calls per task; the benefit is finding bugs cheaper than they'd be to debug from user reports.

### L3 — Code review by a fresh subagent > self-review by the implementer

Same point as L2 but worth flagging separately. The implementer's self-review correctly identified zero of the five bugs above. A *different* subagent reviewing the diff with no prior context found them all. Fresh eyes work; tired eyes don't.

### L4 — Hardware-first reveals bugs that browser dev never will

Three of the worst issues only surfaced on the actual TS-1070:
1. The drag clone was the wrong size on the panel (looked fine in the browser at 1.0× scale).
2. The drag would "reset before placement" — Crestron's touch driver re-targets pointer events to other elements mid-gesture in a way Chrome on a desktop doesn't.
3. A 10px move-cancel threshold was fine in browser DevTools touch emulation; on the real capacitive screen, a stationary finger jitters ±15px.

**Recipe to repeat:** budget at least one round trip of "build → deploy → tune" in the original schedule. Don't claim "done" until it's been touched by a human finger on the actual panel, not a mouse in DevTools.

### L5 — Capture stage outcomes inside the spec, not separately

We left a `## Stage 1 Outcome` heading at the bottom of the spec for the gut-check verdict. This kept the spec evergreen — a future reader sees both the original design AND what actually happened, in one document. Better than scattering outcomes across handoff docs that get forgotten.

**Recipe to repeat:** at the bottom of every spec, leave a placeholder section (`## Outcome` or `## Status`) for post-implementation truth. Fill it in when the work ships or gets parked.

---

## Architecture lessons

### L6 — Source-of-truth shift: feedback stores are authoritative, UI state is separate

The Stage 1 mockup tracked routing in `appState.routing = { d1: 'roomPc', d2: 'airMedia', d3: null }`. When porting to Svelte, the temptation was to recreate that state in a router store. **Wrong.** The system already has authoritative state — the `display{N}SourceFb` feedback signal stores. Whatever the processor echoes back IS the routing, regardless of what the UI thinks happened.

Stage 2's router store therefore tracks **only UI state** (`armedSource`, `draggingSource`, `cloneCoords`). On drop, we publish the analog and let the feedback round-trip update the landed chip. This means the UI is correct even when:
- The processor rejects the route command for any reason
- A different controller (e.g., wall panel, scheduled scene) changes routing while the user is dragging
- The user's finger leaves the panel mid-route and the system reverts

**Recipe to repeat:** in any CH5 panel, never store a duplicate of what a feedback signal already holds. The local copy will drift. Subscribe to the feedback store and render directly from it.

### L7 — Two writable stores + module-level `let` for imperative state

The router store at `MCCCD-AA140/src/lib/stores/router.ts` mixes two patterns deliberately:

```ts
// Reactive UI state — components subscribe via $store
export const armedSource = writable<SourceId | null>(null);
export const draggingSource = writable<SourceId | null>(null);
export const cloneCoords = writable<{x: number; y: number}>({ x: 0, y: 0 });

// Imperative state — module-private, never subscribed
let suppressNextClick = false;
let pressTimerId: ReturnType<typeof setTimeout> | null = null;
let pressOriginEl: HTMLElement | null = null;
let pressOriginX = 0, pressOriginY = 0;
let lastPointerX = 0, lastPointerY = 0;
```

Reactive state goes through `writable()`. Imperative state — timer IDs, pointer-origin coordinates, the chip element being captured — stays as plain module-level `let`. Mixing them in a single `$state()` runes object would make every pointer-move event re-trigger reactive consumers needlessly.

**Recipe to repeat:** for drag/animation state machines, separate "UI bindings" (writable stores) from "bookkeeping" (module-level vars). Subscribers only re-render when something they care about changes.

### L8 — Match existing conventions even if a newer pattern is "better"

The MCCCD-AA140 codebase predates Svelte 5 runes and uses Svelte 4 `writable()` stores everywhere (`signals.ts`, `page.ts`). I used the same pattern in `router.ts` even though `$state()` runes would have been slightly tidier. Reasoning: introducing a new state idiom *incidentally* (because of one feature) makes the codebase harder to reason about. Adopt new patterns *deliberately*, not as side effects.

**Recipe to repeat:** in an existing codebase, new code matches existing patterns unless the task is explicitly "modernize this." Refactor decisions should be one PR, not implicit in feature work.

---

## Touch panel pointer-event hardening

This is the meat of what's worth teaching FRED for future Crestron work. **Every** drag/long-press/swipe UX on a CH5 panel needs these patterns or it will be flaky.

### L9 — `setPointerCapture` is mandatory, not optional

Crestron's touch driver can re-target pointer events to other elements during a gesture (we don't know exactly when or why — different from desktop Chromium). Without `setPointerCapture`, a drag would sometimes "reset before placement" — the user is mid-drag and suddenly the chip snaps back as if pointerup fired, even though the finger is still down.

The fix:

```ts
chipEl.setPointerCapture(e.pointerId);
activePointerId = e.pointerId;
pressCapturedEl = chipEl;
```

…on `pointerdown`, with explicit release on **every** cleanup path:

```ts
function releasePointerCaptureSafely(): void {
  if (pressCapturedEl && activePointerId !== null) {
    try { pressCapturedEl.releasePointerCapture(activePointerId); } catch { /* already released */ }
  }
  pressCapturedEl = null;
  activePointerId = null;
}
```

The `try/catch` is necessary because the browser may auto-release on certain transitions (element detached, etc.) and `releasePointerCapture` throws on an already-released pointer.

**Recipe to repeat:** any touch-driven UX on a CH5 panel must capture the pointerId on pointerdown and release it on every terminal path (pointerup, pointercancel, move-cancel, drop, snap-back). Treat capture as part of the state machine.

### L10 — `MOVE_CANCEL_THRESHOLD` for capacitive jitter is 25–30px, not 10

The "did the user move significantly during the long-press?" check has to tolerate finger jitter on capacitive screens. Resting a still finger on the TS-1070 reports `clientX/Y` deltas of ±10–15px naturally — the screen's noise floor. A 10px threshold cancels the long-press almost every time. 30px is the empirical sweet spot for the TS-1070; behaves well from finger-pads on glass.

**Recipe to repeat:** on capacitive touch panels, 30px is the threshold for "user actually moved" during a hold. Don't go below 20. Be ready to bump to 50 if a panel reports more aggressive noise.

### L11 — `pointermove` and `pointerup` listeners must be explicitly removed

The original plan used `{ once: true }` on `pointerup` and `pointercancel` registrations:

```ts
// WRONG
document.addEventListener('pointerup', onPointerUp, { once: true });
document.addEventListener('pointercancel', onPointerUp, { once: true });
```

When `pointerup` fired (the common path), only the `pointerup` listener self-removed. The `pointercancel` listener stayed registered forever. After 50 drags in a session, document had 50 stale `pointercancel` listeners. None broke functionality immediately but the panel runs for hours unattended — eventually it matters.

The fix:

```ts
function detachPointerListeners(): void {
  document.removeEventListener('pointermove', onPointerMove);
  document.removeEventListener('pointerup', onPointerUp);
  document.removeEventListener('pointercancel', onPointerCancel);
}

function onPointerUp(e: PointerEvent): void {
  detachPointerListeners();
  // ... handle up
}

function onPointerCancel(e: PointerEvent): void {
  detachPointerListeners();
  // ... handle cancel
}
```

Both terminal handlers detach all three listeners. Idempotent removeEventListener calls are safe — no need to track which ones actually fired.

**Recipe to repeat:** never use `{ once: true }` on paired terminal listeners (pointerup AND pointercancel). Use explicit `removeEventListener` from a shared helper. Same applies to mouseup/mouseout pairs in older browsers.

### L12 — `pointercancel` must NOT commit a drop — it must cancel

The original implementation aliased `pointercancel` to `pointerup`. On a system gesture, focus loss, or OS interruption mid-drag, the cancel would route to whatever tile happened to be under the cancel coordinates. The user never released the chip onto a tile; the system did it for them.

The fix: separate `onPointerCancel` handler that calls `endDrag(-1, -1)`. The `(-1, -1)` coords are guaranteed outside any viewport tile, so `tileUnderPointer` returns null and the drop branch is skipped — only the snap-back animation runs.

```ts
function onPointerCancel(_e: PointerEvent): void {
  detachPointerListeners();
  if (pressTimerId) { /* press not yet promoted to drag */ ... return; }
  if (get(draggingSource)) {
    endDrag(-1, -1);  // Force snap-back; no commit.
  }
}
```

**Recipe to repeat:** `pointercancel` is "the gesture is dead, the user did not commit anything." Treat it as a cancel, never as a complete. Same for any event named "cancel" in any pointer/touch model.

### L13 — Multi-touch is NOT ignored by default

Pointer Events spec promises one event stream per pointerId. If a second finger lands during a drag, you get a *new* set of `pointerdown`/`move`/`up` events for the second pointer — they don't get suppressed. Without a guard, the second finger's `pointerdown` overwrites your module-level `pressOriginEl` / `pressTimerId` / `dragClone` references and the first drag is leaked into orphan DOM.

The fix is one line at the top of the chip's `pointerdown` handler:

```ts
if (get(draggingSource) || pressTimerId) return;
```

If a drag is in flight or a press is pending, ignore the new pointer.

**Recipe to repeat:** if your spec says "multi-touch ignored," verify it with code, not with documentation — Pointer Events do not give that for free.

### L14 — Panel-scale aware coordinates for any element rendered outside `.panel-stage`

The CH5 panel renders at logical 1280×800 inside a `.panel-stage` element that's `transform: scale(var(--panel-scale))`. On TS-1070 (1920×1200), `--panel-scale: 1.5`. Pointer events fire with viewport coordinates (post-scale).

The drag clone in this project is rendered OUTSIDE `.panel-stage` (App-root level) so it doesn't double-scale. But that means its CSS sizes are raw viewport pixels — the clone is 80×88 viewport pixels while the rail chip on the panel is 80×88 *logical* (= 120×132 viewport on a 1.5× panel). Mismatch.

Two ways to handle:

```ts
// Option A: read --panel-scale and apply it in the JS-set transform
const ps = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--panel-scale')) || 1;
clone.style.transform = `translate(${x - 40}px, ${y - 44}px) scale(${1.08 * ps}) rotate(2deg)`;
```

```ts
// Option B: render INSIDE .panel-stage and convert pointer events to logical coords
const stageRect = panelStageEl.getBoundingClientRect();
const ps = stageRect.width / 1280;
const logicalX = (e.clientX - stageRect.left) / ps;
```

We chose Option A because the clone needs to span outside the panel's bounds during drag and elementFromPoint behaves more predictably with viewport coords.

**Recipe to repeat:** every fixed-positioned overlay on a Crestron panel needs to be panel-scale-aware. Either render inside `.panel-stage` (and convert coords once on input) or read `--panel-scale` in JS and apply it explicitly to sizes/transforms.

---

## Animation patterns

### L15 — Three-phase drop animation reads as "the signal landed"

The drop animation has a strict ~280ms total budget split into three phases:

1. **Snap (0–180ms)** — clone slides from the pointer position to the slot center via cubic-bezier ease-out.
2. **Thunk (~180–280ms)** — newly-landed chip in the slot does a `scale 1.0 → 1.06 → 1.0` bump (100ms).
3. **Flash (~180–330ms)** — tile's outer border briefly cyan-flashes (150ms fade).

Phases 2 and 3 fire in parallel after phase 1 completes. The total wall-clock is short enough to feel immediate, but each phase is distinct enough to read as "thing landed" rather than "button changed state."

The whole animation is set up imperatively in the `endDrag` function, not declaratively in CSS. Rationale: CSS-only animations can't easily express "after 180ms, swap content and trigger a different animation" — the JS sequence is clearer.

**Recipe to repeat:** for "landing" animations, three phases at ~50/35/50% of the time budget feels right. Use a `setTimeout` to hand off between phases; don't try to do it with CSS animation chains.

### L16 — `void el.offsetWidth` reflow trick to retrigger CSS animations

A class toggle alone doesn't restart a CSS animation in many browsers — they cache the previous animation state. To reliably re-trigger:

```ts
el.classList.remove('thunk');
void el.offsetWidth;  // Force synchronous reflow — flushes the class change.
el.classList.add('thunk');
```

Reading `offsetWidth` forces the browser to compute layout, which commits the class removal. Now adding the class again is seen as a fresh state and the animation restarts.

**Recipe to repeat:** when an animation needs to fire repeatedly on the same element, always do remove-reflow-add. The void-offsetWidth pattern is the standard idiom.

### L17 — Snap-back ends with an opacity fade to avoid the "pop"

Originally the snap-back animation just translated the clone back to the rail chip's position and `removeChild`'d it at t=220ms. The clone hard-disappeared at full opacity, causing a visual "pop" — two superimposed chips at different opacities, then an abrupt swap. The reviewer flagged this; the fix:

```ts
clone.classList.add('snapping');
clone.style.transform = `translate(${originRect.left}px, ${originRect.top}px) scale(...) rotate(0deg)`;
clone.style.opacity = '0.3';  // Match the ghost opacity so it merges into the rail chip.
```

The clone fades to 0.3 (the rail chip's ghost opacity) DURING the snap-back, so by the time it's removed at t=220ms, it visually merges with the rail chip beneath.

**Recipe to repeat:** for "return to origin" animations on an overlay, fade the overlay's opacity toward whatever's underneath it, so removal is invisible.

---

## Tooling and process

### L18 — `superpowers:brainstorming` before any creative UX decision

The brainstorming skill is rigid about asking one question at a time and proposing 2–3 approaches per fork before settling. It feels slow when you have a clear vision, but it surfaces decisions that would otherwise be made implicitly. We used it for both Stage 1 and Stage 2 and both times the user changed their first answer to a question after seeing the trade-offs. Worth the friction.

**Recipe to repeat:** never skip brainstorming on a UX-shaped task, even if the user says "just build it." The skill exists because UX decisions made implicitly become tech debt.

### L19 — `npm run check` before `npm run archive` catches type errors early

The build (`build.mjs` → vite + ch5-cli archive) does NOT run `svelte-check`. A broken type signature can produce a working `.ch5z` that crashes silently at runtime. Always run `npm run check` before `npm run deploy:tabletop`.

**Recipe to repeat:** if a deploy script doesn't gate on type check, add it manually as the first step. Type errors should never reach the panel.

### L20 — Hardcoded color literals are theme bombs

Stage 2 ended up with four hardcoded `rgba(56, 189, 248, ...)` values that didn't theme when we activated the orange MCCCD palette. They were "fine" while the project was cyan but became visible as bugs the moment a theme switch happened. CSS custom properties are the only safe color reference in any project that might ever theme.

```css
/* WRONG — won't theme */
background-color: rgba(56, 189, 248, 0.10);

/* RIGHT — themes via cascade */
background-color: var(--color-accent-dim);
```

**Recipe to repeat:** every color in component CSS goes through a CSS custom property. If a token doesn't exist for the shade you need, add it to `:root` and the theme override block at the same time. Never put rgba/hex in a component file.

---

## File structure for replication

When teaching a future agent to recreate this approach in another panel project, the minimum file set is:

```
src/
  lib/
    stores/
      router.ts                    Drag/arm state machine + pointer handlers
      signals.ts                   Existing CrComLib feedback subscriptions
    contract.ts                    Existing SIGNALS namespace
    CrComLib.ts                    publishAnalog/subscribeAnalog wrappers
  components/
    SourceRail.svelte              Vertical rail with chip palette
    DragCloneOverlay.svelte        App-root chip clone overlay
    DisplayTile.svelte             Drop-zone tile with landed chip slot
  pages/
    Home.svelte                    Layout: rail + tiles + chrome
  App.svelte                       Mounts overlay + page router
  global.css                       Tokens + .theme-orange + 3 keyframes
  main.ts                          Apply theme class + mount
```

Plus deploy plumbing (`scripts/deploy.py`, `package.json` archive script).

---

## Anti-patterns to NOT repeat

- ❌ Skipping the static-HTML mockup and going straight to Svelte for an unproven UX.
- ❌ Self-review by the implementer as the only quality gate.
- ❌ `{ once: true }` on paired terminal listeners.
- ❌ `pointercancel` aliased to `pointerup`.
- ❌ Hardcoded color literals in component CSS.
- ❌ Storing routing state locally when feedback signals already hold it.
- ❌ Assuming `Pointer Events ignore multi-touch`. They don't.
- ❌ Trusting browser DevTools touch emulation as a substitute for hardware testing.
- ❌ Using `$state()` runes when the rest of the codebase uses `writable()` stores — adopt new patterns deliberately.

---

## Cross-references

- **Narrative retrospective:** [`Drag-Drop-Source-Routing-Writeup.md`](./Drag-Drop-Source-Routing-Writeup.md)
- **Stage 1 spec:** `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md`
- **Stage 1 plan:** `MCCCD-AA140/docs/superpowers/plans/2026-05-01-drag-drop-source-routing-plan.md`
- **Stage 2 spec:** `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md`
- **Session handoff:** `MCCCD-AA140/docs/Handoffs/2026-05-02-drag-drop-stage-1-2-handoff.md`
- **Stage 1 mockup:** `mockups/18-drag-drop-router.html`
- **Stage 2 source files:** `MCCCD-AA140/src/lib/stores/router.ts`, `src/components/SourceRail.svelte`, `src/components/DragCloneOverlay.svelte`, plus modifications to `DisplayTile.svelte` and `Home.svelte`.
