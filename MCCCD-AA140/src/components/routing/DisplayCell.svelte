<!--
  DisplayCell — single output display in the routing matrix.

  Class names `tile`, inner `tile-slot`, and `data-display={displayId}` are
  MANDATORY: lib/stores/router.ts uses these exact selectors globally for
  drag-drop hover, drop validation, and post-drop animation.
-->

<script lang="ts">
  import SourceIcon from '../../lib/ui/SourceIcon.svelte';
  import {
    armedSource,
    disarm,
    routeSource,
    shouldSuppressClick,
    SOURCES,
    type DisplayId,
    type SourceId,
  } from '../../lib/stores/router';

  interface Props {
    displayId: DisplayId;
    label: string;
    spec: string;
    activeSourceFb: number;
    powerOn: boolean;
    audioActive: boolean;
    onPowerToggle: () => void;
    onAudioToggle: () => void;
    onMirror?: () => void;
  }

  let {
    displayId,
    label,
    spec,
    activeSourceFb,
    powerOn,
    audioActive,
    onPowerToggle,
    onAudioToggle,
    onMirror,
  }: Props = $props();

  // Map analog feedback (1..4) → SourceId
  const VALUE_TO_SOURCE: Record<number, SourceId> = {
    1: 'roomPc',
    2: 'extPc',
    3: 'airMedia',
    4: 'laptop',
  };

  let activeSource = $derived<SourceId | null>(VALUE_TO_SOURCE[activeSourceFb] ?? null);
  let activeName = $derived(activeSource ? SOURCES[activeSource].label : 'No Source');

  function onCellClick(e: MouseEvent) {
    if (shouldSuppressClick()) return;
    if (!$armedSource) return;
    const target = e.target as Element | null;
    // Buttons inside the info row stop propagation themselves; this is a
    // safety net.
    if (target?.closest('button.da-btn')) return;
    const sourceId = $armedSource;
    disarm();
    routeSource(sourceId, displayId);
  }

  function onCellKeyDown(e: KeyboardEvent) {
    if (e.key !== 'Enter' && e.key !== ' ') return;
    if (!$armedSource) return;
    e.preventDefault();
    const sourceId = $armedSource;
    disarm();
    routeSource(sourceId, displayId);
  }

  function stopThen(fn: () => void) {
    return (e: MouseEvent) => {
      e.stopPropagation();
      fn();
    };
  }
</script>

<div
  class="tile disp-cell"
  class:audio-active={audioActive}
  data-display={displayId}
  onclick={onCellClick}
  onkeydown={onCellKeyDown}
  role="button"
  tabindex="0"
  aria-label="{label} — currently {activeName}"
