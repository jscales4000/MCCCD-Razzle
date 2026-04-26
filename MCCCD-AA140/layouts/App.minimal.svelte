<script lang="ts">
  import { onMount } from 'svelte';
  import { panelOnline } from '../lib/stores/signals';
  import { ROOM_NAME, SIGNALS } from '../lib/contract';

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

  // Sources: index of active selection
  let activeSource = 0;
  const sources = ['AirMedia', 'HDMI'];

  function selectSource(idx: number) {
    activeSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sourceSelect, idx + 1);
    });
  }

  function togglePower() {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.displayPower, true);
    });
  }

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
    return () => window.removeEventListener('resize', applyViewport);
  });
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell layout-minimal">

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

    <main class="main-area glass-card">
      <div class="accent-bar"></div>
      <div class="sources-row">
        {#each sources as label, i}
          <button
            class="btn source-btn"
            class:active={activeSource === i}
            onclick={() => selectSource(i)}
            aria-pressed={activeSource === i}
          >{label}</button>
        {/each}
      </div>
      <div class="display-placeholder glass-card-inner">
        <span class="display-label">Display</span>
      </div>
    </main>

    <footer class="app-footer glass-card">
      <button class="btn power-btn" onclick={togglePower} aria-label="Power">
        ⏻ Power
      </button>
      <span class="footer-source-count">{sources.length} sources</span>
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

<style>
  .layout-minimal {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    position: relative;
    border-radius: var(--radius-panel);
    padding: 22px;
    display: flex;
    flex-direction: column;
    gap: 18px;
    min-height: 0;
  }

  .accent-bar {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 4px;
    border-radius: var(--radius-panel) var(--radius-panel) 0 0;
    background: var(--color-accent);
    opacity: 0.55;
  }

  .sources-row {
    display: flex;
    gap: 14px;
    padding-top: 8px;
  }

  .source-btn {
    height: 72px;
    padding: 0 32px;
    font-size: 22px;
    font-weight: 600;
    border-radius: var(--radius-button);
  }

  .display-placeholder {
    flex: 1;
    border-radius: 16px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    display: grid;
    place-items: center;
    min-height: 0;
  }

  .display-label {
    color: var(--color-copy-muted);
    font-size: 18px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }

  .glass-card-inner {
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
  }

  .power-btn {
    height: 68px;
    padding: 0 32px;
    font-size: 22px;
    font-weight: 600;
  }

  .footer-source-count {
    color: var(--color-copy-muted);
    font-size: 14px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }
</style>
