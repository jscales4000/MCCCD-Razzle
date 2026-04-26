<script lang="ts">
  import { publishAnalog } from '../lib/CrComLib';
  import { nvxAutoSwitchSrc } from '../lib/stores/signals';

  interface Props {
    label: string;             // "Display 1" | "Display 2" | "Display 3"
    sourceSetSignal: string;   // SIGNALS.display1Source / display2Source / display3Source
    activeSourceFb: number;    // current source from feedback store (1..5, 0 = none)
    audioActive?: boolean;     // optional, only D1/D2
    onAudioToggle?: () => void; // optional, only D1/D2
    onMirrorToD3?: () => void;  // optional, only D1/D2
  }

  let {
    label,
    sourceSetSignal,
    activeSourceFb,
    audioActive = false,
    onAudioToggle,
    onMirrorToD3
  }: Props = $props();

  const sources = [
    { label: 'Room PC', value: 1 },
    { label: 'Ext PC', value: 2 },
    { label: 'AirMedia', value: 3 },
    { label: 'HDMI', value: 4 },
    { label: 'USB-C', value: 5 },
  ];

  function selectSource(value: number) {
    publishAnalog(sourceSetSignal, value);
  }

  function activeLabel(): string {
    const s = sources.find(s => s.value === activeSourceFb);
    return s ? s.label : '—';
  }

  // Show NVX-384 auto-switch badge when user picked HDMI but USB-C is active (or vice versa)
  function autoSwitchBadge(): string | null {
    if (activeSourceFb !== 4 && activeSourceFb !== 5) return null;
    const actual = $nvxAutoSwitchSrc; // 1=HDMI, 2=USB-C
    if (activeSourceFb === 4 && actual === 2) return 'USB-C active';
    if (activeSourceFb === 5 && actual === 1) return 'HDMI active';
    return null;
  }
</script>

<div class="glass-card display-tile" class:audio-active={audioActive}>
  <div class="tile-header">
    <div class="tile-meta">
      <p class="tile-label">{label}</p>
      <p class="tile-source">{activeLabel()}</p>
      {#if autoSwitchBadge()}
        <span class="autoswitch-badge">↳ {autoSwitchBadge()}</span>
      {/if}
    </div>
    <div class="tile-actions">
      {#if onAudioToggle}
        <button
          class="btn icon-btn"
          class:active={audioActive}
          onclick={onAudioToggle}
          aria-pressed={audioActive}
          title="Route room audio to this display"
        >🔊</button>
      {/if}
      {#if onMirrorToD3}
        <button
          class="btn icon-btn mirror-btn"
          onclick={onMirrorToD3}
          title="Mirror this display to Display 3"
        >↗</button>
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
    border-radius: var(--radius-panel);
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
  .tile-label {
    margin: 0 0 4px;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .tile-source {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--color-accent);
  }
  .autoswitch-badge {
    display: inline-block;
    margin-top: 4px;
    font-size: 11px;
    font-weight: 600;
    padding: 2px 8px;
    border-radius: 999px;
    background: rgba(56, 189, 248, 0.15);
    border: 1px solid rgba(56, 189, 248, 0.3);
    color: #bae6fd;
  }
  .tile-actions {
    display: flex;
    gap: 6px;
  }
  .icon-btn {
    width: 40px;
    height: 40px;
    font-size: 18px;
    border-radius: 10px;
  }
  .icon-btn.active {
    background: var(--color-accent);
    color: #0b1220;
  }
  .mirror-btn {
    color: #fbbf24;
    border-color: rgba(251, 191, 36, 0.3);
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
    min-height: 56px;
  }
</style>