>
  <div class="tile-slot disp-screen" data-routed={activeSource ?? ''}>
    <div class="disp-powered">
      <span class="disp-led" class:off={!powerOn}></span>
      {powerOn ? 'ON' : 'OFF'}
    </div>
    {#if audioActive}
      <div class="disp-audio-tag">♪ AUDIO</div>
    {/if}

    {#if activeSource}
      <span class="disp-content-icon">
        <SourceIcon source={activeSource} size={32} state="active" />
      </span>
      <span class="disp-content-src">{activeName}</span>
    {:else}
      <span class="disp-content-src empty">No Source</span>
    {/if}

    <div class="disp-drop-hint">Drop Source Here</div>
  </div>

  <div class="disp-info">
    <div class="disp-label">
      <span class="disp-num">Display {displayId.replace('d', '')}</span>
      <span class="disp-name">{label}</span>
      <span class="disp-loc">{spec}</span>
    </div>
    <div class="disp-actions">
      <button
        class="da-btn"
        class:pwr-on={powerOn}
        class:pwr-off={!powerOn}
        onclick={stopThen(onPowerToggle)}
        aria-label={powerOn ? 'Turn display off' : 'Turn display on'}
        title="Power"
        type="button"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path d="M12 3v9"/>
          <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
        </svg>
      </button>
      <button
        class="da-btn"
        class:aud-active={audioActive}
        onclick={stopThen(onAudioToggle)}
        aria-pressed={audioActive}
        aria-label="Route audio to this display"
        title="Audio"
        type="button"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor"/>
          <path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"/>
        </svg>
      </button>
      {#if onMirror}
        <button
          class="da-btn"
          onclick={stopThen(onMirror)}
          aria-label="Mirror to Display 3"
          title="Mirror to D3"
          type="button"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <path d="M8 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h3M16 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3M12 3v18"/>
          </svg>
        </button>
      {/if}
    </div>
  </div>
</div>

<style>
  .disp-cell {
    background: rgba(8, 14, 26, 0.6);
    border: 0.5px solid var(--color-border);
    border-radius: 12px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    cursor: pointer;
    position: relative;
    transition: border-color 200ms ease, box-shadow 200ms ease, transform 200ms ease;
  }

  .disp-cell.audio-active {
    border-color: rgba(34, 197, 94, 0.4);
    box-shadow: 0 0 0 1px rgba(34, 197, 94, 0.08);
  }

  /* DROP-HOVERING — set on the inner .tile-slot by router.ts.
     Lift the cell border to accent so the whole cell reads as the target. */
  :global(.disp-cell:has(.tile-slot.drop-hovering)) {
    border-color: rgba(245, 166, 35, 0.6);
    box-shadow: 0 0 0 2px rgba(245, 166, 35, 0.15);
  }

  .disp-screen {
    aspect-ratio: 16 / 9;
    background: rgba(4, 8, 18, 0.8);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    position: relative;
    overflow: hidden;
    /* tile-slot drop semantics inherit from the global rules below */
    border: 1px solid transparent;
    transition: border-color 200ms, background-color 200ms, transform 200ms;
  }

  .disp-screen::before {
    content: '';
    position: absolute;
    inset: 0;
    background: linear-gradient(135deg, rgba(245, 166, 35, 0.03) 0%, transparent 60%);
    pointer-events: none;
  }

  .disp-content-icon {
    color: rgba(245, 166, 35, 0.6);
    display: inline-flex;
  }

  .disp-content-src {
    font-size: 16px;
    font-weight: 800;
    color: var(--color-copy);
    letter-spacing: -0.01em;
  }

  .disp-content-src.empty {
    color: var(--color-copy-muted);
    font-size: 14px;
    font-weight: 500;
    font-style: italic;
  }

  .disp-powered {
    position: absolute;
    top: 8px;
    left: 8px;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .disp-led {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: var(--color-success);
    box-shadow: 0 0 6px rgba(34, 197, 94, 0.7);
  }

  .disp-led.off {
    background: rgba(100, 116, 139, 0.3);
    box-shadow: none;
  }

  .disp-audio-tag {
    position: absolute;
    top: 8px;
    right: 8px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    padding: 2px 6px;
    border-radius: 4px;
    background: rgba(34, 197, 94, 0.15);
    color: #86efac;
  }

  .disp-drop-hint {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 700;
    color: var(--color-accent);
    letter-spacing: 0.06em;
    text-transform: uppercase;
    background: rgba(245, 166, 35, 0.06);
    opacity: 0;
    transition: opacity 150ms ease;
    pointer-events: none;
  }

  /* When router.ts marks the slot as the active drop target, surface the hint */
  :global(.tile-slot.drop-hovering) .disp-drop-hint,
  :global(.disp-cell:has(.tile-slot.drop-hovering)) .disp-drop-hint {
    opacity: 1;
  }

  .disp-info {
    padding: 10px 12px;
    border-top: 0.5px solid var(--color-border);
    display: flex;
    align-items: center;
    gap: 10px;
    background: rgba(8, 14, 26, 0.4);
  }

  .disp-label {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    min-width: 0;
  }

  .disp-num {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .disp-name {
    font-size: 13px;
    font-weight: 700;
    color: var(--color-copy);
  }

  .disp-loc {
    font-size: 10px;
    color: var(--color-copy-muted);
  }

  .disp-actions {
    display: flex;
    gap: 6px;
  }

  .da-btn {
    width: 32px;
    height: 32px;
    border-radius: 7px;
    background: transparent;
    border: 0.5px solid var(--color-border);
    color: var(--color-copy-soft);
    display: grid;
    place-items: center;
    cursor: pointer;
    transition: background 100ms ease, color 100ms ease, border-color 100ms ease;
  }

  .da-btn:hover {
    background: rgba(255, 255, 255, 0.06);
    color: var(--color-copy);
  }

  .da-btn.pwr-on {
    color: var(--color-success);
  }

  .da-btn.pwr-off {
    color: rgba(100, 116, 139, 0.55);
  }

  .da-btn.aud-active {
    color: var(--color-accent);
    background: rgba(245, 166, 35, 0.08);
    border-color: rgba(245, 166, 35, 0.25);
  }

  /* Reuse global drag-drop rules from the existing router.ts contract:
     body.any-armed / .tile-slot.drop-hovering / .tile-slot.drop-noop
     are styled via global rules originally defined in DropZoneTile.svelte.
     Once that file is deleted in this commit, the same rules need a home.
     Replicate the essentials here so the matrix continues to feedback during drag. */
  :global(body.any-armed) .disp-screen {
    border-color: var(--color-accent-soft);
    background-color: var(--color-accent-dim, rgba(245, 166, 35, 0.04));
  }

  :global(.tile-slot.drop-hovering) {
    border-color: var(--color-accent);
    border-style: solid;
    background-color: var(--color-accent-soft, rgba(245, 166, 35, 0.18));
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

  :global(.tile-slot.drop-noop) {
    border-color: rgba(148, 163, 184, 0.3);
    background-color: rgba(148, 163, 184, 0.04);
    border-style: dashed;
  }

  :global(.tile.flash) {
    animation: tile-flash 150ms ease-out;
  }

  @media (prefers-reduced-motion: reduce) {
    .disp-cell,
    .disp-screen,
    .da-btn,
    .disp-drop-hint {
      transition: none;
    }
  }
</style>
