<script lang="ts">
  import { onMount } from 'svelte';
  import { draggingSource, cloneCoords, SOURCES, registerCloneEl, type SourceId } from '../lib/stores/router';

  // Inline SVG markup for each source — must match SourceRail.svelte exactly.
  const SVGS: Record<SourceId, string> = {
    roomPc:   '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/>',
    extPc:    '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/><circle cx="18" cy="9" r="1.4" fill="currentColor"/>',
    airMedia: '<path d="M5 12a10 10 0 0 1 14 0" stroke-linecap="round"/><path d="M8.5 15.5a5 5 0 0 1 7 0" stroke-linecap="round"/><circle cx="12" cy="19" r="1.3" fill="currentColor"/>',
    laptop:   '<rect x="4" y="5" width="16" height="10" rx="1.5" stroke-linejoin="round"/><path d="M2 19h20" stroke-linejoin="round"/>',
  };

  let cloneEl: HTMLDivElement | undefined = $state();

  onMount(() => {
    if (cloneEl) registerCloneEl(cloneEl);
    return () => registerCloneEl(null);
  });

  // Read the panel's render scale so the clone visually matches the rail chips.
  // The clone is rendered OUTSIDE .panel-stage so it doesn't inherit the scale
  // automatically — we apply it through the transform's scale factor.
  function panelScale(): number {
    const v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--panel-scale'));
    return isFinite(v) && v > 0 ? v : 1;
  }

  // Compute the inline transform from cloneCoords. The translate offsets stay
  // (40, 44) — the un-scaled element-local center — because transform-origin is
  // 50% 50% so the visual center lands on the pointer regardless of scale.
  // Multiplying scale by panel-scale makes the clone visually match the rail
  // chips on a non-1.0× panel (e.g. 1.5× on TS-1070).
  function transformFor(coords: { x: number; y: number }): string {
    const ps = panelScale();
    return `translate(${coords.x - 40}px, ${coords.y - 44}px) scale(${1.08 * ps}) rotate(2deg)`;
  }
</script>

{#if $draggingSource}
  <div
    bind:this={cloneEl}
    class="chip-clone"
    style="transform: {transformFor($cloneCoords)};"
    aria-hidden="true"
  >
    <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8">
      {@html SVGS[$draggingSource]}
    </svg>
    <span class="chip-label">{SOURCES[$draggingSource].label}</span>
  </div>
{/if}

<style>
  .chip-clone {
    position: fixed;
    top: 0;
    left: 0;
    width: 80px;
    min-height: 88px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 10px 6px;
    background: linear-gradient(180deg, rgba(30, 41, 59, 0.95), rgba(30, 41, 59, 0.85));
    border: 1.5px solid var(--color-accent);
    border-radius: var(--radius-button);
    color: var(--color-copy);
    box-shadow:
      0 12px 32px rgba(0, 0, 0, 0.5),
      0 0 24px var(--color-accent-glow);
    pointer-events: none;
    z-index: 1000;
    will-change: transform;
  }

  .chip-clone .chip-ico {
    color: var(--color-accent);
  }

  .chip-clone .chip-label {
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.04em;
  }

  /* Snap transition class — added by router.ts endDrag during drop / snap-back. */
  :global(.chip-clone.snapping) {
    transition: transform 220ms cubic-bezier(0.4, 0, 0.2, 1), opacity 220ms;
  }
</style>
