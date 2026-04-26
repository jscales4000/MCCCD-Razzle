<script lang="ts">
  import { onMount } from 'svelte';
  import { panelOnline, placeholderToggle, togglePlaceholder } from './lib/stores/signals';
  import { ROOM_NAME } from './lib/contract';

  const BASE_WIDTH = 1280;
  const BASE_HEIGHT = 800;
  const DEVICE_PROFILES = {
    auto: null,
    tsw770: { width: 1280, height: 800, label: 'TSW-770' },
    tsw1070: { width: 1920, height: 1200, label: 'TSW-1070' }
  } as const;

  let previewMode: keyof typeof DEVICE_PROFILES = 'auto';
  let viewportLabel = `${BASE_WIDTH}x${BASE_HEIGHT}`;
  let scaleLabel = '1.00x';
  let profileLabel = 'Auto';
  let showPreviewDock = false;
  let applyViewport = () => {};

  function setPreviewMode(mode: keyof typeof DEVICE_PROFILES) {
    previewMode = mode;
    applyViewport();
  }

  onMount(() => {
    showPreviewDock = ['127.0.0.1', 'localhost'].includes(window.location.hostname);

    applyViewport = () => {
      const profile = DEVICE_PROFILES[previewMode];
      const viewportWidth = profile?.width ?? window.innerWidth;
      const viewportHeight = profile?.height ?? window.innerHeight;
      const scale = Math.min(viewportWidth / BASE_WIDTH, viewportHeight / BASE_HEIGHT);

      document.documentElement.style.setProperty('--panel-scale', scale.toString());
      document.documentElement.style.setProperty('--viewport-width', `${viewportWidth}px`);
      document.documentElement.style.setProperty('--viewport-height', `${viewportHeight}px`);

      viewportLabel = `${viewportWidth}x${viewportHeight}`;
      scaleLabel = `${scale.toFixed(2)}x`;
      profileLabel = profile?.label ?? 'Auto';
    };

    applyViewport();
    window.addEventListener('resize', applyViewport);

    return () => {
      window.removeEventListener('resize', applyViewport);
    };
  });
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell">
    <header class="app-header glass-card">
      <div class="header-copy">
        <p class="eyebrow">CH5 Touch Panel</p>
        <h1>{ROOM_NAME}</h1>
      </div>
      <div class="status-pill" class:online={$panelOnline} aria-live="polite">
        <span class="status-dot"></span>
        <span>{$panelOnline ? 'Online' : 'Offline'}</span>
      </div>
    </header>

    <main class="placeholder-stage">
      <section class="placeholder-card glass-card">
        <p class="display-label">Placeholder</p>
        <h2>Replace this with your panel UI</h2>
        <p class="placeholder-copy">
          Edit <code>src/App.svelte</code> for layout, <code>src/lib/contract.ts</code> for signal names,
          and <code>src/lib/stores/signals.ts</code> for the Svelte stores. The button below proves the
          signal pipeline by toggling <code>{ROOM_NAME}.PlaceholderToggle</code>.
        </p>
        <button
          class="placeholder-button btn"
          class:active={$placeholderToggle}
          onclick={togglePlaceholder}
          aria-pressed={$placeholderToggle}
        >
          {$placeholderToggle ? 'Active' : 'Idle'}
        </button>
      </section>
    </main>

    <footer class="app-footer glass-card">
      <span class="footer-label">Footer slot — add transport, power, or status controls here.</span>
    </footer>
  </div>

  {#if showPreviewDock}
    <aside class="preview-dock glass-card" aria-label="Local resolution preview controls">
      <div class="preview-copy">
        <strong>{profileLabel}</strong>
        <span>{viewportLabel} · {scaleLabel}</span>
      </div>
      <div class="preview-actions">
        <button class="preview-button btn" class:active={previewMode === 'auto'} onclick={() => setPreviewMode('auto')}>Auto</button>
        <button class="preview-button btn" class:active={previewMode === 'tsw770'} onclick={() => setPreviewMode('tsw770')}>770</button>
        <button class="preview-button btn" class:active={previewMode === 'tsw1070'} onclick={() => setPreviewMode('tsw1070')}>1070</button>
      </div>
    </aside>
  {/if}
</div>
