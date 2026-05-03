<!--
  MasterStrip — master fader + dB readout + D1/D2 output select
  (Plan 4 — Mockup #13).
-->
<script lang="ts">
  interface Props {
    progLevel: number;        // 0..100
    audioOutput: 1 | 2;
    onProgLevelChange: (n: number) => void;
    onOutputSelect: (out: 1 | 2) => void;
  }

  let { progLevel, audioOutput, onProgLevelChange, onOutputSelect }: Props = $props();

  let dbLabel = $derived(
    progLevel <= 0
      ? '-60 dB'
      : `${Math.round(-60 + (Math.max(0, Math.min(100, progLevel)) / 100) * 60)} dB`,
  );
</script>

<div class="master-strip">
  <p class="master-label">Output</p>

  <div class="master-fader">
    <div class="m-rail"></div>
    <div class="m-fill" style="height: {progLevel}%"></div>
    <input
      class="m-input"
      type="range"
      min="0"
      max="100"
      step="1"
      value={progLevel}
      oninput={(e) => onProgLevelChange(+(e.currentTarget as HTMLInputElement).value)}
      aria-label="Master program level"
    />
    <div class="m-thumb" style="bottom: calc({progLevel}% - 9px)"></div>
  </div>

  <p class="master-db">{dbLabel}</p>

  <div class="output-grid">
    <button
      type="button"
      class="out-btn"
      class:active={audioOutput === 1}
      onclick={() => onOutputSelect(1)}
      aria-pressed={audioOutput === 1}
    >D1</button>
    <button
      type="button"
      class="out-btn"
      class:active={audioOutput === 2}
      onclick={() => onOutputSelect(2)}
      aria-pressed={audioOutput === 2}
    >D2</button>
  </div>
</div>

<style>
  .master-strip {
    display: grid;
    grid-template-rows: auto 1fr auto auto;
    gap: 10px;
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 12px;
    padding: 14px 12px;
    min-height: 0;
    margin: 0 4px;
  }
  .master-label {
    margin: 0;
    text-align: center;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .master-fader {
    position: relative;
    width: 32px;
    justify-self: center;
    height: 100%;
    min-height: 140px;
  }
  .m-rail {
    position: absolute;
    left: 50%;
    top: 4px;
    bottom: 4px;
    width: 6px;
    transform: translateX(-50%);
    background: rgba(15, 23, 42, 0.75);
    border: 0.5px solid var(--color-border);
    border-radius: 3px;
  }
  .m-fill {
    position: absolute;
    left: 50%;
    bottom: 4px;
    width: 6px;
    transform: translateX(-50%);
    background: linear-gradient(0deg, var(--color-accent) 0%, #fbbf24 60%, #f8fafc 100%);
    border-radius: 3px;
    transition: height 60ms linear;
    pointer-events: none;
  }
  .m-thumb {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    width: 28px;
    height: 18px;
    background: linear-gradient(180deg, #f8fafc 0%, #cbd5e1 100%);
    border: 0.5px solid rgba(15, 23, 42, 0.6);
    border-radius: 4px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
    pointer-events: none;
    transition: bottom 60ms linear;
  }
  .m-input {
    position: absolute;
    -webkit-appearance: slider-vertical;
    appearance: slider-vertical;
    writing-mode: vertical-lr;
    direction: rtl;
    width: 32px;
    height: 100%;
    margin: 0;
    opacity: 0;
    cursor: pointer;
    z-index: 2;
  }

  .master-db {
    margin: 0;
    text-align: center;
    font-size: 22px;
    font-weight: 900;
    letter-spacing: 0.02em;
    color: var(--color-copy);
    font-variant-numeric: tabular-nums;
  }

  .output-grid {
    display: grid;
    grid-template-rows: 1fr 1fr;
    gap: 6px;
  }
  .out-btn {
    padding: 9px 0;
    border-radius: 8px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 800;
    letter-spacing: 0.12em;
    cursor: pointer;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease;
  }
  .out-btn:hover {
    background: rgba(51, 65, 85, 0.7);
    color: var(--color-copy);
  }
  .out-btn.active {
    background: rgba(245, 166, 35, 0.18);
    border-color: rgba(245, 166, 35, 0.55);
    color: var(--color-accent);
  }

  @media (prefers-reduced-motion: reduce) {
    .m-fill, .m-thumb { transition: none; }
  }
</style>
