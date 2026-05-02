<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    panelOnline,
    display1SourceFb, display2SourceFb, display3SourceFb,
    display1PowerFb, display2PowerFb, display3PowerFb,
    systemPowerFb,
    audioOutputSelectFb,
    micLavMuteFb, micHandheldMuteFb,
    micLavLineOutFb, micHandheldLineOutFb,
    micLavLevel, micHandheldLevel,
    micLavConnected, micHandheldConnected,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import DisplayTile from '../components/DisplayTile.svelte';
  import SourceRail from '../components/SourceRail.svelte';
  import ConfirmShutdownModal from '../components/ConfirmShutdownModal.svelte';
  import MicVolumeModal from '../components/MicVolumeModal.svelte';

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

  // Mic volume modal — null means closed, otherwise the mic id to show
  type MicId = 'lav' | 'handheld';
  let activeMic: MicId | null = $state(null);

  function mirrorD1ToD3() { pulseDigital(SIGNALS.d1MirrorToD3); }
  function mirrorD2ToD3() { pulseDigital(SIGNALS.d2MirrorToD3); }

  function setAudioOutput(v: 1 | 2) {
    publishAnalog(SIGNALS.audioOutputSelect, v);
  }

  function volDown()      { pulseDigital(SIGNALS.volumeDown); }
  function volUp()        { pulseDigital(SIGNALS.volumeUp); }
  function toggleMaster() { pulseDigital(SIGNALS.muteAll); }

  function powerButtonTapped() {
    if ($systemPowerFb) {
      // System is ON — open confirmation modal (do not pulse yet)
      showShutdownModal = true;
    } else {
      // System is OFF — power up immediately, no confirmation
      pulseDigital(SIGNALS.displayPower);
    }
  }

  function confirmShutdown() {
    showShutdownModal = false;
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
        <button class="chrome-btn" onclick={() => goToPage('cameras')} aria-label="Open cameras page">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M4 7h4l2-2h4l2 2h4v12H4z" stroke="currentColor" stroke-width="1.8" fill="none" stroke-linejoin="round"/>
            <circle cx="12" cy="13" r="3.6" stroke="currentColor" stroke-width="1.8" fill="none"/>
          </svg>
          <span>Cameras</span>
        </button>
        <button class="chrome-btn" onclick={() => goToPage('settings')} aria-label="Open settings page">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <circle cx="12" cy="12" r="2.5" stroke="currentColor" stroke-width="1.8" fill="none"/>
            <path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.5 5.5l2.1 2.1M16.4 16.4l2.1 2.1M5.5 18.5l2.1-2.1M16.4 7.6l2.1-2.1" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" fill="none"/>
          </svg>
          <span>Settings</span>
        </button>
      </div>
    </header>

    <aside class="source-rail-host">
      <SourceRail />
    </aside>

    <main class="display-row">
      <DisplayTile
        label="Display 1"
        displayId="d1"
        activeSourceFb={$display1SourceFb}
        powerOn={$display1PowerFb}
        audioActive={$audioOutputSelectFb === 1}
        onAudioToggle={() => setAudioOutput(1)}
        onMirrorToD3={mirrorD1ToD3}
      />
      <DisplayTile
        label="Display 2"
        displayId="d2"
        activeSourceFb={$display2SourceFb}
        powerOn={$display2PowerFb}
        audioActive={$audioOutputSelectFb === 2}
        onAudioToggle={() => setAudioOutput(2)}
        onMirrorToD3={mirrorD2ToD3}
      />
      <DisplayTile
        label="Display 3"
        displayId="d3"
        activeSourceFb={$display3SourceFb}
        powerOn={$display3PowerFb}
      />
    </main>

    <footer class="app-footer glass-card">
      <button
        class="chrome-btn power-btn"
        class:on={$systemPowerFb}
        onclick={powerButtonTapped}
        aria-label={$systemPowerFb ? 'System on — tap to shut down' : 'System off — tap to power on'}
      >
        <svg viewBox="0 0 24 24" width="24" height="24" aria-hidden="true">
          <path d="M12 3v9" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" fill="none"/>
          <path d="M6.5 7.5a8 8 0 1 0 11 0" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" fill="none"/>
        </svg>
        <span>{$systemPowerFb ? 'System On' : 'Power'}</span>
      </button>

      <div class="mic-group">
        <span class="footer-label">MICS</span>
        <button
          class="btn footer-btn"
          class:active={$micLavMuteFb}
          onclick={() => (activeMic = 'lav')}
          aria-label="Lavalier mic — open volume controls"
        >
          {$micLavMuteFb ? 'Lav (muted)' : 'Lav'}
        </button>
        <button
          class="btn footer-btn"
          class:active={$micHandheldMuteFb}
          onclick={() => (activeMic = 'handheld')}
          aria-label="Handheld mic — open volume controls"
        >
          {$micHandheldMuteFb ? 'Handheld (muted)' : 'Handheld'}
        </button>
      </div>

      <div class="vol-group">
        <button class="chrome-btn" onclick={volDown} aria-label="Volume down">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"/>
            <path d="M16 12h6" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
          </svg>
          <span>Vol −</span>
        </button>
        <button class="chrome-btn" onclick={toggleMaster} aria-label="Mute">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"/>
            <path d="M16 9l6 6M22 9l-6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
          </svg>
          <span>Mute</span>
        </button>
        <button class="chrome-btn" onclick={volUp} aria-label="Volume up">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"/>
            <path d="M16 12h6M19 9v6" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none"/>
          </svg>
          <span>Vol +</span>
        </button>
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
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>

<MicVolumeModal
  open={activeMic === 'lav'}
  name="Lavalier"
  volumeFb={micLavLineOutFb}
  volumeSetSignal={SIGNALS.micLavLineOut}
  muteFb={micLavMuteFb}
  muteSetSignal={SIGNALS.micLavMute}
  levelFb={micLavLevel}
  connectedFb={micLavConnected}
  onClose={() => (activeMic = null)}
/>

<MicVolumeModal
  open={activeMic === 'handheld'}
  name="Handheld"
  volumeFb={micHandheldLineOutFb}
  volumeSetSignal={SIGNALS.micHandheldLineOut}
  muteFb={micHandheldMuteFb}
  muteSetSignal={SIGNALS.micHandheldMute}
  levelFb={micHandheldLevel}
  connectedFb={micHandheldConnected}
  onClose={() => (activeMic = null)}
/>

<style>
  .layout-home {
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    grid-template-columns: 96px 1fr;
    grid-template-areas:
      "header header"
      "rail   tiles"
      "footer footer";
    gap: 20px;
    width: 100%;
    height: 100%;
    padding: 20px;
  }
  .app-header { grid-area: header; }
  .source-rail-host { grid-area: rail; min-height: 0; }
  .display-row { grid-area: tiles; }
  .app-footer { grid-area: footer; }
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
    border: 0.5px solid var(--color-border);
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

  .vol-group, .mic-group {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .footer-btn { min-height: 56px; padding: 0 16px; font-size: 13px; }

  /* Power button — slightly more padding so it has presence on the left,
     and a soft cyan tint when the system is ON to telegraph state without
     reverting to a full button chip. */
  .power-btn { padding: 10px 18px; }
  .power-btn.on { color: var(--color-accent); }
</style>
