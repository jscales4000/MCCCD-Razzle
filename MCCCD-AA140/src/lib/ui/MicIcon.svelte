<!--
  Shared microphone glyph used by Home's Lav + Handheld toggles. Audit H5
  extracted this from inline duplicates. Currently only the mic-on form
  exists; if the codebase ever wants a struck-through "muted" variant the
  parent already handles it via .mbtn.muted .mbtn-icon::after rotation.
-->
<script lang="ts">
  interface Props {
    size?: number;          // px, defaults to 26
    strokeWidth?: number;   // svg stroke-width, defaults to 2
  }
  let { size = 26, strokeWidth = 2 }: Props = $props();
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
  <rect x="9" y="2" width="6" height="12" rx="3"/>
  <path d="M5 10a7 7 0 0014 0M12 19v3"/>
</svg>
