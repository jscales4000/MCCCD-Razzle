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

  const sources = ['PC', 'PC Ext', 'HDMI', 'USB-C', 'AirMedia', 'Airplay'];

  // Each display has its own active source and audio-out state
  let leftSource = 0;
  let rightSource = 2;
  let audioOutput: 'left' | 'right' = 'left';

  let volume = 50;
  let muted = false;

  function selectLeftSource(idx: number) {
    leftSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.display1Source, idx + 1);
    });
  }

  function selectRightSource(idx: number) {
    rightSource = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.display2Source, idx + 1);
    });
  }

  function setAudioOutput(display: 'left' | 'right') {
    audioOutput = display;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.audioOutputSelect, display === 'left' ? 1 : 2);
    });
  }

  function toggleMute() {
    muted = !muted;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.muteAll, muted);
    });
  }

  function volDown() {
    volume = Math.max(0, volume - 5);
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.volumeDown, true);
    });
  }

  function volUp() {
    volume = Math.min(100, volume + 5);
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.volumeUp, true);
    });
  }

  function openCamera() {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.cameraPage, true);
    });
  }

  function systemPower() {
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
  <div class="app-shell layout-dual-display">

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
      <!-- Left display panel -->
      <div class="glass-card display-panel" class:audio-active={audioOutput === 'left'}>
        <div class="accent-bar" class:active={audioOutput === 'left'}></div>
        <div class="display-heading">
          <div>
            <p class="display-label" style="color: var(--color-accent)">Display Left</p>
          </div>
          <button
            class="btn audio-select-btn"
            class:active={audioOutput === 'left'}
            onclick={() => setAudioOutput('left')}
            aria-pressed={audioOutput === 'left'}
            title="Route audio output to this display"
          >🔊</button>
        </div>
        {#if audioOutput === 'left'}
          <p class="audio-active-label">Audio Output Selected</p>
        {/if}
        <div class="source-grid">
          {#each sources as label, i}
            <button
              class="btn source-btn"
              class:active={leftSource === i}
              onclick={() => selectLeftSource(i)}
              aria-pressed={leftSource === i}
            >{label}</button>
          {/each}
        </div>
      </div>

      <!-- Right display panel -->
      <div class="glass-card display-panel" class:audio-active={audioOutput === 'right'}>
        <div class="accent-bar dim" class:active={audioOutput === 'right'}></div>
        <div class="display-heading">
          <div>
            <p class="display-label">Display Right</p>
          </div>
          <button
            class="btn audio-select-btn"
            class:active={audioOutput === 'right'}
            onclick={() => setAudioOutput('right')}
            aria-pressed={audioOutput === 'right'}
            title="Route audio output to this display"
          >🔊</button>
        </div>
        {#if audioOutput === 'right'}
          <p class="audio-active-label">Audio Output Selected</p>
        {/if}
        <div class="source-grid">
          {#each sources as label, i}
            <button
              class="btn source-btn"
              class:active={rightSource === i}
              onclick={() => selectRightSource(i)}
              aria-pressed={rightSource === i}
            >{label}</button>
          {/each}
        </div>
      </div>
    </main>

    <footer class="app-footer glass-card">
      <button class="btn power-btn" onclick={systemPower}>⏻ Power</button>
      <button class="btn vol-btn" onclick={volDown}>Vol −</button>
      <button class="btn vol-btn" class:active={muted} onclick={toggleMute} aria-pressed={muted}>
        {muted ? '🔇 Muted' : 'Mute'}
      </button>
      <input type="range" class="volume-slider" min="0" max="100" bind:value={volume} aria-label="Volume" />
      <span class="vol-readout">{volume}%</span>
      <button class="btn vol-btn" onclick={volUp}>Vol +</button>
      <button class="btn camera-btn" onclick={openCamera}>Camera</button>
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
  .layout-dual-display {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
    min-height: 0;
  }

  .display-panel {
    position: relative;
    border-radius: var(--radius-panel);
    padding: 22px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    min-height: 0;
    transition: border-color 200ms ease;
  }

  .display-panel.audio-active {
    border-color: rgba(34, 197, 94, 0.4);
  }

  .accent-bar {
    position: absolute;
    top: 0; left: 0; right: 0;
    height: 4px;
    border-radius: var(--radius-panel) var(--radius-panel) 0 0;
    background: var(--color-accent);
    opacity: 0.55;
  }

  .accent-bar.dim {
    opacity: 0.22;
  }

  .accent-bar.active {
    opacity: 0.55;
  }

  .display-heading {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
  }

  .display-label {
    margin: 0 0 4px;
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-soft);
  }

  .audio-active-label {
    margin: 0;
    color: #22c55e;
    font-size: 14px;
    font-weight: 600;
  }

  .audio-select-btn {
    width: 48px;
    height: 48px;
    font-size: 20px;
    border-radius: 12px;
  }

  .source-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    flex: 1;
    min-height: 0;
  }

  .source-btn {
    font-size: 18px;
    font-weight: 600;
    min-height: 80px;
  }

  .power-btn {
    height: 68px;
    padding: 0 28px;
    font-size: 20px;
    font-weight: 600;
  }

  .vol-btn {
    height: 68px;
    padding: 0 20px;
    font-size: 18px;
    font-weight: 600;
  }

  .volume-slider {
    flex: 1;
    accent-color: var(--color-accent);
  }

  .vol-readout {
    color: var(--color-copy-soft);
    font-size: 14px;
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
  }

  .camera-btn {
    height: 68px;
    padding: 0 28px;
    font-size: 20px;
    font-weight: 600;
    margin-left: auto;
  }
</style>
