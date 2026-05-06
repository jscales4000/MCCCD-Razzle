<script lang="ts">
  import type { Readable } from 'svelte/store';
  import { publishAnalog, publishDigital } from '../lib/CrComLib';

  interface Props {
    open: boolean;
    name: string;
    volumeFb: Readable<number>;          // 0..100, line-out level
    volumeSetSignal: string;
    muteFb: Readable<boolean>;
    muteSetSignal: string;
    levelFb?: Readable<number>;          // optional live meter 0..100
    connectedFb?: Readable<boolean>;
    onClose: () => void;
  }

  let {
    open,
    name,
    volumeFb,
    volumeSetSignal,
    muteFb,
    muteSetSignal,
    levelFb,
    connectedFb,
    onClose,
  }: Props = $props();

  function setVolume(v: number) { publishAnalog(volumeSetSignal, v); }
  function nudgeVolume(delta: number) {
    let current = 50;
    const unsub = volumeFb.subscribe((v) => { current = v; });
    unsub();
    publishAnalog(volumeSetSignal, Math.max(0, Math.min(100, current + delta)));
  }
  function toggleMute() {
    let current = false;
    const unsub = muteFb.subscribe((m) => { current = m; });
    unsub();
    publishDigital(muteSetSignal, !current);
  }
</script>

{#if open}
  <div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="mic-modal-title">
    <div class="glass-card modal-card">
      <header class="modal-head">
        <div>
          <p class="eyebrow">Microphone</p>
          <h2 id="mic-modal-title">{name}</h2>
        </div>
        {#if connectedFb}
          <p class="mic-status">
            <span class="status-dot" class:connected={$connectedFb}></span>
            {$connectedFb ? 'Connected' : 'No signal'}
          </p>
        {/if}
      </header>

      {#if levelFb}
        <div class="meter">
          <span class="meter-caption">LIVE</span>
          <div class="meter-track">
            <div class="meter-fill" style="width: {Math.max(0, Math.min(100, $levelFb))}%"></div>
          </div>
        </div>
      {/if}

      <div class="vol-row">
        <label class="vol-label" for="mic-vol-{name}">Volume</label>
        <button class="chrome-btn nudge" onclick={() => nudgeVolume(-5)} aria-label="Volume down 5">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M5 12h14" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" fill="none"/>
          </svg>
        </button>
        <input
          id="mic-vol-{name}"
          class="slider"
          type="range" min="0" max="100"
          value={$volumeFb}
          oninput={(e) => setVolume(+(e.currentTarget as HTMLInputElement).value)}
        />
        <button class="chrome-btn nudge" onclick={() => nudgeVolume(5)} aria-label="Volume up 5">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" fill="none"/>
          </svg>
        </button>
        <span class="vol-value">{Math.round($volumeFb)}</span>
      </div>

      <div class="modal-actions">
        <button
          class="btn"
          class:active={$muteFb}
          class:danger={$muteFb}
          onclick={toggleMute}
          aria-pressed={$muteFb}
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"/>
            {#if $muteFb}
              <path d="M16 9l6 6M22 9l-6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
            {:else}
              <path d="M16 9a5 5 0 0 1 0 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
            {/if}
          </svg>
          <span>{$muteFb ? 'Muted — tap to un-mute' : 'Mute'}</span>
        </button>
        <button class="btn primary" onclick={onClose}>Done</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(2, 6, 23, 0.78);
    display: grid;
    place-items: center;
    z-index: 1000;
    backdrop-filter: blur(8px);
    animation: fade-in 140ms ease;
  }
  .modal-card {
    width: 620px;
    max-width: 92%;
    padding: 28px 32px 24px;
    display: flex;
    flex-direction: column;
    gap: 18px;
    animation: lift-in 200ms cubic-bezier(0.2, 0.8, 0.2, 1);
  }
  .modal-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
  }
  .modal-head .eyebrow {
    margin: 0 0 4px;
    color: var(--color-copy-muted);
    font-size: 12px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }
  .modal-head h2 {
    margin: 0;
    font-size: 26px;
    font-weight: 700;
    color: #ffffff;
    letter-spacing: 0.02em;
  }
  .mic-status {
    margin: 4px 0 0;
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .mic-status .status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: rgba(248, 113, 113, 0.7);
    box-shadow: 0 0 8px rgba(248, 113, 113, 0.5);
  }
  .mic-status .status-dot.connected {
    background: var(--color-success);
    box-shadow: 0 0 8px rgba(34, 197, 94, 0.6);
  }

  .meter {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .meter-caption {
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    width: 48px;
  }
  .meter-track {
    flex: 1;
    height: 12px;
    border-radius: var(--radius-button);
    background: rgba(15, 23, 42, 0.6);
    border: 0.5px solid var(--color-border);
    overflow: hidden;
  }
  .meter-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--color-success) 0%, #facc15 70%, var(--color-danger) 95%);
    transition: width 60ms linear;
  }

  .vol-row {
    display: grid;
    grid-template-columns: 80px auto 1fr auto 44px;
    gap: 12px;
    align-items: center;
  }
  .vol-label {
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }
  .nudge {
    min-height: 44px;
    padding: 6px 10px;
  }
  .slider {
    accent-color: var(--color-accent);
    width: 100%;
    height: 32px;
  }
  .vol-value {
    text-align: right;
    color: var(--color-copy);
    font-size: 18px;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
  }

  .modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 4px;
    justify-content: space-between;
  }
  .modal-actions .btn { min-width: 180px; }

  @keyframes fade-in {
    from { opacity: 0; }
    to { opacity: 1; }
  }
  @keyframes lift-in {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0); }
  }

  @media (prefers-reduced-motion: reduce) {
    .modal-backdrop,
    .modal-card,
    .meter-fill { animation: none; transition: none; }
  }
</style>
