# Plan 1 — ConfirmShutdownModal Upgrade (Mockup #15)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite `ConfirmShutdownModal.svelte` to match Mockup #15 (animated danger stripe, SVG countdown ring, shutdown checklist, vacancy strip) while preserving the existing 4-prop API. Update `Home.svelte` to pass the two new optional props so the panel sees the full design.

**Architecture:** Single Svelte 5 component rewrite (script + markup + style in one atomic commit). New optional props (`vacancyMinutes`, `shutdownItems`) are additive — existing call sites continue to work unchanged. Visual additions are CSS + SVG; the existing 1-second `setInterval` countdown loop is preserved.

**Tech Stack:** Svelte 5 (runes mode — `$props`, `$state`, `$derived`, `$effect`, `onDestroy`), TypeScript, no new deps.

**Spec reference:** [docs/superpowers/specs/2026-05-02-four-mockup-pages-design.md §4](../specs/2026-05-02-four-mockup-pages-design.md#4-plan-1--15-shutdown-modal-upgrade).

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify (full rewrite) | `MCCCD-AA140/src/components/ConfirmShutdownModal.svelte` | Confirmation modal — danger styling, countdown ring, optional checklist + vacancy strip. |
| Modify | `MCCCD-AA140/src/pages/Home.svelte` | Update modal call site to pass `vacancyMinutes` and `shutdownItems`. |

No new files. No deletions. No `contract.ts` changes.

---

## Task 1: Rewrite ConfirmShutdownModal.svelte

**Files:**
- Modify (full rewrite): `MCCCD-AA140/src/components/ConfirmShutdownModal.svelte`

This task is a single atomic rewrite — markup, script, and CSS land in one commit because partial rewrites leave the component broken.

- [ ] **Step 1: Replace the file with the new component**

Write the full file contents to `MCCCD-AA140/src/components/ConfirmShutdownModal.svelte`:

```svelte
<script lang="ts">
  import { onDestroy } from 'svelte';

  type ShutdownItem = {
    icon: 'display' | 'audio' | 'camera';
    label: string;
  };

  interface Props {
    open: boolean;
    countdown?: number;                  // seconds; default 30
    title?: string;                      // default "Shut Down Room?"
    body?: string;                       // optional; vacancy-aware default
    vacancyMinutes?: number;             // optional; drives the bottom strip
    shutdownItems?: ShutdownItem[];      // optional; checklist rows
    onConfirm: () => void;
    onCancel: () => void;
  }

  let {
    open,
    countdown = 30,
    title = 'Shut Down Room?',
    body,
    vacancyMinutes,
    shutdownItems,
    onConfirm,
    onCancel,
  }: Props = $props();

  let remaining = $state(countdown);
  let timer: ReturnType<typeof setInterval> | undefined;

  // SVG ring math: circumference of r=52 ≈ 326.
  // dashoffset = circumference × (1 − remaining/countdown).
  // At remaining=countdown → 0 (full ring). At remaining=0 → 326 (empty).
  const RING_CIRCUMFERENCE = 326;
  let strokeDashoffset = $derived(
    RING_CIRCUMFERENCE * (1 - remaining / countdown)
  );

  let displayBody = $derived(
    body ??
      (vacancyMinutes !== undefined
        ? `The room has been vacant for ${vacancyMinutes} minutes. All displays, audio, and cameras will power off. This cannot be undone without a full restart.`
        : 'Are you sure you want to shut down?')
  );

  $effect(() => {
    if (open) {
      remaining = countdown;
      timer = setInterval(() => {
        remaining -= 1;
        if (remaining <= 0) {
          if (timer !== undefined) clearInterval(timer);
          timer = undefined;
          onConfirm();
        }
      }, 1000);
    } else if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
  });

  onDestroy(() => {
    if (timer !== undefined) clearInterval(timer);
  });

  function handleConfirm() {
    if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
    onConfirm();
  }

  function handleCancel() {
    if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
    onCancel();
  }
</script>

{#if open}
  <div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="shutdown-title">
    <div class="modal-card">

      <div class="modal-stripe" aria-hidden="true"></div>

      <div class="modal-body">

        <div class="modal-icon" aria-hidden="true">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M12 3v9"/>
            <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
          </svg>
        </div>

        <h2 id="shutdown-title" class="modal-title">{title}</h2>
        <p class="modal-body-text">{displayBody}</p>

        <div class="countdown-wrap">
          <svg class="countdown-svg" viewBox="0 0 120 120" aria-hidden="true">
            <circle class="countdown-track" cx="60" cy="60" r="52"/>
            <circle
              class="countdown-fill"
              cx="60" cy="60" r="52"
              stroke-dasharray={RING_CIRCUMFERENCE}
              stroke-dashoffset={strokeDashoffset}
            />
          </svg>
          <span class="countdown-num" aria-live="polite">{remaining}</span>
          <span class="countdown-sec">sec</span>
        </div>

        <div class="modal-actions">
          <button class="btn-cancel" onclick={handleCancel}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
              <path d="M18 6L6 18M6 6l12 12"/>
            </svg>
            Cancel
          </button>
          <button class="btn-confirm" onclick={handleConfirm}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
              <path d="M12 3v9"/>
              <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
            </svg>
            Shut Down Now
          </button>
        </div>
      </div>

      {#if shutdownItems && shutdownItems.length > 0}
        <div class="shutdown-list">
          <p class="sl-label">Will be powered off</p>
          {#each shutdownItems as item}
            <div class="sl-item">
              <div class="sl-icon" aria-hidden="true">
                {#if item.icon === 'display'}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                {:else if item.icon === 'audio'}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/></svg>
                {:else}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7h4l2-2h4l2 2h4v12H4z"/><circle cx="12" cy="13" r="3.6"/></svg>
                {/if}
              </div>
              <span>{item.label}</span>
            </div>
          {/each}
        </div>
      {/if}

      {#if vacancyMinutes !== undefined}
        <div class="vacancy-bar">
          <span class="vacancy-dot" aria-hidden="true"></span>
          Triggered by occupancy timeout · Room vacant {vacancyMinutes} min · Auto-shutdown threshold: 15 min
        </div>
      {/if}

    </div>
  </div>
{/if}

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(4, 8, 18, 0.72);
    backdrop-filter: blur(8px);
    display: grid;
    place-items: center;
    z-index: 1000;
    animation: fade-in 140ms ease;
  }

  .modal-card {
    width: 560px;
    max-width: 92%;
    background: rgba(12, 20, 36, 0.98);
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 20px;
    box-shadow:
      0 0 0 1px rgba(239, 68, 68, 0.08),
      0 40px 80px rgba(0, 0, 0, 0.7),
      0 0 60px rgba(239, 68, 68, 0.06);
    display: flex;
    flex-direction: column;
    align-items: center;
    overflow: hidden;
    animation: modal-in 250ms cubic-bezier(0.16, 1, 0.3, 1);
  }

  /* Animated danger top stripe */
  .modal-stripe {
    width: 100%;
    height: 4px;
    background: linear-gradient(90deg, #ef4444, #f97316, #ef4444);
    background-size: 200% 100%;
    animation: stripe-slide 2s linear infinite;
  }

  .modal-body {
    padding: 36px 40px 32px;
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
  }

  .modal-icon {
    width: 72px;
    height: 72px;
    border-radius: 50%;
    background: rgba(239, 68, 68, 0.1);
    border: 1.5px solid rgba(239, 68, 68, 0.35);
    display: grid;
    place-items: center;
    color: #fca5a5;
    margin-bottom: 20px;
    box-shadow: 0 0 30px rgba(239, 68, 68, 0.12);
  }

  .modal-title {
    margin: 0 0 8px;
    font-size: 26px;
    font-weight: 900;
    letter-spacing: -0.02em;
    color: #ffffff;
    text-align: center;
  }

  .modal-body-text {
    margin: 0 0 32px;
    font-size: 14px;
    color: var(--color-copy-soft, #94a3b8);
    text-align: center;
    line-height: 1.6;
    max-width: 380px;
  }

  /* Countdown ring */
  .countdown-wrap {
    position: relative;
    width: 120px;
    height: 120px;
    margin-bottom: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .countdown-svg {
    position: absolute;
    inset: 0;
    transform: rotate(-90deg);
  }
  .countdown-track {
    fill: none;
    stroke: rgba(239, 68, 68, 0.12);
    stroke-width: 6;
  }
  .countdown-fill {
    fill: none;
    stroke: #ef4444;
    stroke-width: 6;
    stroke-linecap: round;
    transition: stroke-dashoffset 1s linear;
    filter: drop-shadow(0 0 4px rgba(239, 68, 68, 0.5));
  }
  .countdown-num {
    font-size: 42px;
    font-weight: 900;
    color: #fca5a5;
    font-variant-numeric: tabular-nums;
    letter-spacing: -0.02em;
    line-height: 1;
    position: relative;
    z-index: 1;
  }
  .countdown-sec {
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: rgba(252, 165, 165, 0.6);
    position: absolute;
    bottom: 16px;
  }

  /* Action buttons */
  .modal-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px;
    width: 100%;
  }
  .btn-cancel,
  .btn-confirm {
    padding: 16px 24px;
    border-radius: 12px;
    font-size: 15px;
    font-weight: 700;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background 140ms ease, border-color 140ms ease;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 9px;
  }
  .btn-cancel {
    background: rgba(30, 41, 59, 0.7);
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.14));
    color: var(--color-copy, #e2e8f0);
  }
  .btn-cancel:hover {
    background: rgba(51, 65, 85, 0.8);
    border-color: rgba(148, 163, 184, 0.3);
  }
  .btn-confirm {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.4);
    color: #fca5a5;
    box-shadow: 0 0 20px rgba(239, 68, 68, 0.06);
  }
  .btn-confirm:hover {
    background: rgba(239, 68, 68, 0.25);
    border-color: rgba(239, 68, 68, 0.6);
  }

  /* Shutdown checklist */
  .shutdown-list {
    width: 100%;
    border-top: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.14));
    padding: 20px 40px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    background: rgba(6, 10, 20, 0.5);
  }
  .sl-label {
    margin: 0 0 4px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #4d6070);
  }
  .sl-item {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 12px;
    color: var(--color-copy-soft, #7c93a8);
  }
  .sl-icon {
    width: 24px;
    height: 24px;
    border-radius: 5px;
    background: rgba(239, 68, 68, 0.08);
    border: 0.5px solid rgba(239, 68, 68, 0.18);
    display: grid;
    place-items: center;
    color: #fca5a5;
    flex-shrink: 0;
  }

  /* Vacancy strip */
  .vacancy-bar {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 10px 40px;
    background: rgba(245, 158, 11, 0.05);
    border-top: 0.5px solid rgba(245, 158, 11, 0.12);
    font-size: 11px;
    color: rgba(252, 211, 77, 0.7);
    font-weight: 600;
  }
  .vacancy-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #fcd34d;
    box-shadow: 0 0 6px rgba(252, 211, 77, 0.4);
    animation: vacancy-pulse 2s ease-in-out infinite;
  }

  /* Animations */
  @keyframes fade-in {
    from { opacity: 0; }
    to { opacity: 1; }
  }
  @keyframes modal-in {
    from { opacity: 0; transform: scale(0.94) translateY(12px); }
    to { opacity: 1; transform: scale(1) translateY(0); }
  }
  @keyframes stripe-slide {
    0%   { background-position: 0% 50%; }
    100% { background-position: 200% 50%; }
  }
  @keyframes vacancy-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.4; }
  }

  @media (prefers-reduced-motion: reduce) {
    .modal-backdrop,
    .modal-card,
    .modal-stripe,
    .vacancy-dot {
      animation: none;
    }
    .countdown-fill {
      transition: none;
    }
  }
</style>
```

- [ ] **Step 2: Type-check the rewritten component**

Run from `MCCCD-AA140/`:
```bash
npm run check
```
Expected: `svelte-check` reports `0 errors and 0 warnings` (or no errors involving `ConfirmShutdownModal.svelte`).

If errors mention runes (`$props`, `$state`, `$derived`, `$effect`): the project is on Svelte 5 — confirm `svelte.config.js` and `tsconfig.json` haven't changed. (They haven't in this plan.)

