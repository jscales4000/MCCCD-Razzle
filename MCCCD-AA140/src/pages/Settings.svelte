<script lang="ts">
  import { onMount } from 'svelte';
  import { ROOM_NAME, SIGNALS } from '../lib/contract';
  import { goToPage } from '../lib/stores/page';
  import {
    panelOnline,
    micLavConnected, micHandheldConnected,
    micCeiling1Connected, micCeiling2Connected, micCeiling3Connected,
    micLavLevel, micHandheldLevel,
    micCeiling1Level, micCeiling2Level, micCeiling3Level,
    micLavTrimFb, micHandheldTrimFb,
    micCeiling1TrimFb, micCeiling2TrimFb, micCeiling3TrimFb,
    micLavLineOutFb, micHandheldLineOutFb,
    micCeiling1LineOutFb, micCeiling2LineOutFb, micCeiling3LineOutFb,
    micCeiling1MuteFb, micCeiling2MuteFb, micCeiling3MuteFb,
  } from '../lib/stores/signals';
  import MicChannel from '../components/MicChannel.svelte';

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
  <title>{ROOM_NAME} Settings</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell layout-settings">

    <header class="app-header glass-card">
      <button class="btn back-btn" onclick={() => goToPage('home')} aria-label="Back to home">← Home</button>
      <div class="header-copy">
        <p class="eyebrow">CH5 Touch Panel</p>
        <h1>{ROOM_NAME} — Settings</h1>
      </div>
      <div class="status-pill" class:online={$panelOnline}>
        <span class="status-dot"></span>
        <span>{$panelOnline ? 'Online' : 'Offline'}</span>
      </div>
    </header>

    <main class="settings-main">
      <section class="section">
        <p class="section-title">Microphone Management</p>
        <div class="mic-grid">
          <MicChannel
            name="Lavalier"
            connectedFb={micLavConnected}
            levelFb={micLavLevel}
            trimFb={micLavTrimFb}
            trimSetSignal={SIGNALS.micLavTrim}
            lineOutFb={micLavLineOutFb}
            lineOutSetSignal={SIGNALS.micLavLineOut}
          />
          <MicChannel
            name="Handheld"
            connectedFb={micHandheldConnected}
            levelFb={micHandheldLevel}
            trimFb={micHandheldTrimFb}
            trimSetSignal={SIGNALS.micHandheldTrim}
            lineOutFb={micHandheldLineOutFb}
            lineOutSetSignal={SIGNALS.micHandheldLineOut}
          />
          <MicChannel
            name="Ceiling 1 (TCCM)"
            connectedFb={micCeiling1Connected}
            levelFb={micCeiling1Level}
            trimFb={micCeiling1TrimFb}
            trimSetSignal={SIGNALS.micCeiling1Trim}
            lineOutFb={micCeiling1LineOutFb}
            lineOutSetSignal={SIGNALS.micCeiling1LineOut}
            muteFb={micCeiling1MuteFb}
            muteSetSignal={SIGNALS.micCeiling1Mute}
          />
          <MicChannel
            name="Ceiling 2 (TCCM)"
            connectedFb={micCeiling2Connected}
            levelFb={micCeiling2Level}
            trimFb={micCeiling2TrimFb}
            trimSetSignal={SIGNALS.micCeiling2Trim}
            lineOutFb={micCeiling2LineOutFb}
            lineOutSetSignal={SIGNALS.micCeiling2LineOut}
            muteFb={micCeiling2MuteFb}
            muteSetSignal={SIGNALS.micCeiling2Mute}
          />
          <MicChannel
            name="Ceiling 3 (TCCM)"
            connectedFb={micCeiling3Connected}
            levelFb={micCeiling3Level}
            trimFb={micCeiling3TrimFb}
            trimSetSignal={SIGNALS.micCeiling3Trim}
            lineOutFb={micCeiling3LineOutFb}
            lineOutSetSignal={SIGNALS.micCeiling3LineOut}
            muteFb={micCeiling3MuteFb}
            muteSetSignal={SIGNALS.micCeiling3Mute}
          />
        </div>
      </section>
    </main>

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
  .layout-settings {
    display: grid;
    grid-template-rows: 92px 1fr;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .back-btn { min-height: 56px; padding: 0 18px; font-size: 13px; margin-right: 16px; }
  .settings-main {
    overflow-y: auto;
    padding-right: 4px;
  }
  .section {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .section-title {
    margin: 0;
    color: var(--color-copy-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }
  .mic-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 14px;
  }
</style>
