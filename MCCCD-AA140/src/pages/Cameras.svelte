<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import { panelOnline, camTrackingModeFb } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import { CAMERAS, rtspMain, type Camera } from '../lib/cameras';

  let activeCamera: Camera = $state(CAMERAS[0]);

  let panSpeed = $state(50);
  let tiltSpeed = $state(50);

  const presets = [
    { idx: 1, name: 'DEFAULT' },
    { idx: 2, name: 'PRIMARY' },
    { idx: 3, name: 'SECONDARY' },
  ];

  // Preview dock (browser-dev only)
  const BASE_WIDTH = 1280;
  const BASE_HEIGHT = 800;
  const DEVICE_PROFILES = {
    auto: null,
    tsw770: { width: 1280, height: 800, label: 'TSW-770' },
    tsw1070: { width: 1920, height: 1200, label: 'TSW-1070' }
  } as const;
  let previewMode: keyof typeof DEVICE_PROFILES = $state('auto');
  let viewportLabel = $state(`${BASE_WIDTH}x${BASE_HEIGHT}`);
  let scaleLabel = $state('1.00x');
  let profileLabel = $state('Auto');
  let showPreviewDock = $state(false);
  let applyViewport = () => {};

  function selectCamera(cam: Camera) {
    activeCamera = cam;
    publishAnalog(SIGNALS.cameraSelect, cam.selectIndex);
  }

  function ptz(dir: 'up' | 'down' | 'left' | 'right') {
    const map = {
      up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
      left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
    };
    pulseDigital(map[dir]);
  }

  function recallPreset(i: number) { publishAnalog(SIGNALS.shotPresetRecall, i); }
  function savePreset(i: number)   { publishAnalog(SIGNALS.shotPresetSave, i); }
  function deletePreset(i: number) { publishAnalog(SIGNALS.shotPresetDelete, i); }

  function sendToVtc() { pulseDigital(SIGNALS.camSendToVtc); }

  function setTrackingMode(mode: 1 | 2 | 3) {
    publishAnalog(SIGNALS.camTrackingMode, mode);
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
  <title>{ROOM_NAME} Cameras</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell layout-cameras">

    <header class="app-header glass-card">
      <button class="btn back-btn" onclick={() => goToPage('home')}>← Home</button>
      <div class="header-copy">
        <p class="eyebrow">CH5 Touch Panel</p>
        <h1>{ROOM_NAME} — Cameras</h1>
      </div>
      <div class="status-pill" class:online={$panelOnline}>
        <span class="status-dot"></span>
        <span>{$panelOnline ? 'Online' : 'Offline'}</span>
      </div>
    </header>

    <div class="work-area">

      <!-- Camera selector sidebar -->
      <div class="glass-card camera-sidebar">
        <p class="panel-label">Cameras</p>
        {#each CAMERAS as cam}
          <button
            class="btn camera-select-btn"
            class:active={activeCamera.id === cam.id}
            onclick={() => selectCamera(cam)}
            aria-pressed={activeCamera.id === cam.id}
          >
            <strong>{cam.label}</strong>
            <em>{cam.model}</em>
          </button>
        {/each}
      </div>

      <!-- Live preview + transparent PTZ overlay -->
      <div class="glass-card preview-panel">
        <p class="panel-label">Preview — {activeCamera.label} ({activeCamera.model})</p>
        <div class="video-container">
          <ch5-video
            id="cam-preview"
            indexId={String(activeCamera.selectIndex)}
            url={rtspMain(activeCamera)}
            sourceType="Network"
            aspectRatio="16:9"
            stretch="false"
            zindex="0"
          ></ch5-video>
          <div class="ptz-overlay">
            <button class="ptz-btn up"    onclick={() => ptz('up')}    aria-label="Tilt up">▲</button>
            <button class="ptz-btn left"  onclick={() => ptz('left')}  aria-label="Pan left">◀</button>
            <button class="ptz-btn right" onclick={() => ptz('right')} aria-label="Pan right">▶</button>
            <button class="ptz-btn down"  onclick={() => ptz('down')}  aria-label="Tilt down">▼</button>
          </div>
        </div>
      </div>

      <!-- Right-side controls -->
      <div class="glass-card controls-panel">
        <div class="speed-block">
          <label class="speed-label">
            <span>Pan Speed</span>
            <input type="range" class="speed-slider" min="1" max="100" bind:value={panSpeed} aria-label="Pan speed" />
            <span class="speed-readout">{panSpeed}</span>
          </label>
          <label class="speed-label">
            <span>Tilt Speed</span>
            <input type="range" class="speed-slider" min="1" max="100" bind:value={tiltSpeed} aria-label="Tilt speed" />
            <span class="speed-readout">{tiltSpeed}</span>
          </label>
        </div>

        <button class="btn vtc-btn" onclick={sendToVtc}>▶ Send to VTC</button>

        <div class="tracking-block">
          <p class="panel-label">Tracking Mode</p>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 1} onclick={() => setTrackingMode(1)}>People</button>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 2} onclick={() => setTrackingMode(2)}>Group</button>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 3} onclick={() => setTrackingMode(3)}>VX AutoSwitch</button>
        </div>
      </div>

    </div>

    <!-- Presets row -->
    <div class="presets-row glass-card">
      <p class="panel-label presets-label">Shot Presets</p>
      <div class="presets-grid">
        {#each presets as p}
          <div class="preset-slot">
            <span class="preset-name">{p.name}</span>
            <div class="preset-actions">
              <button class="btn preset-btn" onclick={() => savePreset(p.idx)}>Save</button>
              <button class="btn preset-btn" onclick={() => recallPreset(p.idx)}>Recall</button>
              <button class="btn preset-btn" onclick={() => deletePreset(p.idx)}>Delete</button>
            </div>
          </div>
        {/each}
      </div>
    </div>

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
  .layout-cameras {
    display: grid;
    grid-template-rows: 92px 1fr 140px;
    gap: 16px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .back-btn { height: 56px; padding: 0 18px; font-size: 14px; font-weight: 700; margin-right: 16px; }
  .work-area {
    display: grid;
    grid-template-columns: 180px 1fr 240px;
    gap: 16px;
    min-height: 0;
  }
  .camera-sidebar {
    border-radius: var(--radius-panel);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .panel-label {
    margin: 0 0 4px;
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }
  .camera-select-btn {
    text-align: left;
    padding: 12px 14px;
    height: auto;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
  }
  .camera-select-btn strong { font-size: 16px; font-weight: 700; }
  .camera-select-btn em { font-style: normal; font-size: 12px; color: var(--color-copy-muted); }
  .preview-panel {
    border-radius: var(--radius-panel);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 0;
  }
  .video-container {
    position: relative;
    flex: 1;
    border-radius: 12px;
    overflow: hidden;
    background: #050d1a;
    border: 1px solid var(--color-border);
  }
  .video-container :global(ch5-video) {
    background: transparent;
    width: 100%;
    height: 100%;
    display: block;
  }
  .ptz-overlay {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }
  .ptz-btn {
    pointer-events: auto;
    position: absolute;
    width: 64px;
    height: 64px;
    font-size: 24px;
    background: rgba(15, 23, 42, 0.4);
    border: 1px solid rgba(56, 189, 248, 0.4);
    color: #38bdf8;
    border-radius: 50%;
    backdrop-filter: blur(4px);
  }
  .ptz-btn.up    { top: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-btn.down  { bottom: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-btn.left  { left: 12px; top: 50%; transform: translateY(-50%); }
  .ptz-btn.right { right: 12px; top: 50%; transform: translateY(-50%); }
  .controls-panel {
    border-radius: var(--radius-panel);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  .speed-block { display: flex; flex-direction: column; gap: 12px; }
  .speed-label { display: grid; grid-template-columns: 1fr 1fr 36px; gap: 8px; align-items: center; font-size: 12px; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; color: var(--color-copy-muted); }
  .speed-slider { accent-color: var(--color-accent); }
  .speed-readout { font-variant-numeric: tabular-nums; color: var(--color-copy-soft); }
  .vtc-btn { height: 60px; font-size: 18px; font-weight: 700; background: rgba(34, 197, 94, 0.2); border-color: rgba(34, 197, 94, 0.5); color: #bbf7d0; }
  .tracking-block { display: flex; flex-direction: column; gap: 8px; }
  .tracking-btn { height: 48px; text-align: left; padding: 0 14px; font-weight: 600; }
  .tracking-btn.active { background: var(--color-accent); color: #0b1220; border-color: var(--color-accent); }
  .presets-row { display: flex; gap: 16px; padding: 16px 22px; }
  .presets-label { writing-mode: vertical-lr; transform: rotate(180deg); align-self: stretch; flex-shrink: 0; }
  .presets-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; flex: 1; }
  .preset-slot { display: flex; flex-direction: column; gap: 6px; padding: 10px 14px; border-radius: 10px; background: rgba(30, 41, 59, 0.6); border: 1px solid var(--color-border); }
  .preset-name { font-size: 12px; font-weight: 700; letter-spacing: 0.16em; text-transform: uppercase; color: var(--color-copy-soft); }
  .preset-actions { display: flex; gap: 6px; }
  .preset-btn { flex: 1; height: 36px; font-size: 12px; font-weight: 600; }
</style>
