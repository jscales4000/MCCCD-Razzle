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

  const localSources = ['HDMI 1', 'AirMedia', 'VTC'];
  let activeSource = 0;

  const vtcControls = ['Mute', 'Camera', 'Layout', 'Dial'];
  let micMuted = false;
  let camActive = true;

  const envControls = ['Lights', 'Shades', 'Presets'];

  function selectSource(idx: number) {
    activeSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sourceSelect, idx + 1);
    });
  }

  function toggleMic() {
    micMuted = !micMuted;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.micMute, micMuted);
    });
  }

  function toggleCam() {
    camActive = !camActive;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.cameraPrivacy, !camActive);
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
  <div class="app-shell layout-vtc">

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
      <!-- Left: local content -->
      <div class="local-column glass-card">
        <div class="accent-bar"></div>
        <div class="sources-row">
          {#each localSources as label, i}
            <button
              class="btn source-btn"
              class:active={activeSource === i}
              onclick={() => selectSource(i)}
              aria-pressed={activeSource === i}
            >{label}</button>
          {/each}
        </div>
        <div class="content-area">
          <span class="area-label">Local Content</span>
        </div>
      </div>

      <!-- Right: far end + VTC controls -->
      <div class="vtc-column">
        <div class="glass-card far-end-panel">
          <span class="area-label">Far End</span>
        </div>
        <div class="glass-card vtc-controls-panel">
          <p class="panel-label">VTC Controls</p>
          <div class="vtc-buttons">
            <button class="btn vtc-btn" class:active={micMuted} onclick={toggleMic} aria-pressed={micMuted}>
              {micMuted ? '🔇 Muted' : '🎙 Mute'}
            </button>
            <button class="btn vtc-btn" class:active={camActive} onclick={toggleCam} aria-pressed={camActive}>
              📷 Cam
            </button>
            <button class="btn vtc-btn">Layout</button>
            <button class="btn vtc-btn">Dial</button>
          </div>
        </div>
      </div>
    </main>

    <footer class="app-footer glass-card">
      {#each envControls as label}
        <button class="btn env-btn">{label}</button>
      {/each}
      <span class="footer-right">Attributes</span>
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
  .layout-vtc {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    display: grid;
    grid-template-columns: 1fr 420px;
    gap: 20px;
    min-height: 0;
  }

  .local-column {
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
    padding: 0 24px;
    font-size: 20px;
    font-weight: 600;
  }

  .content-area, .far-end-panel {
    flex: 1;
    border-radius: 16px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    display: grid;
    place-items: center;
    min-height: 0;
  }

  .far-end-panel {
    border-radius: var(--radius-panel);
    padding: 22px;
  }

  .vtc-column {
    display: flex;
    flex-direction: column;
    gap: 20px;
    min-height: 0;
  }

  .vtc-controls-panel {
    border-radius: var(--radius-panel);
    padding: 22px;
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .panel-label {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }

  .vtc-buttons {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    flex: 1;
  }

  .vtc-btn {
    font-size: 20px;
    font-weight: 600;
    height: 64px;
  }

  .env-btn {
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

  .area-label {
    color: var(--color-copy-muted);
    font-size: 18px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }
</style>
