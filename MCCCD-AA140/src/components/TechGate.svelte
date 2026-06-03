<!--
  TechGate — global view-role affordance, mounted once at the App root.

  • Invisible top-right corner hotspot: press-and-hold ~2s opens the PIN modal.
  • While in Technician view: a small "TECH" badge with an Exit button (top-left).
  • A capture-phase pointerdown listener resets the inactivity timer (bumpActivity)
    so tech view only auto-reverts when the panel is genuinely idle.

  Role state lives in stores/role.ts. This component renders no normal-flow layout
  and is harmless in User view (just the invisible hotspot).
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { role, exitTech, bumpActivity } from '../lib/stores/role';
  import PinModal from './PinModal.svelte';

  const HOLD_MS = 2000;
  let showPin = $state(false);
  let holdTimer: ReturnType<typeof setTimeout> | undefined;

  function startHold() {
    cancelHold();
    holdTimer = setTimeout(() => { showPin = true; }, HOLD_MS);
  }
  function cancelHold() {
    if (holdTimer) { clearTimeout(holdTimer); holdTimer = undefined; }
  }

  onMount(() => {
    // Capture phase so it sees every interaction regardless of stopPropagation.
    window.addEventListener('pointerdown', bumpActivity, true);
    return () => window.removeEventListener('pointerdown', bumpActivity, true);
  });
</script>

<!-- Hidden long-press hotspot (always present; cheap, invisible) -->
<button
  class="tech-hotspot"
  type="button"
  aria-label="Technician access (press and hold)"
  onpointerdown={startHold}
  onpointerup={cancelHold}
  onpointerleave={cancelHold}
  onpointercancel={cancelHold}
></button>

{#if $role === 'tech'}
  <div class="tech-badge" role="status">
    <span class="tb-dot" aria-hidden="true"></span>
    <span class="tb-label">Tech</span>
    <button type="button" class="tb-exit" onclick={exitTech}>Exit</button>
  </div>
{/if}

{#if showPin}
  <PinModal onClose={() => (showPin = false)} />
{/if}

<style>
  .tech-hotspot {
    position: fixed;
    top: 0;
    right: 0;
    width: 64px;
    height: 64px;
    padding: 0;
    margin: 0;
    border: none;
    background: transparent;
    cursor: default;
    z-index: 900;
    -webkit-tap-highlight-color: transparent;
  }

  .tech-badge {
    position: fixed;
    top: 10px;
    left: 10px;
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 6px 8px 6px 12px;
    border-radius: 999px;
    background: rgba(245, 166, 35, 0.14);
    border: 0.5px solid var(--color-accent-soft, rgba(245, 166, 35, 0.4));
    color: var(--color-accent, #f5a623);
    z-index: 950;
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.4);
  }
  .tb-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 6px currentColor;
  }
  .tb-label {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.12em;
    text-transform: uppercase;
  }
  .tb-exit {
    margin-left: 2px;
    padding: 4px 10px;
    border-radius: 999px;
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.25));
    background: rgba(15, 23, 42, 0.6);
    color: var(--color-copy, #e2e8f0);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    cursor: pointer;
    font-family: inherit;
  }
  .tb-exit:hover { background: rgba(30, 41, 59, 0.85); }
</style>
