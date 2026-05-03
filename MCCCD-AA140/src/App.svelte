<script lang="ts">
  import { currentPage } from './lib/stores/page';
  import Home from './pages/Home.svelte';
  import Cameras from './pages/Cameras.svelte';
  import AudioMixer from './pages/AudioMixer.svelte';
  import DisplayRouting from './pages/DisplayRouting.svelte';
  import DragCloneOverlay from './components/DragCloneOverlay.svelte';
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
  <Cameras />
{:else if $currentPage === 'audio'}
  <!-- Audio Mixer page (Mockup #13). Reached via the Audio button on Home's footer. -->
  <AudioMixer />
{:else if $currentPage === 'routing'}
  <!-- Display Routing matrix page (Mockup #14). Reached via tile-tap on Home. -->
  <DisplayRouting />
{/if}

<!--
  DragCloneOverlay lives at the App root, OUTSIDE the .panel-stage transform,
  so the dragging chip uses raw viewport coordinates without double-scaling.
  Renders nothing unless a drag is in flight. Always mounted because the
  routing page relies on its presence; harmless when no drag is active.
-->
<DragCloneOverlay />
