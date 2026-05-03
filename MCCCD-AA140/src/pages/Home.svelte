<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    panelOnline,
    display1SourceFb, display2SourceFb, display3SourceFb,
    display1PowerFb, display2PowerFb, display3PowerFb,
    systemPowerFb,
    audioOutputSelectFb,
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import DisplayTile from '../components/DisplayTile.svelte';
  import ConfirmShutdownModal from '../components/ConfirmShutdownModal.svelte';
  import HomeSplash from '../components/HomeSplash.svelte';

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

  // Power confirmation modal
  let showShutdownModal = $state(false);

  // Optimistic power-on flag for offline / standalone mode.
  // When SIMPL is connected, $systemPowerFb is the source of truth; this flag
  // never blocks the on-state UI because the {#if} also accepts userPoweredOn.
  // When SIMPL is offline (panel can't get systemPowerFb=true from the
  // processor), this flag still lets the user dismiss the splash by tapping
  // Start, otherwise the panel is stuck on the splash forever.
  // Reset on shutdown-confirm so the splash returns when the user shuts down.
  let userPoweredOn = $state(false);
  let systemOn = $derived($systemPowerFb || userPoweredOn);

  function mirrorD1ToD3() { pulseDigital(SIGNALS.d1MirrorToD3); }
  function mirrorD2ToD3() { pulseDigital(SIGNALS.d2MirrorToD3); }

  function setAudioOutput(v: 1 | 2) {
    publishAnalog(SIGNALS.audioOutputSelect, v);
  }

  function volDown()      { pulseDigital(SIGNALS.volumeDown); }
  function volUp()        { pulseDigital(SIGNALS.volumeUp); }
  function toggleMaster() { pulseDigital(SIGNALS.muteAll); }

  function toggleLavMute() {
    publishDigital(SIGNALS.micLavMute, !$micLavMuteFb);
  }
  function toggleHandheldMute() {
    publishDigital(SIGNALS.micHandheldMute, !$micHandheldMuteFb);
  }

  function powerButtonTapped() {
    if (systemOn) {
      // System is ON — open confirmation modal (do not pulse yet)
      showShutdownModal = true;
    } else {
      // System is OFF — power up immediately, no confirmation
      userPoweredOn = true;
      pulseDigital(SIGNALS.displayPower);
    }
  }

  function powerOnFromSplash() {
    userPoweredOn = true;
    pulseDigital(SIGNALS.displayPower);
  }

  function confirmShutdown() {
    showShutdownModal = false;
    userPoweredOn = false;
    pulseDigital(SIGNALS.displayPower);
  }
  function cancelShutdown() {
    showShutdownModal = false;
  }

  function occupancyText(): string {
    if ($occupancyState === 1) return 'Occupied';
    if ($occupancyState === 2) return `Vacant — shutdown in ${$shutdownCountdown} min`;
    return 'Vacant';
  }
  function occupancyClass(): string {
    if ($occupancyState === 1) return 'occupancy-block ok';
    if ($occupancyState === 2) return 'occupancy-block warn';
    return 'occupancy-block idle';
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
  <div class="app-shell layout-home" class:splash-mode={!systemOn}>

    {#if systemOn}
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
        powerOn={$display1PowerFb}
        audioActive={$audioOutputSelectFb === 1}
        onAudioToggle={() => setAudioOutput(1)}
        onMirrorToD3={mirrorD1ToD3}
      />
      <DisplayTile
        label="Display 2"
        sourceSetSignal={SIGNALS.display2Source}
        activeSourceFb={$display2SourceFb}
        powerOn={$display2PowerFb}
        audioActive={$audioOutputSelectFb === 2}
        onAudioToggle={() => setAudioOutput(2)}
        onMirrorToD3={mirrorD2ToD3}
      />
      <DisplayTile
        label="Display 3"
        sourceSetSignal={SIGNALS.display3Source}
        activeSourceFb={$display3SourceFb}
        powerOn={$display3PowerFb}
      />
    </main>

    <footer class="app-footer glass-card">
      <button
        class="btn power-btn"
        class:primary={systemOn}
        onclick={powerButtonTapped}
        aria-label={systemOn ? 'System on — tap to shut down' : 'System off — tap to power on'}
      >
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path d="M12 3v9" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" fill="none"/>
          <path d="M6.5 7.5a8 8 0 1 0 11 0" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" fill="none"/>
        </svg>
        <span>{systemOn ? 'System On' : 'Power'}</span>
      </button>

      <div class="vol-group">
        <span class="footer-label">PROGRAM</span>
        <button class="btn footer-btn" onclick={volDown}>Vol −</button>
        <button class="btn footer-btn" onclick={toggleMaster}>Mute</button>
        <button class="btn footer-btn" onclick={volUp}>Vol +</button>
      </div>

      <div class="mic-group">
        <span class="footer-label">MICS</span>
        <button class="btn footer-btn" class:active={$micLavMuteFb} onclick={toggleLavMute}>
          {$micLavMuteFb ? 'Lav (muted)' : 'Lav'}
        </button>
        <button class="btn footer-btn" class:active={$micHandheldMuteFb} onclick={toggleHandheldMute}>
          {$micHandheldMuteFb ? 'Handheld (muted)' : 'Handheld'}
        </button>
      </div>

      <div class="nav-group">
        <button class="btn nav-btn" onclick={() => goToPage('cameras')} aria-label="Open cameras page">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M4 7h4l2-2h4l2 2h4v12H4z" stroke="currentColor" stroke-width="1.8" fill="none" stroke-linejoin="round"/>
            <circle cx="12" cy="13" r="3.6" stroke="currentColor" stroke-width="1.8" fill="none"/>
          </svg>
          <span>Cameras</span>
        </button>
        <button class="btn nav-btn" onclick={() => goToPage('audio')} aria-label="Open audio mixer page">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M11 5L6 9H2v6h4l5 4z" stroke="currentColor" stroke-width="1.8" fill="none" stroke-linejoin="round"/>
            <path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07" stroke="currentColor" stroke-width="1.8" fill="none"/>
          </svg>
          <span>Audio</span>
        </button>
      </div>
    </footer>
    {:else}
    <HomeSplash
      roomName={ROOM_NAME}
      panelOnline={$panelOnline}
      occupancyState={$occupancyState}
      shutdownCountdown={$shutdownCountdown}
      onPowerOn={powerOnFromSplash}
    />
    {/if}

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

<ConfirmShutdownModal
  open={showShutdownModal}
  countdown={30}
  vacancyMinutes={$occupancyState === 2 ? $shutdownCountdown : undefined}
  shutdownItems={[
    { icon: 'display', label: '3 Displays (D1 Front Left, D2 Front Right, D3 Rear)' },
    { icon: 'audio',   label: 'Audio system + all 5 microphone channels' },
    { icon: 'camera',  label: 'Camera system (2 PTZ cameras)' },
  ]}
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>

<style>
  .layout-home {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .layout-home.splash-mode {
    display: block;
    padding: 0;
  }
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  /* Occupancy block — explicitly NOT a pill (small radius, rectangular). */
  .occupancy-block {
    padding: 10px 14px;
    border-radius: var(--radius-button);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    border: 1px solid var(--color-border);
  }
  .occupancy-block.ok {
    background: rgba(34, 197, 94, 0.12);
    border-color: rgba(34, 197, 94, 0.4);
    color: #bbf7d0;
  }
  .occupancy-block.warn {
    background: rgba(239, 68, 68, 0.12);
    border-color: rgba(239, 68, 68, 0.4);
    color: #fecaca;
  }
  .occupancy-block.idle {
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

  .power-btn {
    min-width: 132px;
  }

  .vol-group, .mic-group, .nav-group {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .footer-btn { min-height: 56px; padding: 0 16px; font-size: 13px; }
  .nav-btn { min-height: 56px; padding: 0 18px; font-size: 13px; }
  .nav-group { margin-left: auto; }
</style>
