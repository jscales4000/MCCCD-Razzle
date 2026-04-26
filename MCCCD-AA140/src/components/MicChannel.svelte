<script lang="ts">
  import type { Readable } from 'svelte/store';
  import { publishAnalog, publishDigital } from '../lib/CrComLib';

  interface Props {
    name: string;                              // "Lavalier" | "Handheld" | "Ceiling 1" | ...
    connectedFb: Readable<boolean>;
    levelFb: Readable<number>;                 // 0..100
    trimFb: Readable<number>;                  // 0..100
    trimSetSignal: string;
    lineOutFb: Readable<number>;
    lineOutSetSignal: string;
    muteFb?: Readable<boolean>;                // omit for Lav/Handheld (mute lives on Home)
    muteSetSignal?: string;
  }

  let {
    name,
    connectedFb,
    levelFb,
    trimFb,
    trimSetSignal,
    lineOutFb,
    lineOutSetSignal,
    muteFb,
    muteSetSignal,
  }: Props = $props();

  function setTrim(v: number) { publishAnalog(trimSetSignal, v); }
  function setLineOut(v: number) { publishAnalog(lineOutSetSignal, v); }
  function toggleMute() {
    if (!muteFb || !muteSetSignal) return;
    let current = false;
    const unsub = muteFb.subscribe((m) => { current = m; });
    unsub();
    publishDigital(muteSetSignal, !current);
  }
</script>

<div class="glass-card mic-channel">
  <div class="mic-header">
    <div>
      <p class="mic-name">{name}</p>
      <p class="mic-status">
        <span class="status-dot" class:connected={$connectedFb}></span>
        {$connectedFb ? 'Connected' : 'No signal'}
      </p>
    </div>
    {#if muteFb && muteSetSignal}
      <button
        class="btn"
        class:active={$muteFb}
        class:danger={$muteFb}
        onclick={toggleMute}
        aria-pressed={$muteFb}
      >
        {$muteFb ? 'Muted' : 'Mute'}
      </button>
    {/if}
  </div>

  <div class="meter">
    <div class="meter-track">
      <div class="meter-fill" style="width: {Math.max(0, Math.min(100, $levelFb))}%"></div>
    </div>
    <span class="meter-value">{Math.round($levelFb)}</span>
  </div>

  <div class="trim-row">
    <label class="trim-label" for="trim-{name}">Mic Trim</label>
    <input
      id="trim-{name}"
      class="slider"
      type="range" min="0" max="100"
      value={$trimFb}
      oninput={(e) => setTrim(+(e.currentTarget as HTMLInputElement).value)}
    />
    <span class="trim-value">{Math.round($trimFb)}</span>
  </div>

  <div class="trim-row">
    <label class="trim-label" for="lineout-{name}">Line Out</label>
    <input
      id="lineout-{name}"
      class="slider"
      type="range" min="0" max="100"
      value={$lineOutFb}
      oninput={(e) => setLineOut(+(e.currentTarget as HTMLInputElement).value)}
    />
    <span class="trim-value">{Math.round($lineOutFb)}</span>
  </div>
</div>

<style>
  .mic-channel {
    padding: 18px 20px;
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .mic-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
  }
  .mic-name {
    margin: 0 0 4px;
    font-size: 18px;
    font-weight: 700;
    color: var(--color-copy);
  }
  .mic-status {
    margin: 0;
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
  .meter-track {
    flex: 1;
    height: 14px;
    border-radius: var(--radius-button);
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid var(--color-border);
    overflow: hidden;
  }
  .meter-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--color-success) 0%, #facc15 70%, var(--color-danger) 95%);
    transition: width 60ms linear;
  }
  .meter-value {
    width: 36px;
    text-align: right;
    color: var(--color-copy-soft);
    font-size: 12px;
    font-variant-numeric: tabular-nums;
  }

  .trim-row {
    display: grid;
    grid-template-columns: 96px 1fr 36px;
    gap: 12px;
    align-items: center;
  }
  .trim-label {
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
  }
  .slider {
    accent-color: var(--color-accent);
    width: 100%;
  }
  .trim-value {
    text-align: right;
    color: var(--color-copy-soft);
    font-size: 13px;
    font-variant-numeric: tabular-nums;
  }

  @media (prefers-reduced-motion: reduce) {
    .meter-fill { transition: none; }
  }
</style>
