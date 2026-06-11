<!--
  ScreenControl — Up / Down for the two motorized projector screens.

  Both screens move together (RMC4 has only two relays: one UP, one DOWN, each
  paralleled across both screens). The processor pulses the relay momentarily and
  the screen runs to its limit, so these are stateless momentary commands — there
  is no position feedback. The brief active highlight below is client-side only
  (last direction tapped), purely for tap confirmation.
-->
<script lang="ts">
  import { SIGNALS } from '../lib/contract';
  import { pulseDigital } from '../lib/CrComLib';

  let lastDir = $state<'up' | 'down' | null>(null);
  let clearTimer: ReturnType<typeof setTimeout> | undefined;

  function flash(dir: 'up' | 'down') {
    lastDir = dir;
    if (clearTimer) clearTimeout(clearTimer);
    clearTimer = setTimeout(() => (lastDir = null), 1500);
  }

  function raise() { pulseDigital(SIGNALS.screenUp); flash('up'); }
  function lower() { pulseDigital(SIGNALS.screenDown); flash('down'); }
</script>

<div class="side-screens">
  <span class="side-h">Projector Screens</span>
  <div class="screen-row">
    <button
      type="button"
      class="screen-btn"
      class:active={lastDir === 'up'}
      onclick={raise}
      aria-label="Raise projector screens"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
        <path d="M12 19V5M5 12l7-7 7 7"/>
      </svg>
      Up
    </button>
    <button
      type="button"
      class="screen-btn"
      class:active={lastDir === 'down'}
      onclick={lower}
      aria-label="Lower projector screens"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
        <path d="M12 5v14M5 12l7 7 7-7"/>
      </svg>
      Down
    </button>
  </div>
  <div class="aud-hint">Both screens move together. Up/Down trigger a momentary pulse; the screen runs to its limit.</div>
</div>

<style>
  .side-screens {
    border-top: 0.5px solid var(--color-border);
    padding-top: 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .side-h {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .screen-row {
    display: flex;
    gap: 6px;
  }

  .screen-btn {
    flex: 1 1 0;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 7px;
    padding: 12px 10px;
    border-radius: 8px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    cursor: pointer;
    font-family: inherit;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease, transform 80ms ease;
  }
  .screen-btn:hover {
    background: rgba(51, 65, 85, 0.7);
    color: var(--color-copy);
  }
  .screen-btn:active { transform: scale(0.97); }
  .screen-btn.active {
    background: rgba(56, 189, 248, 0.14);
    border-color: var(--color-accent-soft);
    color: var(--color-accent);
  }

  .aud-hint {
    font-size: 10px;
    color: var(--color-copy-muted);
    line-height: 1.4;
  }

  @media (prefers-reduced-motion: reduce) {
    .screen-btn { transition: none; }
  }
</style>
