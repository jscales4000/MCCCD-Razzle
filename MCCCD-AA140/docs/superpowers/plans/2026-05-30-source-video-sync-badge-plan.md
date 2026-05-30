# Source Video-Sync Badge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a tri-state (Live / Ready / Idle) video-sync badge to each Home source card, driven by 8 new digital feedback signals, plus AirMedia method-aware and Laptop dual-input sub-labels.

**Architecture:** All-boolean SIMPL FBs avoid the known CrComLib analog-subscribe bug. New stores in `signals.ts` subscribe to each FB; `Home.svelte` computes per-card state via a single `$derived` and renders a corner dot + per-source sub-label variants. No new files, no new components.

**Tech Stack:** Svelte 5 (runes), TypeScript, CH5 CrComLib (`subscribeDigital`), Vite.

**Spec:** [`MCCCD-AA140/docs/superpowers/specs/2026-05-30-source-video-sync-badge-design.md`](../specs/2026-05-30-source-video-sync-badge-design.md)

**Branch:** `feat/source-video-sync-badge` (already cut from `main`).

**Testing reality:** This project has no Svelte component test framework. Verification gates per task are (1) `npm run build` for type/compile correctness and (2) inspection of `npm run dev` in a browser for visible behavior. Final task deploys to both panels per the standing `npm run deploy:both` rule.

---

## File map

| File | Action | Responsibility |
|---|---|---|
| `MCCCD-AA140/src/lib/contract.ts` | Modify | Add 8 new `SIGNALS.*` entries for video sync FBs |
| `MCCCD-AA140/src/lib/stores/signals.ts` | Modify | Export 8 new `writable<boolean>` stores + wire `subscribeDigital` for each in `initSignals()` |
| `MCCCD-AA140/src/pages/Home.svelte` | Modify | Add `key` to `SOURCES`; add `sourceStates` derived; render `.sync-dot`, AirMedia method swap, Laptop dual-token sub-label; add scoped CSS |

No new files. No new components.

---

## Task 1: Add 8 video-sync signals to the contract

**Files:**
- Modify: `MCCCD-AA140/src/lib/contract.ts`

- [ ] **Step 1: Insert the new SIGNALS block**

Insert immediately after the existing Plan 4 / Audio Mixer block (currently the last entries in the `SIGNALS` object, ending with `audioLinkCeilings12Fb`), before the closing `} as const;`. Match existing indentation (2-space).

```ts
  // Source video sync feedback (digital FB, panel-side only)
  // Drives the tri-state corner badge on each Home source card.
  // AirMedia method priority on simultaneous-fire: tx3 > airPlay > miracast.
  roomPcSync:           `${ROOM_NAME}.RoomPcSync`,
  extPcSync:            `${ROOM_NAME}.ExtPcSync`,
  airMediaSync:         `${ROOM_NAME}.AirMediaSync`,
  airMediaMiracast:     `${ROOM_NAME}.AirMediaMiracast`,
  airMediaAirPlay:      `${ROOM_NAME}.AirMediaAirPlay`,
  airMediaTx3:          `${ROOM_NAME}.AirMediaTx3`,
  laptopHdmiSync:       `${ROOM_NAME}.LaptopHdmiSync`,
  laptopUsbcSync:       `${ROOM_NAME}.LaptopUsbcSync`,
```

- [ ] **Step 2: Verify build passes**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success with no TypeScript errors. There are no consumers yet, so this only validates the constants are syntactically valid additions to the `as const` literal.

- [ ] **Step 3: Commit**

```bash
git add MCCCD-AA140/src/lib/contract.ts
git commit -m "$(cat <<'EOF'
feat(contract): add 8 video-sync FB signals for Home source badges

3 NVX HDMI sync + 3 AirMedia method states + AirMedia HDMI sync + 2 NVX-384
BYOD input syncs. All digital FB; no panel publishes.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Add 8 stores + subscribeDigital wiring

**Files:**
- Modify: `MCCCD-AA140/src/lib/stores/signals.ts`

- [ ] **Step 1: Add the 8 store exports**

Insert after the `micCeiling3Connected` writable (end of the `// Mic connection / signal-present` block, before the `// Occupancy + shutdown` block):

```ts
// Source video sync (panel-side only; SIMPL feedback drives Home card dots)
export const roomPcSync          = writable<boolean>(false);
export const extPcSync           = writable<boolean>(false);
export const airMediaSync        = writable<boolean>(false);
export const airMediaMiracast    = writable<boolean>(false);
export const airMediaAirPlay     = writable<boolean>(false);
export const airMediaTx3         = writable<boolean>(false);
export const laptopHdmiSync      = writable<boolean>(false);
export const laptopUsbcSync      = writable<boolean>(false);
```

