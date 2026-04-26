<script lang="ts">
  import { currentPage } from './lib/stores/page';
  import Home from './pages/Home.svelte';
  import Cameras from './pages/Cameras.svelte';
</script>

{#if $currentPage === 'home'}
  <Home />
{:else if $currentPage === 'cameras'}
  <Cameras />
{/if}
