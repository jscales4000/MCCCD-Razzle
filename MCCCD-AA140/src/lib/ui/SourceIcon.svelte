<!--
  SourceIcon — shared inline SVG primitive for the four input sources used
  across Display Routing (Mockup #14), DisplayTile, and the Audio Mixer.

  Glyphs match the Mockup #14 source-list icons. Stroke uses currentColor so
  the host can drive default/active/disabled tinting via the `state` prop.
-->

<script lang="ts">
  type SourceKind = 'roomPc' | 'extPc' | 'airMedia' | 'laptop';
  type IconState = 'default' | 'active' | 'disabled';

  interface Props {
    source: SourceKind;
    size?: number;
    state?: IconState;
  }

  let { source, size = 20, state = 'default' }: Props = $props();
</script>

<svg
  width={size}
  height={size}
  viewBox="0 0 24 24"
  fill="none"
  stroke="currentColor"
  stroke-width="1.8"
  class="src-glyph state-{state}"
  aria-hidden="true"
>
  {#if source === 'roomPc'}
    <rect x="2" y="3" width="20" height="14" rx="2"/>
    <path d="M8 21h8M12 17v4"/>
  {:else if source === 'extPc'}
    <rect x="6" y="2" width="12" height="20" rx="2"/>
    <line x1="12" y1="18" x2="12" y2="18"/>
  {:else if source === 'airMedia'}
    <path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/>
  {:else if source === 'laptop'}
    <rect x="2" y="7" width="20" height="14" rx="2"/>
    <path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2"/>
  {/if}
</svg>

<style>
  .src-glyph {
    color: currentColor;
    transition: color 140ms ease, opacity 140ms ease;
  }
  .src-glyph.state-active {
    color: #f5a623;
  }
  .src-glyph.state-disabled {
    opacity: 0.4;
  }

  @media (prefers-reduced-motion: reduce) {
    .src-glyph {
      transition: none;
    }
  }
</style>