- [ ] **Step 3: Build the project**

Run from `MCCCD-AA140/`:
```bash
npm run build
```
Expected: build completes; `dist/` populated. No new warnings about `ConfirmShutdownModal.svelte`.

- [ ] **Step 4: Browser smoke test (dev server)**

The Home power button only opens the modal when `$systemPowerFb` is true ([Home.svelte:56-63](../../src/pages/Home.svelte#L56-L63)). In dev mode the CH5 feedback signals don't fire, so we'll force-open the modal directly.

Temporarily edit [MCCCD-AA140/src/pages/Home.svelte](../../src/pages/Home.svelte) line 35 from:
```ts
let showShutdownModal = $state(false);
```
to:
```ts
let showShutdownModal = $state(true);  // TEMP: smoke test
```

Run from `MCCCD-AA140/`:
```bash
npm run dev
```
In a browser at `http://localhost:5173/`, the modal opens immediately on page load. Check visually:
   - Modal card has a 4px gradient stripe at the top, sliding left-to-right.
   - 72px circular danger icon above the title.
   - Title "Shut Down Room?".
   - 120px ring with countdown number depleting clockwise from full.
   - Cancel + Shut Down Now buttons.
   - No checklist or vacancy strip yet (Home isn't passing those props yet — that's Task 2).

Stop dev server (Ctrl+C). **Revert the temp change** — set `showShutdownModal` back to `$state(false)`. Verify with `git diff MCCCD-AA140/src/pages/Home.svelte` that there are no remaining changes to that file before committing.

- [ ] **Step 5: Commit Task 1**

From the repo root:
```bash
git add MCCCD-AA140/src/components/ConfirmShutdownModal.svelte
git commit -m "$(cat <<'EOF'
feat(modal): rewrite ConfirmShutdownModal to Mockup #15 design

Animated danger gradient stripe, 72px circular danger icon, 120px SVG
countdown ring with smooth 1s linear stroke-dashoffset transition,
optional shutdown checklist, optional vacancy strip. Required props
(open/countdown/title/body/onConfirm/onCancel) unchanged. New optional
props vacancyMinutes and shutdownItems are additive.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

Expected: commit succeeds; `git log -1` shows the new commit.

---

## Task 2: Pass new optional props from Home.svelte

**Files:**
- Modify: `MCCCD-AA140/src/pages/Home.svelte` (the `<ConfirmShutdownModal/>` call at the bottom — currently lines 223–228)

- [ ] **Step 1: Update the modal call site**

In [MCCCD-AA140/src/pages/Home.svelte](../../src/pages/Home.svelte), find the existing call site:

```svelte
<ConfirmShutdownModal
  open={showShutdownModal}
  countdown={30}
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>
```

Replace it with:

```svelte
<ConfirmShutdownModal
  open={showShutdownModal}
  countdown={30}
  vacancyMinutes={$occupancyState === 2 ? $shutdownCountdown : undefined}
  shutdownItems={[
    { icon: 'display', label: '3 Displays (D1 Front Left, D2 Front Right, D3 Rear)' },
    { icon: 'audio',   label: 'Audio system + all 5 microphone channels' },
    { icon: 'camera',  label: 'Camera system (2 PTZ cameras)' },
  ]}
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>
```

Rationale:
- `vacancyMinutes` only renders the strip when the system is *actually* in vacant-warn state (`occupancyState === 2`). If the user shut down manually (not from a vacancy timeout), `vacancyMinutes` stays `undefined` and the strip is hidden — which matches the spec's optional behavior.
- `$shutdownCountdown` is the **minutes-remaining-before-auto-shutdown** analog feedback (already imported at line 12 of Home.svelte). The `vacancy-bar` text reads "Room vacant N min" — that's not strictly the same number as `shutdownCountdown` (which counts *down*, not *up*). For Plan 1 we use `shutdownCountdown` as a stand-in; if SIMPL adds a separate "minutes-vacant" feedback later, swap the prop. (Logged as a follow-up below.)
- `shutdownItems` is the spec's recommended default for AA140: 3 displays, 5 mics + audio, 2 PTZ cameras.

- [ ] **Step 2: Type-check**

```bash
cd MCCCD-AA140 && npm run check
```
Expected: `0 errors`. The new props are all typed correctly because `vacancyMinutes` accepts `number | undefined` (same as the optional prop type) and `shutdownItems` matches the `ShutdownItem[]` shape.

- [ ] **Step 3: Build**

```bash
cd MCCCD-AA140 && npm run build
```
Expected: build succeeds.

- [ ] **Step 4: Browser smoke test (dev server)**

Same temp-flag pattern as Task 1 Step 4: temporarily set `showShutdownModal = $state(true)` to force-open the modal on dev page load.

```bash
cd MCCCD-AA140 && npm run dev
```
At `http://localhost:5173/`, verify all new visual elements from Task 2 are present in addition to the Task-1 result:
- Shutdown checklist with 3 rows (display / audio / camera icons).
- Vacancy strip at the bottom — *only* visible if `$occupancyState === 2`. In dev mode without SIMPL feeding the panel, `occupancyState` is `0` and the strip will be hidden. To force-test the strip itself, temporarily change the line to `vacancyMinutes={12}` (drop the conditional), observe, then revert. Be sure to revert BOTH temp changes (the `showShutdownModal` initial value AND any `vacancyMinutes` hard-code) before committing.

Stop dev server. Run `git diff MCCCD-AA140/src/pages/Home.svelte` and confirm only the intended permanent changes remain.

- [ ] **Step 5: Commit Task 2**

```bash
git add MCCCD-AA140/src/pages/Home.svelte
git commit -m "$(cat <<'EOF'
feat(home): pass vacancyMinutes + shutdownItems to ConfirmShutdownModal

Renders the new vacancy strip when occupancyState=2 (vacant-warn) and
always shows the shutdown checklist with the AA140 default items
(3 displays, audio + 5 mics, 2 PTZ cameras).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Panel-side acceptance & deploy

The browser smoke tests in Tasks 1–2 don't substitute for panel verification per the deploy workflow. This task is the final gate.

**Files:** none modified.

- [ ] **Step 1: Archive the panel build**

```bash
cd MCCCD-AA140 && npm run archive
```
Expected: a fresh `.ch5z` appears in `MCCCD-AA140/output/`. Note the filename for Step 2.

- [ ] **Step 2: Deploy to TS-1070**

```bash
cd MCCCD-AA140 && npm run deploy:tabletop
```
Expected: `scripts/deploy.py` reports a successful upload to `192.168.2.53` (admin/password). Panel reboots and reloads the panel app (typically 5–15 seconds).

If `deploy:tabletop` is not the right script for the current TS-1070 (e.g., the panel is at the wall location), fall back to `npm run deploy`. Per memory, the default panel is the TS-1070 tabletop at `192.168.2.53`.

- [ ] **Step 3: Acceptance test on the panel**

On the TS-1070:
1. With the system on, tap the Power button on Home. The new modal should appear.
2. Verify each visual element from the mockup:
   - [ ] Animated 4px gradient stripe at top of the card (slides left → right continuously).
   - [ ] 72px circular danger-red icon above the title.
   - [ ] Title "Shut Down Room?".
   - [ ] Body text reflects the vacancy state if `occupancyState=2`, otherwise the generic "Are you sure" fallback.
   - [ ] 120px SVG ring is visible. Ring is full at the start and depletes clockwise as the timer counts down. Transition between seconds is smooth (not steppy).
   - [ ] Countdown number is large red and tabular-numeric.
   - [ ] Cancel + Shut Down Now buttons render side-by-side.
   - [ ] Shutdown checklist with 3 rows (display / audio / camera icons).
   - [ ] Vacancy strip at bottom (only if `occupancyState=2`; pulsing yellow dot).
3. Tap **Cancel** — modal closes; system stays on; no power state change.
4. Tap power again, let the ring deplete fully — `displayPower` pulses fire on auto-confirm; system powers off normally.
5. Tap power again, tap **Shut Down Now** before timer expires — `displayPower` pulses fire; system powers off normally.

- [ ] **Step 4: If any acceptance check fails**

Stop. Do not commit further. Report the failure (which check #, what was observed) before attempting fixes. Common failure modes:
- Ring doesn't tick smoothly → check `transition: stroke-dashoffset 1s linear` on `.countdown-fill`.
- Stripe not animating → check `@keyframes stripe-slide` and `background-size: 200% 100%`.
- Vacancy strip never shows → check that the panel is actually reaching `occupancyState=2`. Force-test by hard-coding `vacancyMinutes={12}` temporarily.
- Modal doesn't open → verify the Home power button still calls `powerButtonTapped()` and `showShutdownModal` toggles.
- Type errors after pull → re-run `npm run check`; verify Svelte 5 runes are recognized.

- [ ] **Step 5: Tag the deploy in the project log**

Append a one-line entry to `MCCCD-AA140/docs/Handoffs/` (whichever current handoff doc is open, or create `2026-05-02-plan-1-deploy-handoff.md` if none) noting:
```
2026-05-02 — Plan 1 (#15 ConfirmShutdownModal upgrade) deployed to TS-1070, panel acceptance passed. Commit <commit-sha>.
```

- [ ] **Step 6: Commit handoff entry**

```bash
git add MCCCD-AA140/docs/Handoffs/
git commit -m "$(cat <<'EOF'
docs(handoff): plan 1 (#15 shutdown modal) deployed + accepted on TS-1070

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Follow-ups (not in this plan)

- **Minutes-vacant feedback signal.** `vacancyMinutes` is currently bound to `$shutdownCountdown` (minutes-until-shutdown). If SIMPL exposes a separate "minutes-vacant" analog feedback, swap the prop. Track when contract regen happens.
- **Reduced-motion panel test.** The TS-1070 doesn't expose `prefers-reduced-motion` in the same way a desktop browser does. Acceptance step 3 doesn't cover this — leave the CSS `@media` query in place as best-effort.

## Done when

All Task 1–3 steps are checked, the modal renders correctly on TS-1070, both Cancel and Shut Down Now paths are verified on the panel, and a handoff line is in the docs.