- [ ] **Step 2: Wire the 8 subscriptions in `initSignals()`**

Insert immediately after the existing `micCeiling3Connected` subscription (the last line of the mic-connected block):

```ts
  // Source video sync (8 digital FBs)
  subscribeDigital(SIGNALS.roomPcSync,       (v) => roomPcSync.set(v));
  subscribeDigital(SIGNALS.extPcSync,        (v) => extPcSync.set(v));
  subscribeDigital(SIGNALS.airMediaSync,     (v) => airMediaSync.set(v));
  subscribeDigital(SIGNALS.airMediaMiracast, (v) => airMediaMiracast.set(v));
  subscribeDigital(SIGNALS.airMediaAirPlay,  (v) => airMediaAirPlay.set(v));
  subscribeDigital(SIGNALS.airMediaTx3,      (v) => airMediaTx3.set(v));
  subscribeDigital(SIGNALS.laptopHdmiSync,   (v) => laptopHdmiSync.set(v));
  subscribeDigital(SIGNALS.laptopUsbcSync,   (v) => laptopUsbcSync.set(v));
```

- [ ] **Step 3: Verify build passes**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success. The new stores are exported but not yet imported anywhere; TypeScript will not warn about unused exports.

- [ ] **Step 4: Commit**

```bash
git add MCCCD-AA140/src/lib/stores/signals.ts
git commit -m "$(cat <<'EOF'
feat(stores): subscribe to 8 video-sync FB signals

8 writable<boolean> stores added with subscribeDigital wiring in
initSignals(). Defaults to false; populated by SIMPL feedback at runtime.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Update Home.svelte SOURCES table

**Files:**
- Modify: `MCCCD-AA140/src/pages/Home.svelte` (the `SOURCES` constant, lines 22-27 in the pre-task file)

- [ ] **Step 1: Replace the SOURCES constant**

Find:

```ts
  const SOURCES = [
    { value: 1, name: 'Room PC',  sub: 'HDMI 1' },
    { value: 2, name: 'Ext PC',   sub: 'HDMI 2' },
    { value: 3, name: 'AirMedia', sub: 'Wireless' },
    { value: 4, name: 'Laptop',   sub: 'HDMI 3' },
  ] as const;
```

Replace with:

```ts
  // `key` selects which sync FB stores feed `sourceStates` below.
  // `sub` is the static sub-label; null = rendered specially (Laptop dual-token).
  const SOURCES = [
    { value: 1, name: 'Room PC',  key: 'roomPc',   sub: 'HDMI 1'   },
    { value: 2, name: 'Ext PC',   key: 'extPc',    sub: 'HDMI 2'   },
    { value: 3, name: 'AirMedia', key: 'airMedia', sub: 'WIRELESS' },
    { value: 4, name: 'Laptop',   key: 'laptop',   sub: null       },
  ] as const;
