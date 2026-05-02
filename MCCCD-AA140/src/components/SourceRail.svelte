<script lang="ts">
  import { armedSource, SOURCES, type SourceId, armChip, disarm, chipPointerDown, shouldSuppressClick } from '../lib/stores/router';

  // Chip metadata — order is render order (top to bottom).
  // SVG markup matches the mockup at mockups/18-drag-drop-router.html.
  const CHIPS: { id: SourceId; svg: string }[] = [
    { id: 'roomPc',   svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/>' },
    { id: 'extPc',    svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/><circle cx="18" cy="9" r="1.4" fill="currentColor"/>' },
    { id: 'airMedia', svg: '<path d="M5 12a10 10 0 0 1 14 0" stroke-linecap="round"/><path d="M8.5 15.5a5 5 0 0 1 7 0" stroke-linecap="round"/><circle cx="12" cy="19" r="1.3" fill="currentColor"/>' },
    { id: 'laptop',   svg: '<rect x="4" y="5" width="16" height="10" rx="1.5" stroke-linejoin="round"/><path d="M2 19h20" stroke-linejoin="round"/>' },
  ];

  function onChipClick(e: MouseEvent, sourceId: SourceId) {
    if (shouldSuppressClick()) return;
    if ($armedSource === sourceId) { disarm(); return; }
    armChip(sourceId);
  }

  function onChipPointerDown(e: PointerEvent, sourceId: SourceId) {
    chipPointerDown(e, e.currentTarget as HTMLElement, sourceId);
  }
</script>

<aside class="rail">
  <p class="rail-title">SOURCES</p>
  <p class="rail-help">long-press or tap to route</p>
  {#each CHIPS as chip (chip.id)}
    <button
      class="chip"
      class:chip-armed={$armedSource === chip.id}
      data-source={chip.id}
      onclick={(e) => onChipClick(e, chip.id)}
      onpointerdown={(e) => onChipPointerDown(e, chip.id)}
      aria-pressed={$armedSource === chip.id}
      aria-label={SOURCES[chip.id].label}
    >
      <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8">
        {@html chip.svg}
      </svg>
      <span class="chip-label">{SOURCES[chip.id].label}</span>
    </button>
  {/each}
</aside>

<style>
  .rail {
    height: 100%;
    background: var(--color-panel-soft, rgba(15, 23, 42, 0.82));
    border: 0.5px solid var(--color-border);
    border-radius: var(--radius-panel);
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 14px 6px 12px;
    gap: 10px;
  }

  .rail-title {
    margin: 0;
    font-size: 10px;
    font-weight: 800;
    letter-spacing: 0.22em;
    color: var(--color-copy-muted);
  }

  .rail-help {
    margin: 0 0 6px;
    font-size: 9px;
    font-weight: 600;
    letter-spacing: 0.04em;
    color: var(--color-copy-muted);
    text-align: center;
    line-height: 1.3;
  }

  .chip {
    width: 80px;
    min-height: 88px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 10px 6px;
    background: linear-gradient(180deg, rgba(30, 41, 59, 0.7), rgba(30, 41, 59, 0.5));
    border: 0.5px solid var(--color-border);
    border-radius: var(--radius-button);
    color: var(--color-copy-soft);
    cursor: pointer;
    transition: border-color 160ms, color 160ms, transform 160ms, box-shadow 160ms;
    /* Required for pointerdown to fire on touch without scroll interception. */
    touch-action: none;
  }

  .chip:hover {
    color: var(--color-copy);
    border-color: var(--color-accent-soft);
  }

  .chip-ico {
    color: var(--color-copy-soft);
    transition: color 160ms;
  }

  .chip:hover .chip-ico {
    color: var(--color-accent);
  }

  .chip-label {
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.04em;
  }

  /* ARMED state — drives the cyan border + pulse from the global keyframe. */
  .chip-armed {
    border-color: var(--color-accent);
    box-shadow: 0 0 0 1.5px var(--color-accent-soft), 0 0 18px var(--color-accent-soft);
    color: var(--color-copy);
    animation: chip-arm-pulse 1.5s ease-in-out infinite;
  }

  .chip-armed .chip-ico {
    color: var(--color-accent);
  }

  /* Ghost state during drag — original chip becomes 30% opacity. */
  :global(.chip.chip-ghost) {
    opacity: 0.3;
    pointer-events: none;
  }
</style>
