<!--
  SourcePopover — inline source picker anchored to a DisplayMarker.

  Position is controlled by the parent via the `anchorRect` prop (the
  DOMRect of the tapped marker, in *plan-container* coordinates). The
  popover places itself just below the marker by default, or above
  if the marker sits in the bottom half of the plan (D3 case).

  Tap-outside is handled by the parent — this component only renders its
  own surface and emits selection events.
-->

<script lang="ts">
  import { SOURCES, type DisplayId, type SourceId } from '../../lib/stores/router';

  interface AnchorRect {
    /** marker top relative to the plan container, in px */
    top: number;
    /** marker left in px */
    left: number;
    width: number;
    height: number;
    /** plan container height, used to decide flip-above */
    containerHeight: number;
    /** plan container width, used to keep popover on-screen */
    containerWidth: number;
  }

  interface Props {
    displayId: DisplayId;
    displayLabel: string;
    activeSource: SourceId | null;
    anchor: AnchorRect;
    canMirrorD1?: boolean;  // hide "Match D1" on D1 itself
    onSelectSource: (sourceId: SourceId) => void;
    onMirrorD1: () => void;
    onClear: () => void;
    onClose: () => void;
  }

  let {
    displayId,
    displayLabel,
    activeSource,
    anchor,
    canMirrorD1 = true,
    onSelectSource,
    onMirrorD1,
    onClear,
    onClose,
  }: Props = $props();

  const POPOVER_WIDTH = 240;
  const POPOVER_HEIGHT_APPROX = 280;
  const GAP = 10;

  // Flip above when the marker is in the bottom half of the plan.
  let flipAbove = $derived(anchor.top + anchor.height + GAP + POPOVER_HEIGHT_APPROX > anchor.containerHeight);

  // Horizontal: try to center on the marker, but clamp to [8, containerWidth - 8 - width].
  let leftPx = $derived.by(() => {
    const ideal = anchor.left + anchor.width / 2 - POPOVER_WIDTH / 2;
    const min = 8;
    const max = anchor.containerWidth - POPOVER_WIDTH - 8;
    return Math.max(min, Math.min(max, ideal));
  });

  let topPx = $derived(
    flipAbove
      ? anchor.top - POPOVER_HEIGHT_APPROX - GAP
      : anchor.top + anchor.height + GAP
  );

  // Arrow x position (relative to the popover's left edge) so it points at the
  // marker center, even when the popover is clamped to a wall.
  let arrowLeftPx = $derived(
    Math.max(14, Math.min(POPOVER_WIDTH - 26, (anchor.left + anchor.width / 2) - leftPx))
  );

  type Row = { id: SourceId; sub: string };
  const ROWS: Row[] = [
    { id: 'roomPc',   sub: 'HDMI 1' },
    { id: 'extPc',    sub: 'HDMI 2' },
    { id: 'airMedia', sub: 'Wireless' },
    { id: 'laptop',   sub: 'HDMI 3' },
  ];

  function handleRowClick(sourceId: SourceId, e: MouseEvent) {
    e.stopPropagation();
    onSelectSource(sourceId);
  }
</script>

<div
  class="popover"
  class:flip-above={flipAbove}
  style="top: {topPx}px; left: {leftPx}px; --arrow-x: {arrowLeftPx}px;"
  role="dialog"
  aria-label="Choose source for {displayLabel}"
