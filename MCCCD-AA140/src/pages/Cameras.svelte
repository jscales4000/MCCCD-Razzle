<script lang="ts">
  import { goToPage } from '../lib/stores/page';
</script>

<div class="page-stub">
  <button onclick={() => goToPage('home')}>← Home</button>
  <h1>Cameras</h1>
  <p>Stub. Replaced in Phase 3.</p>
</div>

<style>
  .page-stub { padding: 40px; color: #cbd5e1; }
  h1 { color: var(--color-accent); }
</style>
