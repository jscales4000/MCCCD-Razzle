<script lang="ts">
  import { currentPage } from './lib/stores/page';
  import Home from './pages/Home.svelte';
  import DragCloneOverlay from './components/DragCloneOverlay.svelte';

  // Home + DragCloneOverlay load synchronously (always-needed first paint).
  // Cameras / AudioMixer / DisplayRouting are dynamic-imported so their JS
  // and CSS land in separate Vite chunks, only fetched when the user
  // navigates to that page. Per-audit H3 — biggest single first-paint win.
</script>

<!--
  ch5-background is now placed directly in <body> via build.mjs (per persona
  example) so its IntersectionObserver sees the full viewport on first paint.
  CH5 Video Specialist hard rule: do NOT set CSS `background-color` on
  ch5-background — the CSS color paints OVER the native surface compositor
  the element uses, blocking the cutout. Use the `backgroundcolor` attribute
  instead, which the native renderer honors.
-->
{#if $currentPage === 'home'}
  <Home />
{:else if $currentPage === 'cameras'}
  {#await import('./pages/Cameras.svelte') then mod}
    {@const Cameras = mod.default}
    <Cameras />
  {/await}
{:else if $currentPage === 'audio'}
  <!-- Audio Mixer page (Mockup #13). Reached via the Audio button on Home's footer. -->
  {#await import('./pages/AudioMixer.svelte') then mod}
    {@const AudioMixer = mod.default}
    <AudioMixer />
  {/await}
{:else if $currentPage === 'routing'}
  <!-- Display Routing matrix page (Mockup #14). Reached via tile-tap on Home. -->
  {#await import('./pages/DisplayRouting.svelte') then mod}
    {@const DisplayRouting = mod.default}
    <DisplayRouting />
  {/await}
{/if}

<!--
  DragCloneOverlay lives at the App root, OUTSIDE the .panel-stage transform,
  so the dragging chip uses raw viewport coordinates without double-scaling.
  Renders nothing unless a drag is in flight. Always mounted because the
  routing page relies on its presence; harmless when no drag is active.
-->
<DragCloneOverlay />