>
  <div class="po-head">
    <span class="po-title">{displayId.toUpperCase()} · Route Source</span>
    <button class="po-x" onclick={onClose} aria-label="Close source picker" type="button">×</button>
  </div>

  <div class="po-list">
    {#each ROWS as row (row.id)}
      <button
        class="po-row"
        class:active={activeSource === row.id}
        onclick={(e) => handleRowClick(row.id, e)}
        aria-pressed={activeSource === row.id}
        type="button"
      >
        <span class="po-ico" aria-hidden="true">
          {#if row.id === 'roomPc'}
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
          {:else if row.id === 'extPc'}
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M3 10h18M8 20h8M12 16v4"/></svg>
          {:else if row.id === 'airMedia'}
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>
          {:else}
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="4" width="20" height="13" rx="2"/><path d="M2 20h20"/></svg>
          {/if}
        </span>
        <span class="po-name">{SOURCES[row.id].label}</span>
        <span class="po-sub">{row.sub}</span>
      </button>
    {/each}
  </div>

  <div class="po-foot">
    {#if canMirrorD1}
      <button class="po-fbtn" onclick={onMirrorD1} type="button">⧉ Match D1</button>
    {:else}
      <span></span>
    {/if}
    <button class="po-fbtn danger" onclick={onClear} type="button">⊘ Clear</button>
  </div>
</div>

<style>
  .popover {
    position: absolute;
    width: 240px;
    background-image: linear-gradient(180deg, #14213a, #08101e);
    border: 1px solid rgba(245, 166, 35, 0.4);
    border-radius: 12px;
    padding: 12px;
    z-index: 50;
    box-shadow:
      0 24px 60px rgba(0, 0, 0, 0.5),
      0 0 0 1px rgba(245, 166, 35, 0.18);
    animation: popover-in 140ms cubic-bezier(0.2, 0.8, 0.2, 1);
  }

  /* Arrow pointing up at the marker (default position: popover below marker) */
  .popover::before {
    content: '';
    position: absolute;
    top: -7px;
    left: var(--arrow-x, 24px);
    width: 12px;
    height: 12px;
    background: #14213a;
    border-top: 1px solid rgba(245, 166, 35, 0.4);
    border-left: 1px solid rgba(245, 166, 35, 0.4);
    transform: rotate(45deg) translate(-1px, -1px);
  }

  /* Flipped: popover sits above the marker, arrow on the bottom edge */
  .popover.flip-above::before {
    top: auto;
    bottom: -7px;
    border: none;
    border-bottom: 1px solid rgba(245, 166, 35, 0.4);
    border-right: 1px solid rgba(245, 166, 35, 0.4);
    background: #08101e;
  }

  @keyframes popover-in {
    from { opacity: 0; transform: translateY(-4px); }
    to   { opacity: 1; transform: translateY(0); }
  }

  .po-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    padding-bottom: 8px;
    border-bottom: 0.5px solid var(--color-border);
  }

  .po-title {
    font-size: 11px;
    font-weight: 900;
    color: #fff;
    letter-spacing: 0.06em;
  }

  .po-x {
    background: transparent;
    border: none;
    color: var(--color-copy-muted);
    font-size: 18px;
    line-height: 1;
    cursor: pointer;
    padding: 0 6px;
    font-family: inherit;
  }
  .po-x:hover { color: var(--color-copy); }

  .po-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .po-row {
    display: flex;
    align-items: center;
    gap: 9px;
    padding: 8px 9px;
    border-radius: 7px;
    background: rgba(15, 23, 42, 0.5);
    border: 0.5px solid transparent;
    color: var(--color-copy);
    cursor: pointer;
    font-size: 12px;
    font-weight: 700;
    font-family: inherit;
    text-align: left;
    transition: background 110ms ease, border-color 110ms ease;
  }
  .po-row:hover {
    background: rgba(30, 41, 59, 0.75);
  }
  .po-row.active {
    background: rgba(245, 166, 35, 0.16);
    border-color: rgba(245, 166, 35, 0.45);
    color: var(--color-accent);
  }

  .po-ico {
    width: 22px;
    height: 22px;
    display: grid;
    place-items: center;
    color: var(--color-accent);
    flex-shrink: 0;
  }
  .po-row:not(.active) .po-ico {
    color: var(--color-copy-soft);
  }

  .po-name {
    flex: 1;
  }

  .po-sub {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
    flex-shrink: 0;
  }
  .po-row.active .po-sub {
    color: var(--color-accent);
  }

  .po-foot {
    margin-top: 10px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 6px;
  }

  .po-fbtn {
    padding: 7px 8px;
    border-radius: 6px;
    font-size: 10px;
    font-weight: 800;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    cursor: pointer;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-family: inherit;
    transition: background 110ms ease, color 110ms ease;
  }
  .po-fbtn:hover { background: rgba(51, 65, 85, 0.7); color: var(--color-copy); }
  .po-fbtn.danger {
    background: rgba(239, 68, 68, 0.10);
    border-color: rgba(239, 68, 68, 0.25);
    color: #fca5a5;
  }
  .po-fbtn.danger:hover { background: rgba(239, 68, 68, 0.18); }

  @media (prefers-reduced-motion: reduce) {
    .popover { animation: none; }
    .po-row, .po-fbtn { transition: none; }
  }
</style>
