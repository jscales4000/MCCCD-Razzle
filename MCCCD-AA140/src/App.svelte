<script lang="ts">
  import { currentPage } from './lib/stores/page';
  import Home from './pages/Home.svelte';
  import Cameras from './pages/Cameras.svelte';
  import Settings from './pages/Settings.svelte';
  import DragDropRouter from './pages/DragDropRouter.svelte';
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
{:else if $currentPage === 'settings'}
  <Settings />
{:else if $currentPage === 'dragdrop'}
  <!-- Experimental sub-page; not reachable from main nav. Reach via
       goToPage('dragdrop') from a dev console or future settings toggle.
       Kept until the chrome realignment to Mockup 10 is in place. -->
  <DragDropRouter />
{/if}

<!--
  DragCloneOverlay lives at the App root, OUTSIDE the .panel-stage transform,
  so the dragging chip uses raw viewport coordinates without double-scaling.
  Renders nothing unless a drag is in flight. Always mounted because the
  DragDropRouter sub-page relies on its presence; harmless when no drag
  is active and not on the dragdrop page.
-->
<DragCloneOverlay />
