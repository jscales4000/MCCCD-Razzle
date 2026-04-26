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

  const displays = [
    'Display 1', 'Display 2', 'Display 3', 'Display 4',
    'Display 5', 'Outside', 'Inside Disp.', 'Display All',
  ];

  const sources = [
    { label: 'AirMedia', subtitle: 'Wireless' },
    { label: 'Cable Box', subtitle: 'Cable TV' },
    { label: 'HDMI/USB-C', subtitle: 'Wired' },
    { label: 'Zoom', subtitle: 'Video conf.' },
  ];

  let multiSelectMode = false;
  let selectedDisplays: Set<number> = new Set();
  let activeDisplay: number | null = null;
  let showAudioRouting = false;

  function toggleMultiSelect() {
    multiSelectMode = !multiSelectMode;
    if (!multiSelectMode) selectedDisplays = new Set();
  }

  function selectDisplay(idx: number) {
    if (multiSelectMode) {
      const next = new Set(selectedDisplays);
      if (next.has(idx)) next.delete(idx);
      else next.add(idx);
      selectedDisplays = next;
    } else {
      activeDisplay = idx;
    }
  }

  function isDisplayActive(idx: number): boolean {
    return multiSelectMode ? selectedDisplays.has(idx) : activeDisplay === idx;
  }

  function routeSource(sourceIdx: number) {
    import('../lib/CrComLib').then(({ CrComLib }) => {
      if (multiSelectMode) {
        selectedDisplays.forEach(d => {
          CrComLib.publishEvent('n', `${SIGNALS.displayRoute}_${d + 1}`, sourceIdx + 1);
        });
      } else if (activeDisplay !== null) {
        CrComLib.publishEvent('n', `${SIGNALS.displayRoute}_${activeDisplay + 1}`, sourceIdx + 1);
      }
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
  <div class="app-shell layout-multi-routing">

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
      <!-- Display list sidebar -->
      <div class="glass-card display-sidebar">
        <button
          class="btn multi-select-btn"
          class:active={multiSelectMode}
          onclick={toggleMultiSelect}
          aria-pressed={multiSelectMode}
        >
          {multiSelectMode ? '✓ Multi-Select' : 'Multi-Select'}
        </button>
        <div class="display-list">
          {#each displays as label, i}
            <button
              class="btn display-btn"
              class:active={isDisplayActive(i)}
              class:special={i >= 6}
              onclick={() => selectDisplay(i)}
              aria-pressed={isDisplayActive(i)}
            >{label}</button>
          {/each}
        </div>
        <button
          class="btn audio-routing-btn"
          class:active={showAudioRouting}
          onclick={() => showAudioRouting = !showAudioRouting}
        >Audio Routing</button>
      </div>

      <!-- Guide / content center -->
      <div class="glass-card guide-panel">
        <div class="accent-bar"></div>
        <div class="welcome-header">
          <span class="welcome-icon" aria-hidden="true">⚜</span>
          <div>
            <h2 class="welcome-title">Welcome — {ROOM_NAME}</h2>
            <p class="welcome-sub">Control System Guide</p>
          </div>
        </div>

        <div class="steps-grid">
          <div class="step-card">
            <div class="step-number">
              <span>1</span>
            </div>
            <div class="step-body">
              <h3 class="step-heading">Select Display</h3>
              <p class="step-copy">Choose display(s) from the sidebar buttons on the left.</p>
              <div class="step-hint">
                <strong>Quick Options:</strong> Use Inside for interior or All Displays for the full room.
              </div>
              <div class="step-hint">
                <strong>Multi-Display:</strong> Click Multi-Select, then pick one source to route to all selected screens.
              </div>
            </div>
          </div>

          <div class="step-card">
            <div class="step-number">
              <span>2</span>
            </div>
            <div class="step-body">
              <h3 class="step-heading">Choose Source</h3>
              <p class="step-copy">Select content source:</p>
              <div class="source-list">
                {#each sources as src}
                  <button
                    class="btn source-route-btn"
                    onclick={() => routeSource(sources.indexOf(src))}
                    disabled={activeDisplay === null && !multiSelectMode}
                  >
                    <span class="source-arrow">▶</span>
                    <span>
                      <strong>{src.label}</strong>
                      <em>{src.subtitle}</em>
                    </span>
                  </button>
                {/each}
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <footer class="app-footer glass-card">
      <div class="footer-displays">
        <span class="footer-label">INSIDE</span>
        <span class="footer-label">OUTSIDE</span>
      </div>
      <button class="btn power-circle" onclick={systemPower} aria-label="System power">⏻</button>
      <div class="footer-audio">
        <span class="footer-label">VOLUME</span>
        <span class="footer-label">MUTE</span>
        <span class="footer-label">VOLUME</span>
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
  .layout-multi-routing {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 20px;
    min-height: 0;
  }

  .display-sidebar {
    border-radius: var(--radius-panel);
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 0;
  }

  .multi-select-btn {
    width: 100%;
    height: 52px;
    font-size: 16px;
    font-weight: 700;
  }

  .multi-select-btn.active {
    background: var(--color-accent);
    color: #0b1220;
    border-color: var(--color-accent);
  }

  .display-list {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 8px;
    overflow-y: auto;
    min-height: 0;
  }

  .display-btn {
    width: 100%;
    height: 44px;
    font-size: 14px;
    font-weight: 600;
    text-align: left;
    padding: 0 12px;
  }

  .display-btn.special {
    border-color: rgba(140, 29, 64, 0.5);
    background: rgba(140, 29, 64, 0.15);
  }

  .display-btn.special.active {
    background: rgba(140, 29, 64, 0.4);
    border-color: rgba(140, 29, 64, 0.8);
  }

  .audio-routing-btn {
    width: 100%;
    height: 48px;
    font-size: 14px;
    font-weight: 700;
    background: rgba(140, 29, 64, 0.3);
    border-color: rgba(140, 29, 64, 0.5);
    color: #fecdd3;
  }

  .guide-panel {
    position: relative;
    border-radius: var(--radius-panel);
    padding: 28px;
    display: flex;
    flex-direction: column;
    gap: 20px;
    min-height: 0;
    overflow-y: auto;
  }

  .accent-bar {
    position: absolute;
    top: 0; left: 0; right: 0;
    height: 4px;
    border-radius: var(--radius-panel) var(--radius-panel) 0 0;
    background: var(--color-accent);
    opacity: 0.55;
  }

  .welcome-header {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .welcome-icon {
    font-size: 36px;
    color: var(--color-accent);
    flex-shrink: 0;
  }

  .welcome-title {
    margin: 0 0 4px;
    font-size: 26px;
    font-weight: 700;
    color: var(--color-accent);
  }

  .welcome-sub {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 14px;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
  }

  .steps-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
    flex: 1;
    min-height: 0;
  }

  .step-card {
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid var(--color-border);
    border-radius: 16px;
    padding: 20px;
    display: flex;
    gap: 14px;
  }

  .step-number {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: var(--color-accent);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .step-number span {
    color: #0b1220;
    font-size: 18px;
    font-weight: 800;
  }

  .step-body {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .step-heading {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--color-accent);
  }

  .step-copy {
    margin: 0;
    color: var(--color-copy-soft);
    font-size: 14px;
  }

  .step-hint {
    background: rgba(56, 189, 248, 0.08);
    border: 1px solid rgba(56, 189, 248, 0.18);
    border-radius: 8px;
    padding: 8px 12px;
    color: var(--color-copy-soft);
    font-size: 13px;
    line-height: 1.5;
  }

  .source-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .source-route-btn {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    height: auto;
    text-align: left;
    font-size: 15px;
  }

  .source-route-btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .source-route-btn strong {
    display: block;
    font-weight: 700;
  }

  .source-route-btn em {
    display: block;
    font-style: normal;
    color: var(--color-copy-muted);
    font-size: 12px;
  }

  .source-arrow {
    color: var(--color-accent);
    font-size: 12px;
    flex-shrink: 0;
  }

  .footer-displays {
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 4px;
  }

  .footer-label {
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
  }

  .power-circle {
    width: 68px;
    height: 68px;
    border-radius: 50%;
    font-size: 28px;
    background: rgba(140, 29, 64, 0.7);
    border-color: rgba(140, 29, 64, 0.9);
    color: #fecdd3;
    margin: 0 auto;
  }

  .footer-audio {
    display: flex;
    align-items: center;
    gap: 24px;
    margin-left: auto;
  }
</style>
