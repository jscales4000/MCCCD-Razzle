<script lang="ts">
  import { armedSource, disarm, routeSource, shouldSuppressClick, type SourceId, type DisplayId } from '../lib/stores/router';

  interface Props {
    label: string;             // "Display 1" | "Display 2" | "Display 3"
    displayId: DisplayId;      // 'd1' | 'd2' | 'd3'
    activeSourceFb: number;    // current source from feedback store (1..4, 0 = none)
    powerOn?: boolean;         // display power state (NVX D200 sink-connected)
    audioActive?: boolean;     // optional, only D1/D2
    onAudioToggle?: () => void; // optional, only D1/D2
    onMirrorToD3?: () => void;  // optional, only D1/D2
  }

  let {
    label,
    displayId,
    activeSourceFb,
    powerOn = false,
    audioActive = false,
    onAudioToggle,
    onMirrorToD3
  }: Props = $props();

  // Map analog feedback (1..4) → SourceId + label + inline SVG markup for the
  // landed chip. Mirrors SourceRail.svelte's CHIPS array.
  const LANDED: Record<number, { id: SourceId; label: string; svg: string }> = {
    1: {
      id: 'roomPc',
      label: 'Room PC',
      svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/>',
    },
    2: {
      id: 'extPc',
      label: 'Ext PC',
      svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/><circle cx="18" cy="9" r="1.4" fill="currentColor"/>',
    },
    3: {
      id: 'airMedia',
      label: 'AirMedia',
      svg: '<path d="M5 12a10 10 0 0 1 14 0" stroke-linecap="round"/><path d="M8.5 15.5a5 5 0 0 1 7 0" stroke-linecap="round"/><circle cx="12" cy="19" r="1.3" fill="currentColor"/>',
    },
    4: {
      id: 'laptop',
      label: 'Laptop',
      svg: '<rect x="4" y="5" width="16" height="10" rx="1.5" stroke-linejoin="round"/><path d="M2 19h20" stroke-linejoin="round"/>',
    },
  };

  function onTileClick(e: MouseEvent) {
    if (shouldSuppressClick()) return;
    if (!$armedSource) return;
    // Don't route if the click target is the audio or mirror icon button — those
    // have their own handlers.
    const target = e.target as Element | null;
    if (target?.closest('.icon-btn')) return;
    const sourceId = $armedSource;
    disarm();
    routeSource(sourceId, displayId);
  }
</script>

<div
  class="glass-card display-tile tile"
  class:audio-active={audioActive}
  data-display={displayId}
  onclick={onTileClick}
  role="button"
  tabindex="0"
>
  <div class="tile-header">
    <div class="tile-meta">
      <p class="tile-label">
        <span class="power-dot" class:on={powerOn} title={powerOn ? 'Display ON' : 'Display OFF'}></span>
        {label}
      </p>
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

  <div class="tile-slot" data-routed={LANDED[activeSourceFb]?.id ?? ''}>
    {#if LANDED[activeSourceFb]}
      {@const meta = LANDED[activeSourceFb]}
      <div class="landed-chip">
        <svg class="chip-ico" viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.8">
          {@html meta.svg}
        </svg>
        <span class="landed-label">{meta.label}</span>
      </div>
    {:else}
      <span class="slot-empty">— No source —</span>
    {/if}
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
    cursor: pointer;
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
    margin: 0;
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

  .tile-actions {
    display: flex;
    gap: 4px;
  }

  /* Drop-zone slot — replaces the old 4-button source-grid. */
  .tile-slot {
    flex: 1;
    border-radius: var(--radius-button);
    border: 1px dashed transparent;
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 200px;
    position: relative;
    transition: border-color 200ms, background-color 200ms, transform 200ms;
  }

  .slot-empty {
    font-size: 13px;
    color: var(--color-copy-muted);
    font-style: italic;
  }

  .landed-chip {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 18px 22px;
    background: rgba(30, 41, 59, 0.5);
    border: 0.5px solid var(--color-border);
    border-radius: var(--radius-button);
    color: var(--color-copy);
    transition: transform 200ms;
  }

  .landed-chip .chip-ico {
    color: var(--color-accent);
  }

  .landed-label {
    font-size: 14px;
    font-weight: 700;
  }

  /* DROP-VALID — body.any-armed triggers all tile-slots to show the dashed cyan tint. */
  :global(body.any-armed) .tile-slot {
    border-color: var(--color-accent-soft);
    background-color: rgba(56, 189, 248, 0.10);
  }

  /* DROP-HOVERING — solid cyan + 1.02 scale; class added imperatively by router.ts. */
  :global(.tile-slot.drop-hovering) {
    border-color: var(--color-accent);
    border-style: solid;
    background-color: var(--color-accent-soft);
    transform: scale(1.02);
  }
  :global(.tile-slot.drop-hovering)::after {
    content: attr(data-hover-hint);
    position: absolute;
    bottom: 12px;
    left: 50%;
    transform: translateX(-50%);
    font-size: 11px;
    font-weight: 700;
    color: var(--color-accent);
    letter-spacing: 0.04em;
    white-space: nowrap;
  }

  /* DROP-NOOP — already-routed source; lower-emphasis. */
  :global(.tile-slot.drop-noop) {
    border-color: rgba(148, 163, 184, 0.3);
    background-color: rgba(148, 163, 184, 0.04);
    border-style: dashed;
  }

  /* tile-flash animation (border + ring); class toggled by router.ts on drop. */
  :global(.tile.flash) {
    animation: tile-flash 150ms ease-out;
  }

  /* thunk on the landed chip after drop. */
  :global(.landed-chip.thunk) {
    animation: thunk 100ms ease-out;
  }

  @media (prefers-reduced-motion: reduce) {
    .display-tile,
    .power-dot {
      transition: none;
    }
  }
</style>
