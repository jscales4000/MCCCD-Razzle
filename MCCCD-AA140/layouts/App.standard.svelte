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

  const sources = ['HDMI 1', 'HDMI 2', 'AirMedia'];
  let activeSource = 0;

  const scenePresets = ['Presentation', 'Whiteboard', 'Blank'];
  let activeScene = 0;

  let volume = 50;

  function selectSource(idx: number) {
    activeSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sourceSelect, idx + 1);
    });
  }

  function selectScene(idx: number) {
    activeScene = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sceneRecall, idx + 1);
    });
  }

  function setVolume(e: Event) {
    volume = Number((e.target as HTMLInputElement).value);
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.volumeSet, Math.round((volume / 100) * 65535));
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
  <div class="app-shell layout-standard">

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

    <main class="main-area">
      <!-- Left: display + sources -->
      <div class="display-column glass-card">
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
        <div class="display-area">
          <span class="display-label">Main Display</span>
        </div>
      </div>

      <!-- Right: camera + volume -->
      <div class="controls-column">
        <div class="glass-card controls-panel">
          <p class="panel-label">Camera</p>
          <div class="ptz-grid">
            <button class="btn ptz-btn" aria-label="PTZ Up">▲</button>
            <button class="btn ptz-btn" aria-label="PTZ Down">▼</button>
          </div>
        </div>
        <div class="glass-card volume-panel">
          <p class="panel-label">Volume</p>
          <input
            type="range"
            class="volume-slider"
            min="0" max="100"
            bind:value={volume}
            oninput={setVolume}
            aria-label="Volume"
          />
          <span class="volume-value">{volume}%</span>
        </div>
      </div>
    </main>

    <footer class="app-footer glass-card">
      {#each scenePresets as label, i}
        <button
          class="btn scene-btn"
          class:active={activeScene === i}
          onclick={() => selectScene(i)}
          aria-pressed={activeScene === i}
        >{label}</button>
      {/each}
      <span class="footer-right">Env · Presets</span>
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
  .layout-standard {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    display: grid;
    grid-template-columns: 1fr 320px;
    gap: 20px;
    min-height: 0;
  }

  .display-column {
    position: relative;
    border-radius: var(--radius-panel);
    padding: 22px;
    display: flex;
    flex-direction: column;
    gap: 16px;
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

  .sources-row {
    display: flex;
    gap: 12px;
    flex-shrink: 0;
  }

  .source-btn {
    height: 60px;
    padding: 0 28px;
    font-size: 20px;
    font-weight: 600;
    border-radius: var(--radius-button);
  }

  .display-area {
    flex: 1;
    border-radius: 16px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    display: grid;
    place-items: center;
    min-height: 0;
  }

  .controls-column {
    display: flex;
    flex-direction: column;
    gap: 20px;
    min-height: 0;
  }

  .controls-panel,
  .volume-panel {
    border-radius: var(--radius-panel);
    padding: 22px;
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 14px;
    min-height: 0;
  }

  .panel-label {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }

  .ptz-grid {
    display: flex;
    gap: 12px;
  }

  .ptz-btn {
    flex: 1;
    height: 56px;
    font-size: 22px;
  }

  .volume-slider {
    width: 100%;
    accent-color: var(--color-accent);
  }

  .volume-value {
    color: var(--color-copy-soft);
    font-size: 15px;
    align-self: flex-end;
  }

  .scene-btn {
    height: 68px;
    padding: 0 28px;
    font-size: 20px;
    font-weight: 600;
  }

  .footer-right {
    margin-left: auto;
    color: var(--color-copy-muted);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }

  .display-label {
    color: var(--color-copy-muted);
    font-size: 18px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }
</style>
