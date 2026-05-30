<!--
  DisplayMarker — a tappable rectangle inside the reflected-ceiling RoomPlan.

  Represents one physical display surface. Visual state is driven by:
    - activeSource: which source is currently routed (null = none)
    - powerOn: per-display power feedback
    - selected: true when this display owns the open SourcePopover

  Click → bubbles displayId up via the onTap prop. Positioning (top/left/
  width/height) is controlled by the parent (RoomPlan) via inline style
  string, so a single marker component can serve all three displays.
-->

<script lang="ts">
  import { SOURCES, type DisplayId, type SourceId } from '../../lib/stores/router';

  interface Props {
    displayId: DisplayId;
    label: string;
    spec: string;
    activeSource: SourceId | null;
    powerOn: boolean;
    selected: boolean;
    /** Inline positioning string, e.g. "top: 2%; left: 14%; width: 22%; height: 38px;" */
    position: string;
    onTap: (displayId: DisplayId, el: HTMLElement) => void;
  }

  let { displayId, label, spec, activeSource, powerOn, selected, position, onTap }: Props = $props();

  let sourceLabel = $derived(activeSource ? SOURCES[activeSource].label : null);

  function handleClick(e: MouseEvent) {
    onTap(displayId, e.currentTarget as HTMLElement);
  }
</script>

<button
  class="marker"
  class:live={!!activeSource && powerOn}
  class:off={!powerOn}
  class:selected
  data-display={displayId}
  style={position}
  onclick={handleClick}
  aria-label="{label} — {sourceLabel ?? 'no source'}"
  aria-expanded={selected}
  type="button"
>
  <span class="m-id">{displayId.toUpperCase()}</span>
  <span class="m-srcline">
    {#if sourceLabel}
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
        {#if activeSource === 'roomPc'}
          <rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>
        {:else if activeSource === 'extPc'}
          <rect x="3" y="4" width="18" height="12" rx="2"/><path d="M3 10h18M8 20h8M12 16v4"/>
        {:else if activeSource === 'airMedia'}
          <path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/>
        {:else}
          <rect x="2" y="4" width="20" height="13" rx="2"/><path d="M2 20h20"/>
        {/if}
      </svg>
      {sourceLabel}
    {:else}
      — No Source
    {/if}
  </span>
</button>

<style>
  .marker {
    position: absolute;
    appearance: none;
    -webkit-appearance: none;
    display: flex;
    align-items: center;
    gap: 9px;
    padding: 0 10px;
    border-radius: 5px;
    background-image: linear-gradient(180deg, rgba(245, 166, 35, 0.18), rgba(245, 166, 35, 0.06));
    border: 1.5px solid rgba(245, 166, 35, 0.5);
    color: #fde047;
    cursor: pointer;
    overflow: hidden;
    transition: transform 140ms ease, box-shadow 140ms ease, border-color 140ms ease, background-image 140ms ease;
    font: inherit;
    text-align: left;
    -webkit-tap-highlight-color: transparent;
  }

  .marker:hover {
    transform: translateY(-1px);
    box-shadow: 0 0 18px rgba(245, 166, 35, 0.35);
  }

  .marker.live {
    background-image: linear-gradient(180deg, rgba(245, 166, 35, 0.35), rgba(245, 166, 35, 0.16));
    box-shadow: 0 0 22px rgba(245, 166, 35, 0.30);
  }

  .marker.off {
    background-image: linear-gradient(180deg, rgba(100, 116, 139, 0.18), rgba(100, 116, 139, 0.06));
    border-color: rgba(148, 163, 184, 0.3);
    color: var(--color-copy-muted);
  }

  .marker.selected {
    border-color: var(--color-accent);
    box-shadow: 0 0 0 2px var(--color-accent), 0 0 28px rgba(245, 166, 35, 0.45);
  }

  .marker:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 3px;
  }

  .m-id {
    flex-shrink: 0;
    font-size: 13px;
    font-weight: 900;
    color: #fde047;
    background: rgba(0, 0, 0, 0.35);
    padding: 2px 7px;
    border-radius: 4px;
    line-height: 1.1;
  }
  .marker.off .m-id {
    color: var(--color-copy-soft);
  }

  .m-srcline {
    display: flex;
    align-items: center;
    gap: 6px;
    flex: 1;
    min-width: 0;
    font-size: 12px;
    font-weight: 800;
    letter-spacing: 0.02em;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }


  @media (prefers-reduced-motion: reduce) {
    .marker { transition: none; }
  }
</style>
