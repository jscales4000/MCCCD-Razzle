<!--
  MixerChannel — broadcast-style vertical channel strip (Plan 4 — Mockup #13).

  Layout (top-to-bottom):
    .ch-head — type / name / connection dot+label / model
    .ch-body (1fr)
      .fader-wrap row: VuMeter | vertical fader | VuMeter
      .trim-row:  label | range | readout
    .ch-foot — mute button + Peak readout

  When `connected === false`, fader/trim/mute are dimmed and disabled.
  When `muted`, mute button reads "MUTED" with red styling; otherwise "LIVE"
  (or "OFFLINE" when not connected). Peak shows `-60..0` dB derived from level.
-->
<script lang="ts">
  import VuMeter from '../../lib/ui/VuMeter.svelte';

  interface Props {
    type: string;
    name: string;
    model: string;
    connected: boolean;
    level: number;        // 0..100, drives stereo VU bars + Peak readout
    lineOut: number;      // 0..100, drives fader fill + thumb
    trim: number;         // -20..+20 dB
    muted: boolean;
    advanced?: boolean;   // false = User view: hide line-out fader + trim (keep meter + mute)
    onLineOutChange: (n: number) => void;
    onTrimChange: (n: number) => void;
    onMuteToggle: () => void;
  }

  let {
    type,
    name,
    model,
    connected,
    level,
    lineOut,
    trim,
    muted,
    advanced = true,
    onLineOutChange,
    onTrimChange,
    onMuteToggle,
  }: Props = $props();

  let muteLabel = $derived(!connected ? 'OFFLINE' : muted ? 'MUTED' : 'LIVE');
  // Peak in dB. level=0 → -60, level=100 → 0. When offline, show em-dash.
  let peakLabel = $derived(connected ? `${Math.round(-60 + (Math.max(0, Math.min(100, level)) / 100) * 60)} dB` : '—');
</script>

<div class="mixer-channel">
  <!-- Head -->
  <div class="ch-head">
    <p class="ch-type">{type}</p>
    <p class="ch-name">{name}</p>
    <p class="ch-conn">
      <span class="conn-dot" class:on={connected}></span>
      {connected ? 'Connected' : 'No Signal'}
    </p>
    <p class="ch-model">{model}</p>
  </div>

  <!-- Body -->
  <div class="ch-body">
    <div class="fader-wrap" class:disabled={!connected}>
      <div class="vu-slot"><VuMeter level={connected ? level : 0} /></div>

      {#if advanced}
        <!-- Line-out (output feed) fader — Technician only -->
        <div class="fader">
          <div class="fader-rail"></div>
          <div class="fader-fill" style="height: {lineOut}%"></div>
          <input
            class="fader-input"
            type="range"
            min="0"
            max="100"
            step="1"
            value={lineOut}
            disabled={!connected}
            oninput={(e) => onLineOutChange(+(e.currentTarget as HTMLInputElement).value)}
            aria-label="{name} line out level"
          />
          <div class="fader-thumb" style="bottom: calc({lineOut}% - 8px)"></div>
        </div>
      {:else}
        <div class="fader-spacer" aria-hidden="true"></div>
      {/if}

      <div class="vu-slot"><VuMeter level={connected ? level : 0} /></div>
    </div>

    {#if advanced}
      <!-- Input trim/gain — Technician only -->
      <div class="trim-row" class:disabled={!connected}>
        <span class="trim-label">Trim</span>
        <input
          class="trim-slider"
          type="range"
          min="-20"
          max="20"
          step="1"
          value={trim}
          disabled={!connected}
          oninput={(e) => onTrimChange(+(e.currentTarget as HTMLInputElement).value)}
          aria-label="{name} trim"
        />
        <span class="trim-readout">{trim > 0 ? '+' : ''}{trim}</span>
      </div>
    {/if}
  </div>

  <!-- Foot -->
  <div class="ch-foot">
    <button
      type="button"
      class="mute-btn"
      class:muted
      class:offline={!connected}
      onclick={onMuteToggle}
      disabled={!connected}
      aria-pressed={muted}
    >
      {muteLabel}
    </button>
    <p class="peak">Peak <span class="peak-val">{peakLabel}</span></p>
  </div>
</div>

<style>
  .mixer-channel {
    display: grid;
    grid-template-rows: auto 1fr auto;
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 12px;
    overflow: hidden;
    min-height: 0;
    min-width: 0;
    margin: 0 4px;
  }

  /* ── Head ───────────────────────────────────────────────────────── */
  .ch-head {
    padding: 10px 12px 8px;
    border-bottom: 0.5px solid var(--color-border);
    background: rgba(8, 14, 26, 0.4);
    text-align: center;
    min-height: 80px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 2px;
  }
  .ch-type {
    margin: 0;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .ch-name {
    margin: 0;
    font-size: 14px;
    font-weight: 800;
    color: var(--color-copy);
    letter-spacing: 0.02em;
  }
  .ch-conn {
    margin: 2px 0 0;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .conn-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: rgba(248, 113, 113, 0.7);
    box-shadow: 0 0 6px rgba(248, 113, 113, 0.5);
  }
  .conn-dot.on {
    background: var(--color-success);
    box-shadow: 0 0 6px rgba(34, 197, 94, 0.6);
  }
  .ch-model {
    margin: 0;
    font-size: 10px;
    font-weight: 600;
    color: var(--color-copy-soft);
    opacity: 0.7;
  }

  /* ── Body ───────────────────────────────────────────────────────── */
  .ch-body {
    display: grid;
    grid-template-rows: 1fr auto;
    padding: 14px 10px 10px;
    gap: 12px;
    min-height: 0;
  }

  .fader-wrap {
    display: grid;
    grid-template-columns: 16px 1fr 16px;
    gap: 10px;
    align-items: stretch;
    justify-items: center;
    min-height: 0;
  }
  .fader-wrap.disabled {
    opacity: 0.35;
    cursor: not-allowed;
    pointer-events: none;
  }

  .vu-slot {
    width: 16px;
    height: 100%;
    min-height: 120px;
  }

  .fader {
    position: relative;
    width: 28px;
    height: 100%;
    min-height: 120px;
  }
  /* User view: no line-out fader — keep the column so the VU meters stay flanked. */
  .fader-spacer {
    width: 28px;
    height: 100%;
    min-height: 120px;
  }
  .fader-rail {
    position: absolute;
    left: 50%;
    top: 4px;
    bottom: 4px;
    width: 4px;
    transform: translateX(-50%);
    background: rgba(15, 23, 42, 0.7);
    border: 0.5px solid var(--color-border);
    border-radius: 2px;
  }
  .fader-fill {
    position: absolute;
    left: 50%;
    bottom: 4px;
    width: 4px;
    transform: translateX(-50%);
    background: linear-gradient(0deg, var(--color-accent) 0%, var(--color-accent-soft, #fbbf24) 100%);
    border-radius: 2px;
    transition: height 60ms linear;
    pointer-events: none;
  }
  .fader-thumb {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    width: 24px;
    height: 16px;
    background: linear-gradient(180deg, #f8fafc 0%, #cbd5e1 100%);
    border: 0.5px solid rgba(15, 23, 42, 0.6);
    border-radius: 4px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
    pointer-events: none;
    transition: bottom 60ms linear;
  }
  /* Hidden but accessible range input — the visible track/thumb above
     are decorative; the input handles touch + a11y. */
  .fader-input {
    position: absolute;
    -webkit-appearance: slider-vertical;
    appearance: slider-vertical;
    writing-mode: vertical-lr;
    direction: rtl;
    width: 28px;
    height: 100%;
    margin: 0;
    opacity: 0;
    cursor: pointer;
    z-index: 2;
  }

  .trim-row {
    display: grid;
    grid-template-columns: auto 1fr auto;
    gap: 8px;
    align-items: center;
  }
  .trim-row.disabled {
    opacity: 0.35;
    cursor: not-allowed;
    pointer-events: none;
  }
  .trim-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .trim-slider {
    width: 100%;
    accent-color: var(--color-accent);
  }
  .trim-readout {
    font-size: 11px;
    font-variant-numeric: tabular-nums;
    color: var(--color-copy-soft);
    min-width: 28px;
    text-align: right;
  }

  /* ── Foot ───────────────────────────────────────────────────────── */
  .ch-foot {
    border-top: 0.5px solid var(--color-border);
    background: rgba(8, 14, 26, 0.3);
    padding: 10px 10px 10px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    align-items: stretch;
    min-height: 70px;
  }
  .mute-btn {
    width: 100%;
    padding: 8px 0;
    border-radius: 8px;
    border: 0.5px solid rgba(34, 197, 94, 0.3);
    background: rgba(34, 197, 94, 0.10);
    color: #86efac;
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.16em;
    cursor: pointer;
    transition: background 110ms ease, border-color 110ms ease, color 110ms ease;
  }
  .mute-btn:hover {
    background: rgba(34, 197, 94, 0.18);
  }
  .mute-btn.muted {
    background: rgba(239, 68, 68, 0.16);
    border-color: rgba(239, 68, 68, 0.45);
    color: #fca5a5;
  }
  .mute-btn.muted:hover {
    background: rgba(239, 68, 68, 0.26);
  }
  .mute-btn.offline {
    background: rgba(100, 116, 139, 0.10);
    border-color: rgba(100, 116, 139, 0.25);
    color: var(--color-copy-muted);
    cursor: not-allowed;
  }
  .peak {
    margin: 0;
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
    text-align: center;
  }
  .peak-val {
    color: var(--color-copy-soft);
    font-variant-numeric: tabular-nums;
    margin-left: 4px;
  }

  @media (prefers-reduced-motion: reduce) {
    .fader-fill, .fader-thumb { transition: none; }
  }
</style>
