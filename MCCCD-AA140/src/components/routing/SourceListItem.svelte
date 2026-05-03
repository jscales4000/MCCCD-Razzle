<!--
  SourceListItem — single row in the Display Routing source list.

  Class names `chip` and `data-source={sourceId}` are MANDATORY: the
  drag-drop store at lib/stores/router.ts selects elements by these
  exact names. See router.ts → endDrag()'s `.chip[data-source="..."]`
  and chipPointerDown's element capture.
-->

<script lang="ts">
  import SourceIcon from '../../lib/ui/SourceIcon.svelte';
  import type { SourceId } from '../../lib/stores/router';

  interface Props {
    sourceId: SourceId;
    name: string;
    subLabel: string;
    routedTo: string[];
    selected: boolean;
    onPointerDown: (e: PointerEvent, el: HTMLElement) => void;
    onClick: (e: MouseEvent) => void;
  }

  let {
    sourceId,
    name,
    subLabel,
    routedTo,
    selected,
    onPointerDown,
    onClick,
  }: Props = $props();

  function handlePointerDown(e: PointerEvent) {
    onPointerDown(e, e.currentTarget as HTMLElement);
  }
</script>

<button
  class="chip src-item"
  class:selected
  data-source={sourceId}
  onclick={onClick}
  onpointerdown={handlePointerDown}
  aria-pressed={selected}
  aria-label={name}
  type="button"
>
  <span class="src-icon">
    <SourceIcon source={sourceId} size={20} state={selected ? 'active' : 'default'} />
  </span>
  <span class="src-info">
    <span class="src-name">{name}</span>
    <span class="src-sub">{subLabel}</span>
  </span>
  {#if routedTo.length > 0}
    <span class="src-routed">{routedTo.map(d => d.toUpperCase()).join(' ')}</span>
  {/if}
</button>

<style>
  .src-item {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px;
    margin: 0 0 4px;
    border-radius: 10px;
    border: 0.5px solid transparent;
    background: transparent;
    color: var(--color-copy);
    cursor: pointer;
    text-align: left;
    transition: background 110ms ease, border-color 110ms ease, color 110ms ease;
    /* Required: pointerdown must fire before scroll on touch panels. */
    touch-action: none;
  }

  .src-item:hover {
    background: rgba(255, 255, 255, 0.04);
    border-color: var(--color-border);
  }

  .src-item.selected {
    background: rgba(245, 166, 35, 0.10);
    border-color: rgba(245, 166, 35, 0.35);
  }

  .src-icon {
    width: 40px;
    height: 40px;
    border-radius: 9px;
    background: rgba(30, 41, 59, 0.6);
    border: 0.5px solid var(--color-border);
    display: grid;
    place-items: center;
    color: var(--color-copy-soft);
    flex-shrink: 0;
    transition: background 110ms ease, border-color 110ms ease, color 110ms ease;
  }

  .src-item.selected .src-icon {
    background: rgba(245, 166, 35, 0.12);
    border-color: rgba(245, 166, 35, 0.30);
    color: var(--color-accent);
  }

  .src-info {
    display: flex;
    flex-direction: column;
    min-width: 0;
    flex: 1;
  }

  .src-name {
    font-size: 14px;
    font-weight: 700;
    letter-spacing: -0.01em;
    color: var(--color-copy);
  }

  .src-sub {
    font-size: 11px;
    color: var(--color-copy-muted);
    margin-top: 2px;
  }

  .src-routed {
    margin-left: auto;
    font-size: 10px;
    font-weight: 700;
    padding: 3px 7px;
    border-radius: 5px;
    background: rgba(245, 166, 35, 0.10);
    border: 0.5px solid rgba(245, 166, 35, 0.25);
    color: var(--color-accent);
    white-space: nowrap;
    flex-shrink: 0;
    letter-spacing: 0.04em;
  }

  /* Ghost state during drag — original chip becomes 30% opacity. */
  :global(.chip.chip-ghost) {
    opacity: 0.3;
    pointer-events: none;
  }

  @media (prefers-reduced-motion: reduce) {
    .src-item,
    .src-icon {
      transition: none;
    }
  }
</style>
