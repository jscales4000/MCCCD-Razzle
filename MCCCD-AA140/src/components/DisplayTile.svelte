<script lang="ts">
  import { publishAnalog } from '../lib/CrComLib';

  interface Props {
    label: string;             // "Display 1" | "Display 2" | "Display 3"
    sourceSetSignal: string;   // SIGNALS.display1Source / display2Source / display3Source
    activeSourceFb: number;    // current source from feedback store (1..4, 0 = none)
    powerOn?: boolean;         // display power state (NVX D200 sink-connected)
    audioActive?: boolean;     // optional, only D1/D2
    onAudioToggle?: () => void; // optional, only D1/D2
    onMirrorToD3?: () => void;  // optional, only D1/D2
  }

  let {
    label,
    sourceSetSignal,
    activeSourceFb,
    powerOn = false,
    audioActive = false,
    onAudioToggle,
    onMirrorToD3
  }: Props = $props();

  const sources = [
    { label: 'Room PC',  value: 1 },
    { label: 'Ext PC',   value: 2 },
    { label: 'AirMedia', value: 3 },
    { label: 'Laptop',   value: 4 }, // NVX-384 auto-switch between HDMI + USB-C
  ];

  function selectSource(value: number) {
    publishAnalog(sourceSetSignal, value);
  }

  function activeLabel(): string {
    const s = sources.find(s => s.value === activeSourceFb);
    return s ? s.label : '—';
  }
</script>

<div class="glass-card display-tile" class:audio-active={audioActive}>
  <div class="tile-header">
    <div class="tile-meta">
      <p class="tile-label">
        <span class="power-dot" class:on={powerOn} title={powerOn ? 'Display ON' : 'Display OFF'}></span>
        {label}
      </p>
      <p class="tile-source">{activeLabel()}</p>
    </div>
    <div class="tile-actions">
      {#if onAudioToggle}
        <button
          class="icon-btn"
          class:active={audioActive}
          onclick={onAudioToggle}
          aria-pressed={audioActive}
          aria-label="Route room audio to this display"
          title="Route room audio to this display"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M3 10v4h4l5 4V6L7 10H3z" fill="currentColor"/>
            <path d="M16 8c1.5 1 2.5 2.5 2.5 4S17.5 15 16 16" stroke="currentColor" stroke-width="1.6" fill="none" stroke-linecap="round"/>
          </svg>
        </button>
      {/if}
      {#if onMirrorToD3}
        <button
          class="icon-btn"
          onclick={onMirrorToD3}
          aria-label="Mirror this display to Display 3"
          title="Mirror this display to Display 3"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M7 17L17 7M17 7H10M17 7V14" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      {/if}
    </div>
  </div>
  <div class="source-grid">
    {#each sources as src}
      <button
        class="btn source-btn"
        class:active={activeSourceFb === src.value}
        onclick={() => selectSource(src.value)}
        aria-pressed={activeSourceFb === src.value}
      >{src.label}</button>
    {/each}
  </div>
</div>

<style>
  .display-tile {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    min-height: 0;
    transition: border-color 200ms ease;
  }
  .display-tile.audio-active {
    border-color: rgba(34, 197, 94, 0.4);
  }

  .tile-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 10px;
  }
  .tile-meta { min-width: 0; }
  .tile-label {
    margin: 0 0 4px;
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .power-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: rgba(148, 163, 184, 0.35);
    flex-shrink: 0;
    transition: background 180ms ease, box-shadow 180ms ease;
  }
  .power-dot.on {
    background: var(--color-success);
    box-shadow: 0 0 10px rgba(34, 197, 94, 0.5);
  }
  .tile-source {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--color-accent);
  }

  .tile-actions {
    display: flex;
    gap: 4px;
  }

  .source-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 8px;
    flex: 1;
    min-height: 0;
  }
  .source-btn {
    font-size: 14px;
    font-weight: 600;
    min-height: 64px;
  }

  @media (prefers-reduced-motion: reduce) {
    .display-tile,
    .power-dot {
      transition: none;
    }
  }
</style>
