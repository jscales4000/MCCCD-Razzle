<!--
  DisplayStatusCard — one row in the right-hand status sidebar.

  Mirrors the live state of a single display. Tapping the card is a
  shortcut to open the source picker for that display (same effect as
  tapping the display marker in the plan).
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
    onTap: (displayId: DisplayId) => void;
  }

  let { displayId, label, spec, activeSource, powerOn, selected, onTap }: Props = $props();

  let sourceLabel = $derived(activeSource ? SOURCES[activeSource].label : null);
</script>

<button
  class="sd-row"
  class:active={selected}
  class:off={!powerOn}
  onclick={() => onTap(displayId)}
  aria-label="{label} status — currently {sourceLabel ?? 'no source'}"
  aria-pressed={selected}
  type="button"
>
  <div class="sd-head">
    <span class="sd-id">{displayId.toUpperCase()}</span>
    <span class="sd-name">{label}</span>
    <span class="sd-spacer"></span>
    <span class="sd-pwr" class:off={!powerOn}>
      <span class="dot" aria-hidden="true"></span>{powerOn ? 'On' : 'Off'}
    </span>
  </div>
  <div class="sd-src">
    {#if sourceLabel}
      <span class="arr">▸</span> {sourceLabel}
    {:else}
      — No Source
    {/if}
  </div>
  <div class="sd-meta">{spec}</div>
</button>

<style>
  .sd-row {
    appearance: none;
    -webkit-appearance: none;
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 10px 12px;
    background-image: linear-gradient(180deg, rgba(15, 23, 42, 0.55), rgba(8, 14, 26, 0.55));
    border: 0.5px solid var(--color-border);
    border-radius: 10px;
    color: inherit;
    text-align: left;
    cursor: pointer;
    font-family: inherit;
    transition: border-color 140ms ease, background-image 140ms ease, transform 110ms ease;
  }
  .sd-row:hover {
    border-color: rgba(245, 166, 35, 0.4);
  }
  .sd-row:active {
    transform: scale(0.99);
  }
  .sd-row.active {
    border-color: rgba(245, 166, 35, 0.55);
    background-image: linear-gradient(180deg, rgba(245, 166, 35, 0.18), rgba(245, 166, 35, 0.05));
  }
  .sd-row.off {
    opacity: 0.85;
  }
  .sd-row:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 2px;
  }

  .sd-head {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .sd-id {
    font-size: 12px;
    font-weight: 900;
    color: var(--color-accent);
    background: rgba(0, 0, 0, 0.3);
    padding: 2px 7px;
    border-radius: 4px;
  }
  .sd-row.off .sd-id {
    color: var(--color-copy-muted);
  }

  .sd-name {
    font-size: 12px;
    font-weight: 700;
    color: var(--color-copy);
  }

  .sd-spacer { flex: 1; }

  .sd-pwr {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-success);
  }
  .sd-pwr.off { color: var(--color-copy-muted); }

  .sd-pwr .dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 6px currentColor;
  }
  .sd-pwr.off .dot {
    box-shadow: none;
  }

  .sd-src {
    font-size: 14px;
    font-weight: 800;
    color: var(--color-copy);
    margin-top: 2px;
  }
  .sd-row.off .sd-src {
    color: var(--color-copy-muted);
  }
  .sd-src .arr {
    color: var(--color-accent);
    font-weight: 900;
  }

  .sd-meta {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  @media (prefers-reduced-motion: reduce) {
    .sd-row { transition: none; }
  }
</style>
