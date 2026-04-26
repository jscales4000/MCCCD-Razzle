<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import { panelOnline, camTrackingModeFb } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import { CAMERAS, rtspMain, type Camera } from '../lib/cameras';
  import PresetButton from '../components/PresetButton.svelte';

  let activeCamera: Camera = $state(CAMERAS[0]);

  let panSpeed = $state(50);
  let tiltSpeed = $state(50);

  const presets = [
    { idx: 1, name: 'Default' },
    { idx: 2, name: 'Primary' },
    { idx: 3, name: 'Secondary' },
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

  // PTZ press-and-hold (rising edge starts movement, falling edge stops)
  function ptzStart(dir: 'up' | 'down' | 'left' | 'right') {
    const map = {
      up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
      left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
    };
    publishDigital(map[dir], true);
  }
  function ptzEnd(dir: 'up' | 'down' | 'left' | 'right') {
    const map = {
      up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
      left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
    };
    publishDigital(map[dir], false);
  }

  // Zoom press-and-hold
  function zoomStart(dir: 'in' | 'out') {
    publishDigital(dir === 'in' ? SIGNALS.zoomIn : SIGNALS.zoomOut, true);
  }
  function zoomEnd(dir: 'in' | 'out') {
    publishDigital(dir === 'in' ? SIGNALS.zoomIn : SIGNALS.zoomOut, false);
  }

  function recallPreset(i: number) { publishAnalog(SIGNALS.shotPresetRecall, i); }
  function savePreset(i: number)   { publishAnalog(SIGNALS.shotPresetSave, i); }

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
            <button
              class="icon-btn ptz-btn ptz-up"
              aria-label="Tilt up"
              onmousedown={() => ptzStart('up')}
              onmouseup={() => ptzEnd('up')}
              onmouseleave={() => ptzEnd('up')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('up'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('up'); }}
              ontouchcancel={() => ptzEnd('up')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M12 5l7 8H5z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-left"
              aria-label="Pan left"
              onmousedown={() => ptzStart('left')}
              onmouseup={() => ptzEnd('left')}
              onmouseleave={() => ptzEnd('left')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('left'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('left'); }}
              ontouchcancel={() => ptzEnd('left')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M5 12l8-7v14z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-right"
              aria-label="Pan right"
              onmousedown={() => ptzStart('right')}
              onmouseup={() => ptzEnd('right')}
              onmouseleave={() => ptzEnd('right')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('right'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('right'); }}
              ontouchcancel={() => ptzEnd('right')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M19 12l-8-7v14z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-down"
              aria-label="Tilt down"
              onmousedown={() => ptzStart('down')}
              onmouseup={() => ptzEnd('down')}
              onmouseleave={() => ptzEnd('down')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('down'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('down'); }}
              ontouchcancel={() => ptzEnd('down')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M12 19l-7-8h14z" fill="currentColor"/>
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Right-side controls -->
      <div class="glass-card controls-panel">
        <div class="speed-block">
          <label class="speed-label">
            <span>Pan Speed</span>
            <input type="range" class="slider" min="1" max="100" bind:value={panSpeed} aria-label="Pan speed" />
            <span class="readout">{panSpeed}</span>
          </label>
          <label class="speed-label">
            <span>Tilt Speed</span>
            <input type="range" class="slider" min="1" max="100" bind:value={tiltSpeed} aria-label="Tilt speed" />
            <span class="readout">{tiltSpeed}</span>
          </label>
        </div>

        <div class="zoom-block">
          <span class="block-label">Zoom</span>
          <button
            class="icon-btn zoom-btn"
            aria-label="Zoom in"
            onmousedown={() => zoomStart('in')}
            onmouseup={() => zoomEnd('in')}
            onmouseleave={() => zoomEnd('in')}
            ontouchstart={(e) => { e.preventDefault(); zoomStart('in'); }}
            ontouchend={(e) => { e.preventDefault(); zoomEnd('in'); }}
            ontouchcancel={() => zoomEnd('in')}
          >
            <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
              <circle cx="11" cy="11" r="6" stroke="currentColor" stroke-width="2" fill="none"/>
              <path d="M11 8v6M8 11h6M16.5 16.5L21 21" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
            </svg>
          </button>
          <button
            class="icon-btn zoom-btn"
            aria-label="Zoom out"
            onmousedown={() => zoomStart('out')}
            onmouseup={() => zoomEnd('out')}
            onmouseleave={() => zoomEnd('out')}
            ontouchstart={(e) => { e.preventDefault(); zoomStart('out'); }}
            ontouchend={(e) => { e.preventDefault(); zoomEnd('out'); }}
            ontouchcancel={() => zoomEnd('out')}
          >
            <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
              <circle cx="11" cy="11" r="6" stroke="currentColor" stroke-width="2" fill="none"/>
              <path d="M8 11h6M16.5 16.5L21 21" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
            </svg>
          </button>
        </div>

        <button class="btn vtc-btn" onclick={sendToVtc}>Send to VTC</button>

        <div class="tracking-block">
          <p class="block-label">Tracking Mode</p>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 1} onclick={() => setTrackingMode(1)}>People</button>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 2} onclick={() => setTrackingMode(2)}>Group</button>
          <button class="btn tracking-btn" class:active={$camTrackingModeFb === 3} onclick={() => setTrackingMode(3)}>VX AutoSwitch</button>
        </div>
      </div>

    </div>

    <!-- Presets row -->
    <div class="presets-row glass-card">
      <p class="block-label presets-label">Shot Presets</p>
      <div class="presets-grid">
        {#each presets as p}
          <PresetButton
            label={p.name}
            onRecall={() => recallPreset(p.idx)}
            onSave={() => savePreset(p.idx)}
          />
        {/each}
      </div>
      <p class="hint-row">Tap to recall · Hold 3 seconds to save</p>
    </div>

  </div>

  {#if showPreviewDock}
    <aside class="preview-dock glass-card" aria-label="Local resolution preview controls">
      <div class="preview-copy">
        <strong>{profileLabel}</strong>
        <span>{viewportLabel} · {scaleLabel}</span>
      </div>
      <div class="preview-actions">
        <button class="btn preview-button" class:active={previewMode === 'auto'} onclick={() => setPreviewMode('auto')}>Auto</button>
        <button class="btn preview-button" class:active={previewMode === 'tsw770'} onclick={() => setPreviewMode('tsw770')}>770</button>
        <button class="btn preview-button" class:active={previewMode === 'tsw1070'} onclick={() => setPreviewMode('tsw1070')}>1070</button>
      </div>
    </aside>
  {/if}
</div>

<style>
  .layout-cameras {
    display: grid;
    grid-template-rows: 92px 1fr 168px;
    gap: 16px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .back-btn { min-height: 56px; padding: 0 18px; font-size: 13px; margin-right: 16px; }

  .work-area {
    display: grid;
    grid-template-columns: 180px 1fr 240px;
    gap: 16px;
    min-height: 0;
  }

  .camera-sidebar {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .panel-label,
  .block-label {
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
    min-height: 56px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
  }
  .camera-select-btn strong { font-size: 16px; font-weight: 700; }
  .camera-select-btn em { font-style: normal; font-size: 12px; color: var(--color-copy-muted); }

  .preview-panel {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 0;
  }

  .video-container {
    position: relative;
    flex: 1;
    border-radius: var(--radius-button);
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

  /* Transparent PTZ overlay buttons — flat white icons, no colored borders. */
  .ptz-overlay {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }
  .ptz-btn {
    pointer-events: auto;
    position: absolute;
    width: 60px;
    height: 60px;
    color: #ffffff;
    background: transparent;
    border: none;
    border-radius: 50%;
  }
  .ptz-btn:active {
    background: rgba(255, 255, 255, 0.08);
    transform: scale(0.92);
    box-shadow: inset 0 0 0 1px var(--color-accent);
  }
  .ptz-up    { top: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-down  { bottom: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-up:active    { transform: translateX(-50%) scale(0.92); }
  .ptz-down:active  { transform: translateX(-50%) scale(0.92); }
  .ptz-left  { left: 12px; top: 50%; transform: translateY(-50%); }
  .ptz-right { right: 12px; top: 50%; transform: translateY(-50%); }
  .ptz-left:active  { transform: translateY(-50%) scale(0.92); }
  .ptz-right:active { transform: translateY(-50%) scale(0.92); }

  .controls-panel {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  .speed-block { display: flex; flex-direction: column; gap: 12px; }
  .speed-label {
    display: grid;
    grid-template-columns: 1fr 1fr 36px;
    gap: 8px;
    align-items: center;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .slider {
    accent-color: var(--color-accent);
  }
  .readout {
    text-align: right;
    color: var(--color-copy-soft);
    font-size: 13px;
    font-variant-numeric: tabular-nums;
  }

  .zoom-block {
    display: grid;
    grid-template-columns: 1fr auto auto;
    align-items: center;
    gap: 8px;
  }
  .zoom-btn {
    width: 56px;
    height: 56px;
  }

  .vtc-btn {
    min-height: 60px;
    font-size: 14px;
    font-weight: 700;
  }

  .tracking-block { display: flex; flex-direction: column; gap: 8px; }
  .tracking-btn {
    min-height: 48px;
    text-align: left;
    padding: 0 14px;
    font-weight: 600;
  }

  .presets-row {
    display: flex;
    gap: 16px;
    padding: 16px 22px;
    align-items: stretch;
  }
  .presets-label {
    writing-mode: vertical-lr;
    transform: rotate(180deg);
    align-self: stretch;
    flex-shrink: 0;
  }
  .presets-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    flex: 1;
  }
  .hint-row {
    align-self: center;
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    writing-mode: vertical-rl;
  }
</style>
