<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    panelOnline,
    display1SourceFb, display2SourceFb, display3SourceFb,
    audioOutputSelectFb,
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import DisplayTile from '../components/DisplayTile.svelte';

  // Preview dock (browser-dev only) — same pattern as the original scaffold App.svelte
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

  // Mirror buttons fire pulse signals; SIMPL# does the actual one-shot copy
  function mirrorD1ToD3() { pulseDigital(SIGNALS.d1MirrorToD3); }
  function mirrorD2ToD3() { pulseDigital(SIGNALS.d2MirrorToD3); }

  function setAudioOutput(v: 1 | 2) {
    publishAnalog(SIGNALS.audioOutputSelect, v);
  }

  function volDown()      { pulseDigital(SIGNALS.volumeDown); }
  function volUp()        { pulseDigital(SIGNALS.volumeUp); }
  function toggleMaster() { pulseDigital(SIGNALS.muteAll); }

  function toggleLavMute() {
    const next = !$micLavMuteFb;
    publishDigital(SIGNALS.micLavMute, next);
  }
  function toggleHandheldMute() {
    const next = !$micHandheldMuteFb;
    publishDigital(SIGNALS.micHandheldMute, next);
  }

  function systemPower() { pulseDigital(SIGNALS.displayPower); }

  function occupancyText(): string {
    if ($occupancyState === 1) return 'Occupied';
    if ($occupancyState === 2) return `Vacant — shutdown in ${$shutdownCountdown} min`;
    return 'Vacant';
  }
  function occupancyClass(): string {
    if ($occupancyState === 1) return 'occupancy-pill ok';
    if ($occupancyState === 2) return 'occupancy-pill warn';
    return 'occupancy-pill idle';
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
  <div class="app-shell layout-home">

    <header class="app-header glass-card">
      <div class="header-copy">
        <p class="eyebrow">CH5 Touch Panel</p>
        <h1>{ROOM_NAME}</h1>
      </div>
      <div class="header-right">
        <div class={occupancyClass()} aria-live="polite">{occupancyText()}</div>
        <div class="status-pill" class:online={$panelOnline} aria-live="polite">
          <span class="status-dot"></span>
          <span>{$panelOnline ? 'Online' : 'Offline'}</span>
        </div>
      </div>
    </header>

    <main class="display-row">
      <DisplayTile
        label="Display 1"
        sourceSetSignal={SIGNALS.display1Source}
        activeSourceFb={$display1SourceFb}
        audioActive={$audioOutputSelectFb === 1}
        onAudioToggle={() => setAudioOutput(1)}
        onMirrorToD3={mirrorD1ToD3}
      />
      <DisplayTile
        label="Display 2"
        sourceSetSignal={SIGNALS.display2Source}
        activeSourceFb={$display2SourceFb}
        audioActive={$audioOutputSelectFb === 2}
        onAudioToggle={() => setAudioOutput(2)}
        onMirrorToD3={mirrorD2ToD3}
      />
      <DisplayTile
        label="Display 3"
        sourceSetSignal={SIGNALS.display3Source}
        activeSourceFb={$display3SourceFb}
      />
    </main>

    <footer class="app-footer glass-card">
      <button class="btn power-btn" onclick={systemPower}>⏻ Power</button>

      <div class="vol-group">
        <span class="footer-label">PROGRAM</span>
        <button class="btn footer-btn" onclick={volDown}>Vol −</button>
        <button class="btn footer-btn" onclick={toggleMaster}>Mute</button>
        <button class="btn footer-btn" onclick={volUp}>Vol +</button>
      </div>

      <div class="mic-group">
        <span class="footer-label">MICS</span>
        <button class="btn footer-btn" class:active={$micLavMuteFb} onclick={toggleLavMute}>
          {$micLavMuteFb ? '🔇 Lav' : 'Lav'}
        </button>
        <button class="btn footer-btn" class:active={$micHandheldMuteFb} onclick={toggleHandheldMute}>
          {$micHandheldMuteFb ? '🔇 Handheld' : 'Handheld'}
        </button>
      </div>

      <button class="btn camera-btn" onclick={() => goToPage('cameras')}>📷 Cameras</button>
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
  .layout-home {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .occupancy-pill {
    padding: 6px 14px;
    border-radius: 999px;
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    border: 1px solid var(--color-border);
  }
  .occupancy-pill.ok {
    background: rgba(34, 197, 94, 0.15);
    border-color: rgba(34, 197, 94, 0.4);
    color: #bbf7d0;
  }
  .occupancy-pill.warn {
    background: rgba(239, 68, 68, 0.15);
    border-color: rgba(239, 68, 68, 0.4);
    color: #fecaca;
  }
  .occupancy-pill.idle {
    background: rgba(245, 158, 11, 0.12);
    border-color: rgba(245, 158, 11, 0.3);
    color: #fed7aa;
  }
  .display-row {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 16px;
    min-height: 0;
  }
  .footer-label {
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    margin-right: 8px;
  }
  .power-btn { height: 64px; padding: 0 24px; font-size: 18px; font-weight: 700; }
  .vol-group, .mic-group {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .footer-btn { height: 56px; padding: 0 16px; font-size: 14px; font-weight: 600; }
  .footer-btn.active {
    background: rgba(239, 68, 68, 0.25);
    border-color: rgba(239, 68, 68, 0.5);
    color: #fecaca;
  }
  .camera-btn { height: 64px; padding: 0 24px; font-size: 18px; font-weight: 700; margin-left: auto; }
</style>
