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

  // Zone faders — values 0–100 map to dB labels
  let zones = [
    { label: 'Zone 1', level: 60, muted: false },
    { label: 'Zone 2', level: 75, muted: false },
    { label: 'Zone 3', level: 45, muted: false },
  ];

  let progAudioLevel = 55;

  const routingSources = ['HDMI 1', 'HDMI 2'];
  let activeRoute = 0;

  function setZoneLevel(idx: number, e: Event) {
    zones[idx].level = Number((e.target as HTMLInputElement).value);
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', `${SIGNALS.zoneVolume}_${idx + 1}`, Math.round((zones[idx].level / 100) * 65535));
    });
  }

  function toggleZoneMute(idx: number) {
    zones[idx].muted = !zones[idx].muted;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', `${SIGNALS.zoneMute}_${idx + 1}`, zones[idx].muted);
    });
  }

  function setProgAudio(e: Event) {
    progAudioLevel = Number((e.target as HTMLInputElement).value);
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.progAudioLevel, Math.round((progAudioLevel / 100) * 65535));
    });
  }

  function selectRoute(idx: number) {
    activeRoute = idx;
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('n', SIGNALS.sourceSelect, idx + 1);
    });
  }

  function muteAll() {
    zones = zones.map(z => ({ ...z, muted: true }));
    import('../lib/CrComLib').then(({ CrComLib }) => {
      CrComLib.publishEvent('b', SIGNALS.muteAll, true);
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

  function levelToDb(level: number): string {
    if (level === 0) return '-∞';
    const db = (level / 100) * 12 - 12;
    return `${db >= 0 ? '+' : ''}${db.toFixed(0)} dB`;
  }
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell layout-audio">

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
      <!-- Zone fader columns -->
      {#each zones as zone, i}
        <div class="glass-card zone-panel">
          <p class="zone-label">{zone.label}</p>
          <div class="fader-track">
            <input
              type="range"
              class="fader"
              orient="vertical"
              min="0" max="100"
              value={zone.level}
              oninput={(e) => setZoneLevel(i, e)}
              aria-label="{zone.label} level"
            />
          </div>
          <span class="db-value" class:muted={zone.muted}>{zone.muted ? 'MUTE' : levelToDb(zone.level)}</span>
          <button
            class="btn mute-btn"
            class:active={zone.muted}
            onclick={() => toggleZoneMute(i)}
            aria-pressed={zone.muted}
          >{zone.muted ? 'Unmute' : 'Mute'}</button>
        </div>
      {/each}

      <!-- Right panel: prog audio + routing -->
      <div class="right-column">
        <div class="glass-card prog-panel">
          <p class="panel-label">Prog Audio</p>
          <input
            type="range"
            class="prog-slider"
            min="0" max="100"
            bind:value={progAudioLevel}
            oninput={setProgAudio}
            aria-label="Program audio level"
          />
          <span class="db-value">{levelToDb(progAudioLevel)}</span>
        </div>
        <div class="glass-card routing-panel">
          <p class="panel-label">Routing</p>
          {#each routingSources as label, i}
            <button
              class="btn route-btn"
              class:active={activeRoute === i}
              onclick={() => selectRoute(i)}
              aria-pressed={activeRoute === i}
            >{label}</button>
          {/each}
        </div>
      </div>
    </main>

    <footer class="app-footer glass-card">
      <button class="btn footer-btn">Sources</button>
      <button class="btn footer-btn" onclick={muteAll}>Mute All</button>
      <span class="footer-right">Prog Audio</span>
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
  .layout-audio {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }

  .main-area {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr 280px;
    gap: 20px;
    min-height: 0;
  }

  .zone-panel {
    border-radius: var(--radius-panel);
    padding: 22px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 14px;
    min-height: 0;
  }

  .zone-label {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
  }

  .fader-track {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 0;
  }

  .fader {
    writing-mode: vertical-lr;
    direction: rtl;
    width: 44px;
    height: 100%;
    accent-color: var(--color-accent);
  }

  .db-value {
    color: var(--color-copy-soft);
    font-size: 14px;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
  }

  .db-value.muted {
    color: var(--color-danger);
  }

  .mute-btn {
    width: 100%;
    height: 48px;
    font-size: 16px;
    font-weight: 600;
  }

  .right-column {
    display: flex;
    flex-direction: column;
    gap: 20px;
    min-height: 0;
  }

  .prog-panel, .routing-panel {
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

  .prog-slider {
    width: 100%;
    accent-color: var(--color-accent);
  }

  .route-btn {
    height: 52px;
    font-size: 18px;
    font-weight: 600;
  }

  .footer-btn {
    height: 68px;
    padding: 0 32px;
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
