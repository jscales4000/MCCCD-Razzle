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

  const sources = ['Laptop', 'Doc Cam', 'AirMedia'];
  let activeSource = 0;

  const footerControls = ['Doc Cam', 'Capture', 'Screen', 'Volume'];
  let captureActive = false;
  let volume = 50;

  function selectSource(idx: number) {
    activeSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sourceSelect, idx + 1);
    });
  }

  function toggleCapture() {
    captureActive = !captureActive;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.recordEnable, captureActive);
    });
  }

  function toggleLights() {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.lightsToggle, true);
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
      const w = profile?.width ?? window.innerWidth;
      const h = profile?.height ?? window.innerHeight;
      const scale = Math.min(w / BASE_WIDTH, h / BASE_HEIGHT);
      document.documentElement.style.setProperty('--panel-scale', scale.toString());
      document.documentElement.style.setProperty('--viewport-width', `${w}px`);
      document.documentElement.style.setProperty('--viewport-height', `${h}px`);
      viewportLabel = `${w}x${h}`;
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
  <div class="app-shell layout-edu">

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

    <!-- Source row sits between header and main display -->
    <div class="sources-bar">
      {#each sources as label, i}
        <button
          class="btn source-btn"
          class:active={activeSource === i}
          onclick={() => selectSource(i)}
          aria-pressed={activeSource === i}
        >{label}</button>
      {/each}
      <button class="btn lights-btn" onclick={toggleLights} style="margin-left:auto">Lights</button>
    </div>

    <main class="main-display glass-card">
      <div class="accent-bar"></div>
      <span class="display-label">Main Display</span>
    </main>

    <footer class="app-footer glass-card">
      <button class="btn footer-btn">Doc Cam</button>
      <button class="btn footer-btn" class:active={captureActive} onclick={toggleCapture} aria-pressed={captureActive}>
        {captureActive ? '● Capture' : 'Capture'}
      </button>
      <button class="btn footer-btn">Screen</button>
      <div class="volume-group">
        <span class="footer-label">Volume</span>
        <input
          type="range"
          class="volume-slider"
          min="0" max="100"
          bind:value={volume}
          aria-label="Volume"
        />
      </div>
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
  .layout-edu {
    display: grid;
    grid-template-rows: 92px 72px 1fr 104px;
    gap: 16px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .sources-bar {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .source-btn, .lights-btn {
    height: 60px;
    padding: 0 28px;
    font-size: 20px;
    font-weight: 600;
  }

  .main-display {
    position: relative;
    border-radius: var(--radius-panel);
    display: grid;
    place-items: center;
    min-height: 0;
  }

  .accent-bar {
    position: absolute;
    top: 0; left: 0; right: 0;
    height: 4px;
    border-radius: var(--radius-panel) var(--radius-panel) 0 0;
    background: var(--color-accent);
    opacity: 0.55;
  }

  .display-label {
    color: var(--color-copy-muted);
    font-size: 20px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
  }

  .footer-btn {
    height: 68px;
    padding: 0 32px;
    font-size: 20px;
    font-weight: 600;
  }

  .volume-group {
    display: flex;
    align-items: center;
    gap: 16px;
    margin-left: auto;
    flex: 1;
    max-width: 360px;
  }

  .footer-label {
    color: var(--color-copy-muted);
    font-size: 14px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    white-space: nowrap;
  }

  .volume-slider {
    flex: 1;
    accent-color: var(--color-accent);
  }
</style>
