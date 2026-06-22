# Home Source-Select Workflow Toggle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a live A/B toggle to the Home page that switches source selection between the current destination-first flow and a new source-first "paint" flow (arm a source, tap displays to route immediately, Send-to-All, persists until another source).

**Architecture:** Approach 1 — a session-scoped `homeRouteMode` store drives a thin branch inside `Home.svelte`; all routing logic stays in `router.ts`, reusing the existing `armedSource` store and `routeSource()` primitive. Both workflows share the same source-card and display-chip markup, so the toggle is a true A/B on identical pixels and Workflow A (destination-first) is left functionally untouched.

**Tech Stack:** Svelte 5 (runes: `$state`/`$derived`/`$props`), TypeScript, Vite, Crestron CH5 (`@crestron/ch5-crcomlib` via `lib/CrComLib.ts`). No unit-test runner exists in this project.

## Global Constraints

- **Pure panel-side.** No contract signal, no `.cce`, no Contract Editor build, no processor build. Only existing `Display{N}Source` set signals + `Display{N}SourceFb` feedback are used.
- **Feedback-driven only.** All live chip/card state derives from `Display{N}SourceFb` / sync feedbacks — no optimistic mirrors.
- **Workflow A untouched.** Destination-first keeps the same code path, timers, and `resetTargetDisplays()` behavior. Default mode is `'destination'`.
- **Persona compliance:** touch targets ≥44px; theme custom-properties only (no hardcoded color/size — reuse existing `--color-*` and the `#f5a623`/`#0d1b2e` accent already used in `Home.svelte`); state never on color alone (pair with shape/label); animations ≤300ms; guard all motion with `@media (prefers-reduced-motion: reduce)`; `:active` press state on every control (capacitive panels have no hover).
- **Verification model (this repo has no test runner):** every task ends with `npm run check` (svelte-check must be type-clean except the one known pre-existing `MicVolumeModal.svelte:64` error and `ConfirmShutdownModal.svelte:29` warning) + a concrete dev-browser check at `http://localhost:5174/` + a commit. Live routing is verified on glass in the final task (browser dev has no processor, so chips won't repaint on route — armed/toggle visuals still verify in dev).
- Branch: `feat/home-source-select-toggle` (already created; spec committed at `549fc32`).
- Run all `npm` commands from `MCCCD-AA140/`.

---

## File Structure

- `src/lib/stores/session.ts` — **Modify.** Add the `homeRouteMode` session store (+ `HomeRouteMode` type). Session-scoped so the chosen test mode survives `goToPage()`.
- `src/lib/stores/router.ts` — **Modify.** Add `armForPaint(sourceId)` (arm with no auto-disarm) and `routeArmedToAll()` (paint armed source to all four). Reuse existing `armedSource`, `routeSource`, `ALL_DISPLAYS`, `disarm`.
- `src/pages/Home.svelte` — **Modify.** Mode toggle control; branch source-tap and chip-tap handlers by mode; armed source-card visual; source-mode caption; chip "has-it" feedback treatment; Send-to-All button; animations; `onMount` disarm. All CSS additions in the existing `<style>` block.

---

### Task 1: State layer — mode store + router helpers

**Files:**
- Modify: `src/lib/stores/session.ts`
- Modify: `src/lib/stores/router.ts`

**Interfaces:**
- Produces: `homeRouteMode: Writable<'destination' | 'source'>` and `type HomeRouteMode` (from `session.ts`); `armForPaint(sourceId: SourceId): void` and `routeArmedToAll(): void` (from `router.ts`).
- Consumes: existing `armedSource: Writable<SourceId | null>`, `routeSource(sourceId, displayId)`, `ALL_DISPLAYS`, `armedTimeoutId`, `get` (all already in `router.ts`).

- [ ] **Step 1: Add the mode store to `session.ts`**

Append to `src/lib/stores/session.ts` (after the `userPoweredOn` export):

```ts
// homeRouteMode: which Home source-selection workflow is active.
//   'destination' = pick displays, then tap a source to route + reset (the
//                   historical default flow — unchanged).
//   'source'      = arm a source, then paint displays (route on each chip tap)
//                   with a Send-to-All shortcut; persists until another source
//                   is armed.
// Session-scoped (not a CH5 feedback mirror) so the chosen test mode survives
// goToPage() round-trips. Resets to 'destination' on reload.
export type HomeRouteMode = 'destination' | 'source';
export const homeRouteMode = writable<HomeRouteMode>('destination');
```

- [ ] **Step 2: Add `armForPaint` + `routeArmedToAll` to `router.ts`**

In `src/lib/stores/router.ts`, immediately after the `routeSourceToTargets` function (ends at the line `  resetTargetDisplays();\n}`), add:

```ts
/** Arm a source for source-first "paint" mode (Home). Unlike armChip(), there
 *  is NO 4s auto-disarm — the armed source persists until a different source is
 *  armed, so Home's source mode keeps page state between actions. Does NOT set
 *  the body 'any-armed' class (that drives Advanced-Routing tile dimming, which
 *  Home has no use for). */
export function armForPaint(sourceId: SourceId): void {
  if (armedTimeoutId) {
    clearTimeout(armedTimeoutId);
    armedTimeoutId = null;
  }
  armedSource.set(sourceId);
}

/** Route the currently-armed source to all four displays at once. No-op when
 *  nothing is armed. Backs Home source-mode "Send to All". routeSource() already
 *  no-ops a display that shows the source, so this is safe to spam. */
export function routeArmedToAll(): void {
  const armed = get(armedSource);
  if (!armed) return;
  ALL_DISPLAYS.forEach((d) => routeSource(armed, d));
}
```

- [ ] **Step 3: Type-check**

Run: `npm run check`
Expected: completes with only the known pre-existing `MicVolumeModal.svelte:64` ERROR and `ConfirmShutdownModal.svelte:29` WARNING — **no new errors** referencing `session.ts` or `router.ts`.

- [ ] **Step 4: Commit**

```bash
git add src/lib/stores/session.ts src/lib/stores/router.ts
git commit -m "feat(home): mode store + armForPaint/routeArmedToAll router helpers"
```

---

### Task 2: Mode toggle control (default destination, no behavior change yet)

**Files:**
- Modify: `src/pages/Home.svelte`

**Interfaces:**
- Consumes: `homeRouteMode` (Task 1).
- Produces: the visible toggle; `$homeRouteMode` reactive value consumed by Tasks 3–6. No routing behavior change yet (handlers still call the originals).

- [ ] **Step 1: Import the mode store**

In `src/pages/Home.svelte`, extend the `stores/session` import. Replace:

```ts
  import { userPoweredOn } from '../lib/stores/session';
```

with:

```ts
  import { userPoweredOn, homeRouteMode } from '../lib/stores/session';
```

- [ ] **Step 2: Replace the eyebrow with the toggle**

In the markup, replace this line:

```svelte
      <div class="eyebrow">— Choose your source —</div>
```

with:

```svelte
      <div class="mode-toggle" role="group" aria-label="Source selection workflow">
        <button
          class="mode-seg"
          class:on={$homeRouteMode === 'destination'}
          aria-pressed={$homeRouteMode === 'destination'}
          onclick={() => homeRouteMode.set('destination')}
          type="button"
        >Display <span class="seg-arrow" aria-hidden="true">→</span> Source</button>
        <button
          class="mode-seg"
          class:on={$homeRouteMode === 'source'}
          aria-pressed={$homeRouteMode === 'source'}
          onclick={() => homeRouteMode.set('source')}
          type="button"
        >Source <span class="seg-arrow" aria-hidden="true">→</span> Display</button>
      </div>
```

- [ ] **Step 3: Add toggle styles**

In the `<style>` block, after the `.eyebrow { ... }` rule, add:

```css
  /* ── Mode toggle — Workflow A/B switch above the source row ── */
  .mode-toggle {
    display: inline-flex;
    gap: 4px;
    padding: 4px;
    border-radius: 12px;
    background-color: rgba(8, 16, 30, 0.6);
    border: 0.5px solid rgba(148, 163, 184, 0.18);
  }
  .mode-seg {
    appearance: none;
    -webkit-appearance: none;
    min-height: 44px;
    padding: 0 18px;
    border: none;
    border-radius: 9px;
    background: transparent;
    color: var(--color-copy-soft, #94a3b8);
    font-family: inherit;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: pointer;
    transition: color 160ms ease, background-color 160ms ease, box-shadow 160ms ease;
  }
  .mode-seg .seg-arrow { opacity: 0.6; padding: 0 2px; }
  .mode-seg:active { transform: scale(0.97); transition-duration: 90ms; }
  .mode-seg.on {
    color: #1a1208;
    font-weight: 800;
    background-color: #f5a623;
    background-image: linear-gradient(180deg, #f9b94a, #ec9415);
    box-shadow: 0 2px 10px rgba(245, 166, 35, 0.3);
  }
  .mode-seg.on .seg-arrow { opacity: 0.85; }
```

- [ ] **Step 4: Type-check**

Run: `npm run check`
Expected: no new errors referencing `Home.svelte`.

- [ ] **Step 5: Verify in dev browser**

The dev server is already running at `http://localhost:5174/` (restart with `npm run dev` if needed). With the panel powered on (tap the splash), confirm: the two-segment toggle renders above the source cards; "Display → Source" is active by default (amber fill); tapping "Source → Display" moves the amber fill; tapping a source still routes-to-all as before (Workflow A unchanged); no console errors.

- [ ] **Step 6: Commit**

```bash
git add src/pages/Home.svelte
git commit -m "feat(home): add Display-first/Source-first workflow toggle (UI only)"
```

---

### Task 3: Source-first arm + paint behavior + armed card visual

**Files:**
- Modify: `src/pages/Home.svelte`

**Interfaces:**
- Consumes: `homeRouteMode` (Task 1), `armForPaint` / `routeArmedToAll` / `armedSource` / `disarm` (Task 1 + existing `router.ts`), existing `routeSource`, `toggleTargetDisplay`, `routeSourceToTargets`.
- Produces: `onSourceTap(src)` and `onChipTap(d)` mode-branching handlers; `.hero-card.armed` visual; armed-on-mount cleanup. Consumed by Tasks 4–6.

- [ ] **Step 1: Import the source-mode router functions**

Replace the existing `stores/router` import block:

```ts
  import {
    SOURCES as ROUTER_SOURCES,
    routeSourceToTargets, sourceFromValue,
    targetDisplays, toggleTargetDisplay, allTargeted, resetTargetDisplays,
    type DisplayId,
  } from '../lib/stores/router';
```

with:

```ts
  import {
    SOURCES as ROUTER_SOURCES,
    routeSourceToTargets, sourceFromValue,
    targetDisplays, toggleTargetDisplay, allTargeted, resetTargetDisplays,
    armedSource, armForPaint, routeArmedToAll, routeSource, disarm,
    type DisplayId, type SourceId,
  } from '../lib/stores/router';
```

- [ ] **Step 2: Extract the flash helper and add mode-branching handlers**

Replace the existing `selectSourceForTargets` function:

```ts
  let flashedSource = $state<number | null>(null);
  let flashTimerId: ReturnType<typeof setTimeout> | null = null;
  function selectSourceForTargets(value: 1 | 2 | 3 | 4) {
    routeSourceToTargets(value);
    flashedSource = null;
    if (flashTimerId) clearTimeout(flashTimerId);
    requestAnimationFrame(() => { flashedSource = value; });
    flashTimerId = setTimeout(() => { flashedSource = null; }, 300);
  }
```

with:

```ts
  let flashedSource = $state<number | null>(null);
  let flashTimerId: ReturnType<typeof setTimeout> | null = null;
  function flashCard(value: number) {
    flashedSource = null;
    if (flashTimerId) clearTimeout(flashTimerId);
    requestAnimationFrame(() => { flashedSource = value; });
    flashTimerId = setTimeout(() => { flashedSource = null; }, 300);
  }

  // Source-card tap. Destination mode: route to the current target set + reset
  // (Workflow A, unchanged). Source mode: arm the source for painting (no route).
  function onSourceTap(src: { value: 1 | 2 | 3 | 4; key: SourceId }) {
    if ($homeRouteMode === 'source') {
      armForPaint(src.key);
    } else {
      routeSourceToTargets(src.value);
    }
    flashCard(src.value);
  }

  // Display-chip tap. Destination mode: toggle target membership (Workflow A).
  // Source mode: immediately route the armed source to this display (paint).
  function onChipTap(d: { id: DisplayId }) {
    if ($homeRouteMode === 'source') {
      if ($armedSource) routeSource($armedSource, d.id);
    } else {
      toggleTargetDisplay(d.id);
    }
  }
```

- [ ] **Step 3: Point the source card and chip onclick at the new handlers**

In the source-card `<button>`, replace:

```svelte
            onclick={() => selectSourceForTargets(src.value)}
```

with:

```svelte
            onclick={() => onSourceTap(src)}
```

Add the armed class to the same `<button>` — replace:

```svelte
            class="hero-card"
            class:active={$display1SourceFb === src.value}
            class:route-flash={flashedSource === src.value}
```

with:

```svelte
            class="hero-card"
            class:active={$display1SourceFb === src.value}
            class:armed={$homeRouteMode === 'source' && $armedSource === src.key}
            class:route-flash={flashedSource === src.value}
```

In the display-chip `<button>`, replace:

```svelte
            onclick={() => toggleTargetDisplay(d.id)}
```

with:

```svelte
            onclick={() => onChipTap(d)}
```

- [ ] **Step 4: Disarm on Home mount (prevent armed state leaking across nav)**

In the `onMount` callback, replace:

```ts
    // Every arrival at Home starts from the route-everywhere default — a
    // narrowed target set left by a previous visit must not survive nav.
    resetTargetDisplays();
```

with:

```ts
    // Every arrival at Home starts from the route-everywhere default — a
    // narrowed target set left by a previous visit must not survive nav.
    resetTargetDisplays();
    // Likewise, never inherit a source armed on another page (or a prior Home
    // visit). Mode flips within a single visit keep the armed source; leaving
    // and returning starts clean.
    disarm();
```

- [ ] **Step 5: Add the armed-card style**

In the `<style>` block, after the `.hero-card.active .hc-ico, .hero-card.active .hc-name { color: #f5a623; }` rule, add:

```css
  /* Armed state (source-first mode): persistent amber treatment driven by
     armedSource, mirroring .active but independent of D1 feedback. */
  .hero-card.armed {
    border-color: rgba(245, 166, 35, 0.55);
    box-shadow:
      0 0 0 1px rgba(245, 166, 35, 0.4),
      0 0 28px rgba(245, 166, 35, 0.2);
  }
  .hero-card.armed::after {
    content: 'ARMED';
    position: absolute;
    bottom: 10px;
    left: 50%;
    transform: translateX(-50%);
    font-size: 9px;
    font-weight: 800;
    letter-spacing: 0.18em;
    color: #f5a623;
    pointer-events: none;
  }
  .hero-card.armed .hc-ico,
  .hero-card.armed .hc-name { color: #f5a623; }
```

- [ ] **Step 6: Type-check**

Run: `npm run check`
Expected: no new errors referencing `Home.svelte`.

- [ ] **Step 7: Verify in dev browser**

At `http://localhost:5174/`: switch to "Source → Display". Tap a source → it gets the persistent amber border + "ARMED" label and stays armed (no reset). Tap a different source → arm moves to it. Switch back to "Display → Source" → tapping a source routes-to-all as before. (Chip repaint on tap is only observable on glass — verified in Task 7.) No console errors.

- [ ] **Step 8: Commit**

```bash
git add src/pages/Home.svelte
git commit -m "feat(home): source-first arm + paint handlers and armed card visual"
```

---

### Task 4: Source-mode caption + display-chip "has-it" feedback

**Files:**
- Modify: `src/pages/Home.svelte`

**Interfaces:**
- Consumes: `homeRouteMode`, `armedSource`, `ROUTER_SOURCES`, existing `displayStates`, `targetsAreAll`, `targetCaption`.
- Produces: `armedValue`/`armedLabel` deriveds and the source-mode caption + chip treatment. Consumed by Task 6 (animation) only cosmetically.

- [ ] **Step 1: Add armed-value/label deriveds**

After the existing `targetCaption` derived (the `let targetCaption = $derived(...)` block), add:

```ts
  // Source-mode helpers: the armed source's analog value + display label.
  let armedValue = $derived($armedSource ? ROUTER_SOURCES[$armedSource].value : null);
  let armedLabel = $derived($armedSource ? ROUTER_SOURCES[$armedSource].label : null);
```

- [ ] **Step 2: Branch the caption by mode**

Replace the existing caption block:

```svelte
      <div class="target-caption" class:narrowed={!targetsAreAll} aria-live="polite">
        Source goes to: <strong>{targetCaption}</strong>{#if targetsAreAll}<span class="tc-hint"> · tap a display to limit</span>{/if}
      </div>
```

with:

```svelte
      {#if $homeRouteMode === 'source'}
        <div class="target-caption" class:narrowed={$armedSource != null} aria-live="polite">
          {#if armedLabel}
            Sending <strong>{armedLabel}</strong><span class="tc-hint"> · tap displays, or Send to All</span>
          {:else}
            <strong>Pick a source</strong><span class="tc-hint"> · then tap displays to send</span>
          {/if}
        </div>
      {:else}
        <div class="target-caption" class:narrowed={!targetsAreAll} aria-live="polite">
          Source goes to: <strong>{targetCaption}</strong>{#if targetsAreAll}<span class="tc-hint"> · tap a display to limit</span>{/if}
        </div>
      {/if}
```

- [ ] **Step 3: Add the chip "has-it" treatment in source mode**

In the display-chip `{#each}`, replace:

```svelte
          {@const ds = displayStates[d.id]}
          {@const targeted = $targetDisplays.has(d.id) && !targetsAreAll}
          <button
            class="disp-chip"
            class:targeted
            class:powered={ds.powerOn}
```

with:

```svelte
          {@const ds = displayStates[d.id]}
          {@const targeted = $targetDisplays.has(d.id) && !targetsAreAll}
          {@const hasArmed = $homeRouteMode === 'source' && armedValue != null && ds.sourceFb === armedValue}
          <button
            class="disp-chip"
            class:targeted={targeted && $homeRouteMode !== 'source'}
            class:has-armed={hasArmed}
            class:powered={ds.powerOn}
```

- [ ] **Step 4: Add the `.has-armed` style**

In the `<style>` block, after the `.disp-chip.targeted { ... }` rule, add:

```css
  /* Source-mode "this display already shows the armed source" — feedback-driven
     (from Display{N}SourceFb), edge-lit like .targeted but paired with the check
     glyph below so state never rides on color alone. */
  .disp-chip.has-armed {
    border-color: rgba(34, 197, 94, 0.5);
    background-color: rgba(34, 197, 94, 0.08);
    box-shadow: 0 0 0 1px rgba(34, 197, 94, 0.35), 0 0 14px rgba(34, 197, 94, 0.1);
  }
  .disp-chip.has-armed .dc-id {
    color: #86efac;
    border-color: rgba(34, 197, 94, 0.4);
  }
```

- [ ] **Step 5: Render the check glyph for `has-armed` too**

In the chip markup, replace:

```svelte
            {#if targeted}
              <svg class="dc-check" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <path d="M4 12.5l5.5 5.5L20 6.5"/>
              </svg>
            {/if}
```

with:

```svelte
            {#if (targeted && $homeRouteMode !== 'source') || hasArmed}
              <svg class="dc-check" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" class:check-green={hasArmed}>
                <path d="M4 12.5l5.5 5.5L20 6.5"/>
              </svg>
            {/if}
```

Then add the green-check style after the existing `.dc-check { ... }` rule:

```css
  .dc-check.check-green { color: #86efac; }
```

- [ ] **Step 6: Type-check**

Run: `npm run check`
Expected: no new errors referencing `Home.svelte`.

- [ ] **Step 7: Verify in dev browser**

At `http://localhost:5174/`: in "Source → Display" mode with nothing armed, caption reads "Pick a source · then tap displays to send". Arm a source → caption reads "Sending <Source> · tap displays, or Send to All". Switch to "Display → Source" → original "Source goes to: All Displays" caption returns. (The green chip "has-it" treatment needs live feedback — verified on glass in Task 7.)

- [ ] **Step 8: Commit**

```bash
git add src/pages/Home.svelte
git commit -m "feat(home): source-mode caption + feedback-driven chip has-it treatment"
```

---

### Task 5: "Send to All" button

**Files:**
- Modify: `src/pages/Home.svelte`

**Interfaces:**
- Consumes: `homeRouteMode`, `armedSource`, `routeArmedToAll`, `flashCard` (Task 3), `armedValue` (Task 4).
- Produces: the Send-to-All control. No new exports.

- [ ] **Step 1: Render the button when source mode + armed**

Directly below the caption `{#if $homeRouteMode === 'source'} ... {/if}` block from Task 4 (i.e., before the `<div class="disp-strip" ...>`), add:

```svelte
      {#if $homeRouteMode === 'source' && $armedSource}
        <button
          class="send-all"
          onclick={() => { routeArmedToAll(); if (armedValue) flashCard(armedValue); }}
          type="button"
          aria-label={`Send ${armedLabel} to all four displays`}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
            <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/>
            <rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>
          </svg>
          Send to All
        </button>
      {/if}
```

- [ ] **Step 2: Add the Send-to-All style**

In the `<style>` block, after the `.adv-float:active { transform: translateY(0); }` rule, add:

```css
  /* Send to All — source-mode minimal-touch shortcut; only rendered while a
     source is armed. Prominent amber, mirrors .adv-float, ≥52px. */
  .send-all {
    appearance: none;
    -webkit-appearance: none;
    display: inline-flex;
    align-items: center;
    gap: 9px;
    min-height: 52px;
    padding: 0 24px;
    border-radius: 11px;
    background-color: #f5a623;
    background-image: linear-gradient(180deg, #f9b94a, #ec9415);
    border: 1px solid rgba(245, 166, 35, 0.6);
    color: #1a1208;
    font-family: inherit;
    font-size: 13px;
    font-weight: 800;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: pointer;
    box-shadow: 0 6px 18px rgba(245, 166, 35, 0.32), 0 0 0 1px rgba(245, 166, 35, 0.1);
    transition: filter 110ms ease, transform 110ms ease;
  }
  .send-all:hover { filter: brightness(1.06); }
  .send-all:active { transform: scale(0.97); transition-duration: 90ms; }
```

- [ ] **Step 3: Type-check**

Run: `npm run check`
Expected: no new errors referencing `Home.svelte`.

- [ ] **Step 4: Verify in dev browser**

At `http://localhost:5174/`: in "Source → Display" mode, the Send-to-All button is absent until a source is armed, then appears between the caption and the display strip. Tapping it flashes the armed card. Switching to "Display → Source" hides it. No console errors.

- [ ] **Step 5: Commit**

```bash
git add src/pages/Home.svelte
git commit -m "feat(home): Send to All shortcut for source-first mode"
```

---

### Task 6: Animations & transitions

**Files:**
- Modify: `src/pages/Home.svelte`

**Interfaces:**
- Consumes: existing markup/classes from Tasks 2–5.
- Produces: transition CSS only. No logic change.

- [ ] **Step 1: Add a Svelte fade transition to the mode-switched zone**

At the top of `Home.svelte`'s `<script>`, after the `import { onMount } from 'svelte';` line, add:

```ts
  import { fade } from 'svelte/transition';
```

Add a `transition:fade` to the caption blocks and Send-to-All button so they cross-fade on mode flip. On the source-mode caption `<div class="target-caption" ...>` (inside the `{#if $homeRouteMode === 'source'}` branch) and on the `<button class="send-all" ...>`, add the attribute:

```svelte
transition:fade={{ duration: 200 }}
```

(Place it alongside the existing attributes on each element.)

- [ ] **Step 2: Add the paint "fly" + Send-to-All stagger keyframes**

In the `<style>` block, after the existing `@keyframes route-flash { ... }` rule, add:

```css
  /* Source-mode paint accent: a quick edge pulse on a chip when it receives the
     armed source. Reuses the chip's own border so the accent is on the element
     itself (no detached halo). Applied via .has-armed appearing. */
  .disp-chip.has-armed {
    animation: chip-paint 280ms ease-out;
  }
  @keyframes chip-paint {
    0%   { box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.55), 0 0 22px rgba(34, 197, 94, 0.3); }
    100% { box-shadow: 0 0 0 1px rgba(34, 197, 94, 0.35), 0 0 14px rgba(34, 197, 94, 0.1); }
  }
  /* Armed card breathing stripe — subtle, ≤300ms loop disabled; one-shot lift. */
  .hero-card.armed { animation: card-arm 220ms ease-out; }
  @keyframes card-arm {
    0%   { transform: translateY(0) scale(0.99); }
    100% { transform: translateY(-2px) scale(1); }
  }
```

- [ ] **Step 3: Extend the reduced-motion guard**

In the existing `@media (prefers-reduced-motion: reduce)` block, add these lines inside it:

```css
    .mode-seg { transition: none; }
    .disp-chip.has-armed { animation: none; }
    .hero-card.armed { animation: none; }
    .send-all { transition: none; }
```

- [ ] **Step 4: Type-check**

Run: `npm run check`
Expected: no new errors referencing `Home.svelte`.

- [ ] **Step 5: Verify in dev browser**

At `http://localhost:5174/`: flipping the toggle cross-fades the caption / Send-to-All zone; arming a source gives a small lift on the card; the Send-to-All button fades in. Toggle OS "reduce motion" and confirm the animations are suppressed (no transform/fade). No console errors.

- [ ] **Step 6: Commit**

```bash
git add src/pages/Home.svelte
git commit -m "feat(home): toggle/arm/paint/send-all animations with reduced-motion guards"
```

---

### Task 7: On-glass verification + deploy

**Files:** none (build + deploy + verify).

- [ ] **Step 1: Full type-check**

Run: `npm run check`
Expected: only the known pre-existing `MicVolumeModal.svelte:64` error + `ConfirmShutdownModal.svelte:29` warning. No new problems.

- [ ] **Step 2: Build the panel**

Run: `npm run build`
Expected: `[build] Done. Output in dist/` with no Rollup errors. (Never `vite build` directly — the `#` path breaks it; `build.mjs` handles it.)

- [ ] **Step 3: Deploy to the tabletop panel (.80)**

Run: `npm run deploy:tabletop`
Expected: `[deploy] ... PROJECTLOAD ... Success. Restarting UI...` then `[deploy] OK`. (Wall `.78` is offline — deploy with `npm run deploy:wall` when it returns.)

- [ ] **Step 4: Verify both workflows on glass (TS-1070 .80, with the processor running)**

Destination-first (default): narrow display chips → tap source → routes to the set → grouping resets to All. Unchanged.

Source-first (toggle to "Source → Display"): tap a source → it arms; tap individual display chips → each immediately routes the armed source and the chip repaints to that source label with the green "has-it" check; "Send to All" routes the armed source to all four; arming a different source moves the armed state; mode persists across nav to Cameras/Audio and back resets to a clean Home. Confirm the per-display `Display{N}SourceFb` labels match what the processor reports.

- [ ] **Step 5: Commit any tuning + final note**

If on-glass tuning required edits, commit them:

```bash
git add -A
git commit -m "fix(home): on-glass tuning for source-select workflow toggle"
```

---

## Self-Review

**Spec coverage:**
- §1 Workflow A unchanged → Tasks 2–6 all branch on mode and leave the destination path intact; verified Task 7 Step 4. ✓
- §1 Workflow B (arm → paint → Send-to-All → persist) → Tasks 3 (arm+paint), 5 (Send-to-All), persistence via `armForPaint` no-timeout (Task 1) + `onMount` disarm (Task 3). ✓
- §2 State model (`homeRouteMode` session store, `armForPaint`, `routeArmedToAll`, reuse `armedSource`/`routeSource`, no optimistic mirror) → Task 1. ✓
- §3 Toggle (2-segment, ≥44px, aria-pressed, non-color-only) → Task 2. ✓
- §4 Source-card armed visual → Task 3. ✓
- §5 Chip semantics by mode + caption → Task 4. ✓
- §6 Send-to-All (only when armed) → Task 5. ✓
- §7 Animations (toggle flip, arm, paint, send-all, reduced-motion) → Task 6. ✓
- §8 Pure panel-side, persona compliance → Global Constraints + reused signals only. ✓

**Placeholder scan:** No TBD/TODO; every code step shows exact content. ✓

**Type consistency:** `armForPaint(sourceId: SourceId)`, `routeArmedToAll()`, `homeRouteMode`, `HomeRouteMode`, `onSourceTap`, `onChipTap`, `flashCard`, `armedValue`, `armedLabel`, `hasArmed` used consistently across tasks. `src.key` is typed `SourceId` (matches `SOURCES` const in `Home.svelte`). `disarm` imported in Task 3 and used in `onMount`. ✓

**Note:** Browser-dev cannot exercise live feedback (no processor), so chip repaint/has-it is explicitly deferred to Task 7 on-glass — flagged in each affected verify step rather than silently assumed.
