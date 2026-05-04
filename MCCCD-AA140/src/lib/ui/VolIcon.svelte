<!--
  Shared speaker-cone glyph used by Home's volume controls + Audio header
  nav. The cone path is the same across all four variants; only the
  trailing decoration changes (waves / minus / X / plus). Audit H5
  extracted this from four inline duplicates.

  Variants:
    audio  — speaker + 2 sound waves   (header-nav "Audio" button)
    down   — speaker + horizontal bar  (Vol -)
    mute   — speaker + X cross         (Mute toggle)
    up     — speaker + plus            (Vol +)
-->
<script lang="ts">
  type Variant = 'audio' | 'down' | 'mute' | 'up';

  interface Props {
    variant: Variant;
    size?: number;          // px
    strokeWidth?: number;   // svg stroke-width
  }
  let { variant, size = 24, strokeWidth = 2 }: Props = $props();
</script>

<svg
  width={size}
  height={size}
  viewBox="0 0 24 24"
  fill="none"
  stroke="currentColor"
  stroke-width={strokeWidth}
  aria-hidden="true"
>
  <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor"/>
  {#if variant === 'audio'}
    <path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"/>
  {:else if variant === 'down'}
    <path d="M16 12h6"/>
  {:else if variant === 'mute'}
    <path d="M16 9l6 6M22 9l-6 6"/>
  {:else}
    <path d="M16 12h6M19 9v6"/>
  {/if}
</svg>
