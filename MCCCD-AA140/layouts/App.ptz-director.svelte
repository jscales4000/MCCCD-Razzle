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

  const cameras = ['Wide Shot', 'Presenter', 'Audience L', 'Audience R', 'Overhead'];
  let activeCamera = 0;

  let panSpeed = 50;
  let tiltSpeed = 50;
  let ismiActive = false;

  // Mic meter levels (0–100)
  let mic1Level = 65;
  let mic2Level = 24;

  const shotPresets = ['DEFAULT', 'PRIMARY', 'SECONDARY'];

  function selectCamera(idx: number) {
    activeCamera = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.cameraSelect, idx + 1);
    });
  }

  function ptzCommand(cmd: 'up' | 'down' | 'left' | 'right') {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      const sigMap: Record<string, string> = {
        up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
        left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
      };
      CrComLib.publishEvent('b', sigMap[cmd], true);
    });
  }

  function recallPreset(idx: number) {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.shotPresetRecall, idx + 1);
    });
  }

  function savePreset(idx: number) {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.shotPresetSave, idx + 1);
    });
  }

  function deletePreset(idx: number) {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.shotPresetDelete, idx + 1);
    });
  }

  function connect() {
    ismiActive = true;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.ismiConnect, true);
    });
  }

  function disconnect() {
    ismiActive = false;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.ismiConnect, false);
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
  <div class="app-shell layout-ptz">

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

    <div class="work-area">
      <!-- Camera list sidebar -->
      <div class="glass-card camera-list">
        <p class="panel-label">Cameras</p>
        {#each cameras as label, i}
          <button
            class="btn camera-btn"
            class:active={activeCamera === i}
            onclick={() => selectCamera(i)}
            aria-pressed={activeCamera === i}
          >{label}</button>
        {/each}
      </div>

      <!-- PTZ joystick -->
      <div class="glass-card ptz-panel">
        <p class="panel-label">PTZ Controls</p>
        <div class="joystick-area">
          <button class="btn joystick-btn up" onclick={() => ptzCommand('up')} aria-label="Tilt up">▲</button>
          <button class="btn joystick-btn left" onclick={() => ptzCommand('left')} aria-label="Pan left">◀</button>
          <div class="joystick-center" aria-hidden="true"></div>
          <button class="btn joystick-btn right" onclick={() => ptzCommand('right')} aria-label="Pan right">▶</button>
          <button class="btn joystick-btn down" onclick={() => ptzCommand('down')} aria-label="Tilt down">▼</button>
        </div>
        <div class="speed-controls">
          <label class="speed-label">
            Pan Speed
            <input type="range" class="speed-slider" min="1" max="100" bind:value={panSpeed} aria-label="Pan speed" />
          </label>
          <label class="speed-label">
            Tilt Speed
            <input type="range" class="speed-slider" min="1" max="100" bind:value={tiltSpeed} aria-label="Tilt speed" />
          </label>
        </div>
      </div>

      <!-- Live preview -->
      <div class="glass-card preview-panel">
        <p class="panel-label">Preview — {cameras[activeCamera]}</p>
        <div class="preview-area">
          <span class="preview-placeholder">[ Live Feed ]</span>
        </div>
      </div>

      <!-- System / mic meters -->
      <div class="glass-card system-panel">
        <p class="panel-label">System</p>
        <div class="meter-group">
          <span class="meter-label">MIC 1</span>
          <div class="meter-track">
            <div class="meter-fill" style="width: {mic1Level}%"></div>
          </div>
        </div>
        <div class="meter-group">
          <span class="meter-label">MIC 2</span>
          <div class="meter-track">
            <div class="meter-fill dim" style="width: {mic2Level}%"></div>
          </div>
        </div>
        <div class="ismi-status" class:active={ismiActive}>
          <span class="ismi-dot"></span>
          <span>{ismiActive ? 'ISMI Active' : 'ISMI Disabled'}</span>
        </div>
      </div>
    </div>

    <!-- Shot presets row -->
    <div class="shot-presets glass-card">
      <p class="panel-label presets-label">Shot Presets</p>
      <div class="presets-grid">
        {#each shotPresets as label, i}
          <div class="preset-slot">
            <div class="preset-header">
              <span class="preset-online-dot"></span>
              <span class="preset-name">{label}</span>
            </div>
            <div class="preset-actions">
              <button class="btn preset-btn active" onclick={() => savePreset(i)}>Save</button>
              <button class="btn preset-btn" onclick={() => recallPreset(i)}>Recall</button>
              <button class="btn preset-btn" onclick={() => deletePreset(i)}>Delete</button>
            </div>
          </div>
        {/each}
      </div>
    </div>

    <footer class="app-footer glass-card">
      <button class="btn connect-btn" class:active={ismiActive} onclick={connect} disabled={ismiActive}>
        Connect
      </button>
      <button class="btn connect-btn" onclick={disconnect} disabled={!ismiActive}>
        Disconnect
      </button>
      <span class="footer-right">{ismiActive ? '● Listening' : 'Start Listening'}</span>
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
  .layout-ptz {
    display: grid;
    grid-template-rows: 92px 1fr 180px 104px;
    gap: 16px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .work-area {
    display: grid;
    grid-template-columns: 160px 1fr 1fr 200px;
    gap: 16px;
    min-height: 0;
  }

  .camera-list {
    border-radius: var(--radius-panel);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 0;
    overflow-y: auto;
  }

  .camera-btn {
    width: 100%;
    height: 52px;
    font-size: 15px;
    font-weight: 600;
    text-align: left;
    padding: 0 14px;
  }

  .ptz-panel, .preview-panel, .system-panel {
    border-radius: var(--radius-panel);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    min-height: 0;
  }

  .panel-label {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    flex-shrink: 0;
  }

  .joystick-area {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    grid-template-rows: 1fr 1fr 1fr;
    gap: 8px;
    flex: 1;
    min-height: 0;
  }

  .joystick-btn {
    font-size: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .up    { grid-column: 2; grid-row: 1; }
  .left  { grid-column: 1; grid-row: 2; }
  .joystick-center {
    grid-column: 2; grid-row: 2;
    border-radius: 50%;
    background: var(--color-accent);
    opacity: 0.5;
    border: 1px solid var(--color-border);
  }
  .right { grid-column: 3; grid-row: 2; }
  .down  { grid-column: 2; grid-row: 3; }

  .speed-controls {
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex-shrink: 0;
  }

  .speed-label {
    display: flex;
    align-items: center;
    gap: 10px;
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }

  .speed-slider {
    flex: 1;
    accent-color: var(--color-accent);
  }

  .preview-area {
    flex: 1;
    background: #050d1a;
    border-radius: 12px;
    border: 1px solid var(--color-border);
    display: grid;
    place-items: center;
    min-height: 0;
  }

  .preview-placeholder {
    color: var(--color-copy-muted);
    font-size: 16px;
    font-weight: 600;
  }

  .meter-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .meter-label {
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
  }

  .meter-track {
    height: 18px;
    border-radius: 4px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    overflow: hidden;
  }

  .meter-fill {
    height: 100%;
    background: var(--color-success);
    opacity: 0.75;
    transition: width 120ms ease;
  }

  .meter-fill.dim {
    opacity: 0.5;
  }

  .ismi-status {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 10px;
    background: rgba(239, 68, 68, 0.12);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #fca5a5;
    font-size: 13px;
    font-weight: 600;
    margin-top: auto;
  }

  .ismi-status.active {
    background: rgba(34, 197, 94, 0.12);
    border-color: rgba(34, 197, 94, 0.3);
    color: #bbf7d0;
  }

  .ismi-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: currentColor;
  }

  .shot-presets {
    border-radius: var(--radius-panel);
    padding: 18px 22px;
    display: flex;
    gap: 20px;
    align-items: flex-start;
    min-height: 0;
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
    gap: 16px;
    flex: 1;
  }

  .preset-slot {
    border-radius: 12px;
    padding: 14px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .preset-header {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .preset-online-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--color-success);
  }

  .preset-name {
    color: var(--color-copy-soft);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }

  .preset-actions {
    display: flex;
    gap: 8px;
  }

  .preset-btn {
    flex: 1;
    height: 40px;
    font-size: 13px;
    font-weight: 600;
  }

  .connect-btn {
    height: 68px;
    padding: 0 36px;
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
</style>