```

- [ ] **Step 2: Verify build still passes**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success. `sub` is currently consumed as `{src.sub}` in the markup — when `null` it will render as empty string. Visual will look slightly off for Laptop until Task 5 lands, but that's fine inside one branch.

- [ ] **Step 3: No commit yet**

This task lives in the same commit as Tasks 4 + 5; they all touch Home.svelte and together form the visible feature.

---

## Task 4: Add store imports + sourceStates derived

**Files:**
- Modify: `MCCCD-AA140/src/pages/Home.svelte` (imports at top of `<script>`, plus new `$derived` block)

- [ ] **Step 1: Extend the signals import**

Find:

```ts
  import {
    panelOnline,
    display1SourceFb,
    systemPowerFb,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';
```

Replace with:

```ts
  import {
    panelOnline,
    display1SourceFb,
    systemPowerFb,
    occupancyState, shutdownCountdown,
    roomPcSync, extPcSync,
    airMediaSync, airMediaMiracast, airMediaAirPlay, airMediaTx3,
    laptopHdmiSync, laptopUsbcSync,
  } from '../lib/stores/signals';
```

- [ ] **Step 2: Add the airMediaState helper + sourceStates derived**

Insert after the `SOURCES` constant block (right before `function selectSourceForAll`):

```ts
  // AirMedia rolls 4 signals (sync + 3 sharing methods) into the tri-state model.
  // Sharing-method priority on simultaneous fire: TX3 > AirPlay > Miracast.
  function airMediaState(sync: boolean, miracast: boolean, airplay: boolean, tx3: boolean) {
    const sharing = miracast || airplay || tx3;
    if (sharing) {
      const method = tx3 ? 'AM-TX3' : airplay ? 'AIRPLAY' : 'MIRACAST';
      return { state: 'live' as const, subDetail: method };
    }
    if (sync) return { state: 'ready' as const, subDetail: null };
    return { state: 'idle' as const, subDetail: null };
  }

  // Per-card state, keyed by SOURCES[i].key. Drives the corner dot + AirMedia sub.
  let sourceStates = $derived({
    roomPc:   { state: ($roomPcSync ? 'live' : 'idle') as 'live' | 'idle', subDetail: null as string | null },
    extPc:    { state: ($extPcSync  ? 'live' : 'idle') as 'live' | 'idle', subDetail: null as string | null },
    airMedia: airMediaState($airMediaSync, $airMediaMiracast, $airMediaAirPlay, $airMediaTx3),
    laptop:   { state: (($laptopHdmiSync || $laptopUsbcSync) ? 'live' : 'idle') as 'live' | 'idle', subDetail: null as string | null },
  });
```

- [ ] **Step 3: Verify build**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success. Imports are now used by the derived. No template changes yet — page should render unchanged in dev mode (the `sourceStates` value isn't consumed yet).

- [ ] **Step 4: No commit yet**

Same commit as Tasks 3 + 5.

---

## Task 5: Render sync dot, AirMedia label swap, Laptop dual-token

**Files:**
- Modify: `MCCCD-AA140/src/pages/Home.svelte` (the `{#each SOURCES as src}` block in the template, plus CSS at the end of `<style>`)

- [ ] **Step 1: Replace the source-card template**

Find the `{#each SOURCES as src}` block:

```svelte
        {#each SOURCES as src}
          <button
            class="hero-card"
            class:active={$display1SourceFb === src.value}
            onclick={() => selectSourceForAll(src.value)}
            aria-pressed={$display1SourceFb === src.value}
            aria-label={`Send ${src.name} to all displays`}
          >
            {#if src.value === 1}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
            {:else if src.value === 2}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M3 10h18M8 20h8M12 16v4"/></svg>
            {:else if src.value === 3}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>
            {:else}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="4" width="20" height="13" rx="2"/><path d="M2 20h20"/></svg>
            {/if}
            <span class="hc-name">{src.name}</span>
            <span class="hc-sub">{src.sub}</span>
          </button>
        {/each}
```

Replace with:

```svelte
        {#each SOURCES as src}
          {@const s = sourceStates[src.key]}
          <button
            class="hero-card"
            class:active={$display1SourceFb === src.value}
            onclick={() => selectSourceForAll(src.value)}
            aria-pressed={$display1SourceFb === src.value}
            aria-label={`Send ${src.name} to all displays${s.state !== 'idle' ? ' — ' + s.state : ''}`}
          >
            {#if s.state !== 'idle'}
              <span class="sync-dot {s.state}" aria-hidden="true"></span>
            {/if}
            {#if src.value === 1}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
            {:else if src.value === 2}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M3 10h18M8 20h8M12 16v4"/></svg>
            {:else if src.value === 3}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>
            {:else}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="4" width="20" height="13" rx="2"/><path d="M2 20h20"/></svg>
            {/if}
            <span class="hc-name">{src.name}</span>
            {#if src.key === 'laptop'}
              <span class="hc-sub laptop-sub">
                <span class="hc-sub-token" class:lit={$laptopHdmiSync}>HDMI</span>
                <span class="hc-sub-token" class:lit={$laptopUsbcSync}>USBC</span>
              </span>
            {:else if src.key === 'airMedia'}
              <span class="hc-sub">{s.subDetail ?? src.sub}</span>
            {:else}
              <span class="hc-sub">{src.sub}</span>
            {/if}
          </button>
        {/each}
```

- [ ] **Step 2: Add scoped CSS**

Append to the end of the `<style>` block, before the closing `</style>`:

```css
  /* Sync badge — top-right corner of each hero card.
     Sits BELOW the 3px orange active-routing stripe (top:0;height:3px), so they
     never overlap. Green = live, amber = ready. Idle = not rendered. */
  .sync-dot {
    position: absolute;
    top: 10px;
    right: 10px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
    pointer-events: none;
  }
  .sync-dot.live {
    background: #22c55e;
    box-shadow: 0 0 8px rgba(34, 197, 94, 0.65);
    animation: sync-pulse 2.2s ease-in-out infinite;
  }
  .sync-dot.ready {
    background: #f59e0b;
    box-shadow: 0 0 6px rgba(245, 158, 11, 0.5);
  }
  @keyframes sync-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.45; }
  }

  /* Laptop dual-token sub-label — both tokens always rendered; .lit on whichever
     NVX-384 input currently has sync. Both lit handled implicitly. */
  .hc-sub.laptop-sub {
    display: inline-flex;
    gap: 8px;
    align-items: baseline;
  }
  .hc-sub-token {
    color: var(--color-copy-muted, #64748b);
    transition: color 160ms ease;
  }
  .hc-sub-token.lit {
    color: var(--color-copy, #e2e8f0);
  }
  .hero-card.active .hc-sub-token.lit {
    color: #f5a623;
  }
```

- [ ] **Step 3: Update the existing prefers-reduced-motion block**

Find at the end of the `<style>` block:

```css
  @media (prefers-reduced-motion: reduce) {
    .pdot { animation: none; }
    .hero-card { transition: none; }
  }
```

Replace with:

```css
  @media (prefers-reduced-motion: reduce) {
    .pdot { animation: none; }
    .hero-card { transition: none; }
    .sync-dot.live { animation: none; }
    .hc-sub-token { transition: none; }
  }
```

- [ ] **Step 4: Verify build**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success.

- [ ] **Step 5: Verify visual in dev mode**

```bash
cd MCCCD-AA140 && npm run dev
```

Open the served URL (typically `http://localhost:5173`) in a browser. Tap "Power On" through the splash to reach Home (or set `userPoweredOn` manually if needed).

Expected baseline (all 8 sync stores still false — no live SIMPL):
- All four cards show **no sync dot** in the top-right corner.
- Room PC sub-label reads `HDMI 1`. Ext PC reads `HDMI 2`. AirMedia reads `WIRELESS`. Laptop shows two side-by-side tokens `HDMI` and `USBC`, both dimmed to muted gray.
- Tap a source — the active orange stripe and orange name/icon coloring still fire normally. The lit-color path for laptop tokens is exercised when a route is active AND a sync FB fires (won't happen pre-SIMPL).
- No console errors.

If any of those fail, stop and fix before committing.

- [ ] **Step 6: Commit all Home.svelte changes (Tasks 3-5)**

```bash
git add MCCCD-AA140/src/pages/Home.svelte
git commit -m "$(cat <<'EOF'
feat(home): tri-state video-sync badge on source cards

Each hero card gets a top-right sync dot:
  green (pulsing) = live source / AirMedia actively sharing
  amber           = AirMedia synced but no one sharing yet
  none            = no sync

SOURCES gains a `key` field; per-card state via a single $derived.
AirMedia sub-label swaps to the active sharing protocol (TX3/AIRPLAY/
MIRACAST). Laptop sub-label becomes a dual HDMI · USBC token, with
.lit on whichever NVX-384 input currently has sync.

Routing is never gated by sync — the dot is informational only.

Spec: docs/superpowers/specs/2026-05-30-source-video-sync-badge-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Deploy and panel-side verify

**Files:** none (deploy only)

- [ ] **Step 1: Production build**

```bash
cd MCCCD-AA140 && npm run build
```

Expected: success with no errors. Confirms the bundled `ch5z` will deploy cleanly.

- [ ] **Step 2: Deploy to both panels**

Per standing memory rule:

```bash
cd MCCCD-AA140 && npm run deploy:both
```

Expected: both TS-1070 (.80) and TSW-1070 (.78) push successfully.

- [ ] **Step 3: Panel-side smoke verification**

On either panel:

- Power on the room from the splash if not already.
- Inspect the 4 Home source cards.
- Expected: **no dots visible** on any card (the 8 SIMPL FBs aren't wired yet — all stores hold their `false` default).
- Sub-labels: `HDMI 1`, `HDMI 2`, `WIRELESS`, and Laptop shows `HDMI` + `USBC` both dimmed gray.
- Tap each card in turn — active-routing orange behavior still works exactly as before.
- No visible layout regression (card sizes, spacing, icons unchanged from current production).

If anything looks off, stop and fix before declaring done. The pre-SIMPL state must be a pure visual no-op vs the current panel UI in the active-routing dimension.

- [ ] **Step 4: Document handoff for SIMPL side**

The panel side is done. Until SIMPL exposes the 8 boolean FBs, the dots will never appear. Confirm to the user that:
- Branch `feat/source-video-sync-badge` holds the panel-side work, ready to merge.
- SIMPL-side work (the 8 FBs + Contract Editor regen + PanelJoins.cs resync) is the next external dependency before the badge does anything visible at runtime.
- See spec § "SIMPL responsibilities" for the wiring list.

No further commit unless a fix was needed in Step 3.

---

## Verification summary

After Task 6 completes, on the branch:
- 3 commits (contract, store, Home.svelte) plus a spec-fix commit = 4 total ahead of `main`.
- `npm run build` clean.
- Both panels showing pre-SIMPL visual baseline (no dots, no regressions).
- Spec's "Pre-SIMPL smoke" verification matrix row satisfied.

Post-SIMPL verification (NOT part of this plan — sits with SIMPL author):
- Use the spec's manual verification matrix (§ "Testing notes") once SIMPL exposes the 8 FBs.
