<script lang="ts">
  import { currentPage } from './lib/stores/page';
  import Home from './pages/Home.svelte';
  import Cameras from './pages/Cameras.svelte';
  import Settings from './pages/Settings.svelte';
</script>

{#if $currentPage === 'home'}
  <Home />
{:else if $currentPage === 'cameras'}
  <Cameras />
{:else if $currentPage === 'settings'}
  <Settings />
{/if}
