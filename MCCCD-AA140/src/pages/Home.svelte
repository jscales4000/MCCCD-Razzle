<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    panelOnline,
    display1SourceFb,
    systemPowerFb,
    progAudioLevelFb,
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';
  import { goToPage } from '../lib/stores/page';
  import { userPoweredOn } from '../lib/stores/session';
  import ConfirmShutdownModal from '../components/ConfirmShutdownModal.svelte';
  import HomeSplash from '../components/HomeSplash.svelte';
  import VolumePopup from '../components/VolumePopup.svelte';
  import MicIcon from '../lib/ui/MicIcon.svelte';
  import VolIcon from '../lib/ui/VolIcon.svelte';

  // ── Source buttons (Mockup 22 — Centered Hero) ──
  // Tapping a source publishes its value to ALL THREE displays at once.
  // Default-meeting assumption: one source mirrored across D1/D2/D3.
  // Advanced (per-display) routing reachable via the Advanced Routing chip.
  const SOURCES = [
    { value: 1, name: 'Room PC',  sub: 'HDMI 1' },
    { value: 2, name: 'Ext PC',   sub: 'HDMI 2' },
    { value: 3, name: 'AirMedia', sub: 'Wireless' },
    { value: 4, name: 'Laptop',   sub: 'HDMI 3' },
  ] as const;

  function selectSourceForAll(value: number) {
    publishAnalog(SIGNALS.display1Source, value);
    publishAnalog(SIGNALS.display2Source, value);
    publishAnalog(SIGNALS.display3Source, value);
  }

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

  // userPoweredOn is a session-scoped store (lib/stores/session.ts) so its
  // value survives goToPage() round-trips through Cameras / AudioMixer /
  // DisplayRouting. Local component state would reset on each remount and
  // bounce the user back to the splash after every nav.
  let systemOn = $derived($systemPowerFb || $userPoweredOn);

  // VolumePopup ref — call .show() on Vol+/Vol- to flash the level for 5s.
  // Mute does NOT trigger the popup (per spec).
  let volumePopup: { show: () => void } | undefined = $state(undefined);

  function volDown() {
    pulseDigital(SIGNALS.volumeDown);
    volumePopup?.show();
  }
  function volUp() {
    pulseDigital(SIGNALS.volumeUp);
    volumePopup?.show();
  }
  function toggleMaster() {
    pulseDigital(SIGNALS.muteAll);
    // No popup for mute, per spec.
  }

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
      userPoweredOn.set(true);
      pulseDigital(SIGNALS.displayPower);
    }
  }

  function powerOnFromSplash() {
    userPoweredOn.set(true);
    pulseDigital(SIGNALS.displayPower);
  }

  function confirmShutdown() {
    showShutdownModal = false;
    userPoweredOn.set(false);
    pulseDigital(SIGNALS.displayPower);
  }
  function cancelShutdown() {
    showShutdownModal = false;
  }

  function occupancyText(): string {
    if ($occupancyState === 1) return 'Occupied';
    if ($occupancyState === 2) return `Vacant · ${$shutdownCountdown} min`;
    return 'Vacant';
  }
  function occupancyClass(): string {
    if ($occupancyState === 1) return 'occ-occ';
    if ($occupancyState === 2) return 'occ-warn';
    return 'occ-idle';
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
    <header class="app-header">
      <span class="room-name">{ROOM_NAME}</span>

      <span class="small-pill" class:ok={$panelOnline} class:off={!$panelOnline}>
        <span class="pdot"></span>{$panelOnline ? 'Online' : 'Offline'}
      </span>

      <span class="small-pill {occupancyClass()}">
        <span class="pdot"></span>{occupancyText()}
      </span>

      <div class="hsp"></div>

      <button class="header-nav" onclick={() => goToPage('cameras')} aria-label="Open cameras page">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
          <path d="M4 7h4l2-2h4l2 2h4v12H4z"/>
          <circle cx="12" cy="13" r="3.6"/>
        </svg>
        Cameras
      </button>
      <button class="header-nav" onclick={() => goToPage('audio')} aria-label="Open audio mixer page">
        <VolIcon variant="audio" size={18} strokeWidth={1.8} />
        Audio
      </button>
    </header>

    <main class="body-wrap">
      <button class="adv-float" onclick={() => goToPage('routing')} aria-label="Open advanced display routing">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
          <rect x="3" y="3" width="7" height="7"/>
          <rect x="14" y="3" width="7" height="7"/>
          <rect x="3" y="14" width="7" height="7"/>
          <rect x="14" y="14" width="7" height="7"/>
        </svg>
        Advanced Routing →
      </button>
      <div class="eyebrow">— Choose your source —</div>
      <div class="src-row">
        {#each SOURCES as src}
          <button
            class="hero-card"
            class:active={$display1SourceFb === src.value}
            onclick={() => selectSourceForAll(src.value)}
            aria-pressed={$display1SourceFb === src.value}
            aria-label={`Send ${src.name} to all displays`}
          >
            {#if src.value === 1}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
            {:else if src.value === 2}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M3 10h18M8 20h8M12 16v4"/></svg>
            {:else if src.value === 3}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>
            {:else}
              <svg class="hc-ico" width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="2" y="4" width="20" height="13" rx="2"/><path d="M2 20h20"/></svg>
            {/if}
            <span class="hc-name">{src.name}</span>
            <span class="hc-sub">{src.sub}</span>
          </button>
        {/each}
      </div>
    </main>

    <footer class="app-footer">
      <button
        class="pwr-btn"
        class:primary={systemOn}
        onclick={powerButtonTapped}
        aria-label={systemOn ? 'System on — tap to shut down' : 'System off — tap to power on'}
      >
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
          <path d="M12 3v9"/>
          <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
        </svg>
        Power
      </button>

      <div class="mics">
        <span class="footer-label">Mics</span>
        <button class="mbtn" class:live={!$micLavMuteFb} class:muted={$micLavMuteFb} onclick={toggleLavMute}>
          <span class="mbtn-icon" aria-hidden="true">
            <MicIcon size={26} />
            {#if !$micLavMuteFb}
              <span class="mbtn-eq">
                <span></span><span></span><span></span><span></span>
              </span>
            {/if}
          </span>
          <span class="mbtn-text">
            <span class="mbtn-name">Lav</span>
            <span class="mbtn-status">
              <span class="mbtn-dot"></span>{$micLavMuteFb ? 'Muted' : 'Live'}
            </span>
          </span>
        </button>
        <button class="mbtn" class:live={!$micHandheldMuteFb} class:muted={$micHandheldMuteFb} onclick={toggleHandheldMute}>
          <span class="mbtn-icon" aria-hidden="true">
            <MicIcon size={26} />
            {#if !$micHandheldMuteFb}
              <span class="mbtn-eq">
                <span></span><span></span><span></span><span></span>
              </span>
            {/if}
          </span>
          <span class="mbtn-text">
            <span class="mbtn-name">Handheld</span>
            <span class="mbtn-status">
              <span class="mbtn-dot"></span>{$micHandheldMuteFb ? 'Muted' : 'Live'}
            </span>
          </span>
        </button>
      </div>

      <div class="vol-grp">
        <span class="footer-label">Vol</span>
        <button class="vbtn" onclick={volDown} aria-label="Volume down">
          <VolIcon variant="down" size={28} />
          −
        </button>
        <button class="vbtn" onclick={toggleMaster} aria-label="Mute toggle">
          <VolIcon variant="mute" size={28} />
          Mute
        </button>
        <button class="vbtn" onclick={volUp} aria-label="Volume up">
          <VolIcon variant="up" size={28} />
          +
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

<VolumePopup bind:this={volumePopup} level={$progAudioLevelFb} />

<style>
  /* ── Mockup 22 — Centered Hero (with #24 Layered-Depth + #25 Medium/Large/Prominent sizing) ── */
  .layout-home {
    display: grid;
    grid-template-rows: 80px 1fr 124px;
    gap: 14px;
    width: 100%;
    height: 100%;
    padding: 14px;
    position: relative;
  }
  .layout-home.splash-mode {
    display: block;
    padding: 0;
  }
  .layout-home::before {
    content: '';
    position: absolute;
    top: -200px;
    left: 50%;
    transform: translateX(-50%);
    width: 1300px;
    height: 700px;
    background: radial-gradient(ellipse, rgba(245, 166, 35, 0.07), transparent 65%);
    pointer-events: none;
  }

  /* ── HEADER ── */
  .app-header {
    display: flex;
    align-items: center;
    padding: 0 18px;
    gap: 16px;
  }
  .room-name {
    font-size: 40px;
    font-weight: 900;
    letter-spacing: -0.025em;
    color: var(--color-copy, #e2e8f0);
  }
  .small-pill {
    display: flex;
    align-items: center;
    gap: 5px;
    padding: 4px 10px;
    border-radius: 12px;
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
  }
  .small-pill.ok {
    background: rgba(34, 197, 94, 0.08);
    border: 0.5px solid rgba(34, 197, 94, 0.25);
    color: #86efac;
  }
  .small-pill.off {
    background: rgba(100, 116, 139, 0.08);
    border: 0.5px solid rgba(100, 116, 139, 0.25);
    color: #94a3b8;
  }
  .small-pill.occ-occ {
    background: rgba(34, 197, 94, 0.08);
    border: 0.5px solid rgba(34, 197, 94, 0.25);
    color: #86efac;
  }
  .small-pill.occ-warn {
    background: rgba(239, 68, 68, 0.1);
    border: 0.5px solid rgba(239, 68, 68, 0.3);
    color: #fca5a5;
  }
  .small-pill.occ-idle {
    background: rgba(245, 158, 11, 0.1);
    border: 0.5px solid rgba(245, 158, 11, 0.3);
    color: #fcd34d;
  }
  .pdot {
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 5px currentColor;
    animation: pdot-pulse 2.2s ease-in-out infinite;
  }
  @keyframes pdot-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.4; }
  }
  .hsp { flex: 1; }
  /* Header nav — Medium (Mockup #25) — 40px tall, comfortable touch */
  .header-nav {
    appearance: none;
    -webkit-appearance: none;
    display: flex;
    align-items: center;
    gap: 9px;
    min-height: 40px;
    min-width: 120px;
    padding: 0 18px;
    border-radius: 9px;
    background-color: rgba(30, 41, 59, 0.5);
    border: 0.5px solid rgba(148, 163, 184, 0.18);
    color: var(--color-copy-soft, #94a3b8);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    cursor: pointer;
    transition: color 110ms ease, background-color 110ms ease, border-color 110ms ease;
    font-family: inherit;
  }
  .header-nav:hover {
    color: var(--color-copy, #e2e8f0);
    background-color: rgba(51, 65, 85, 0.7);
    border-color: rgba(148, 163, 184, 0.3);
  }

  /* ── BODY — centered hero ── */
  .body-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    position: relative;
    min-height: 0;
    gap: 24px;
  }
  .eyebrow {
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.34em;
    text-transform: uppercase;
    color: var(--color-copy-soft, #94a3b8);
    background: linear-gradient(90deg, transparent, rgba(245, 166, 35, 0.4), transparent);
    -webkit-background-clip: text;
    background-clip: text;
    color: transparent;
    text-align: center;
  }
  .src-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 18px;
    width: 80%;
    max-height: 440px;
  }
  /* Source button — Layered Depth (Mockup #24 variant 3) */
  .hero-card {
    appearance: none;
    -webkit-appearance: none;
    background-color: #08101e;
    background-image: linear-gradient(180deg, #14213a, #08101e);
    border: 0.5px solid rgba(148, 163, 184, 0.2);
    border-radius: 18px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 18px;
    padding: 36px 18px;
    cursor: pointer;
    transition: border-color 220ms ease, transform 220ms ease, box-shadow 220ms ease;
    position: relative;
    overflow: hidden;
    color: inherit;
    font: inherit;
  }
  .hero-card:hover {
    border-color: rgba(245, 166, 35, 0.3);
    transform: translateY(-2px);
  }
  .hc-ico {
    color: var(--color-copy-soft, #94a3b8);
    transition: color 220ms ease;
  }
  .hc-name {
    font-size: 22px;
    font-weight: 800;
    letter-spacing: -0.01em;
    color: var(--color-copy, #e2e8f0);
  }
  .hc-sub {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }
  /* Active state: 3px orange stripe across the top edge + faint card glow */
  .hero-card.active {
    border-color: rgba(245, 166, 35, 0.35);
    box-shadow:
      0 0 24px rgba(245, 166, 35, 0.18),
      0 14px 40px rgba(245, 166, 35, 0.1);
  }
  .hero-card.active::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 3px;
    background: linear-gradient(90deg, transparent, #f5a623 18%, #f5a623 82%, transparent);
    pointer-events: none;
  }
  .hero-card.active .hc-ico,
  .hero-card.active .hc-name { color: #f5a623; }

  /* Advanced Routing chip — top-right of body area */
  /* Advanced Routing — Prominent (Mockup #25) — orange-on-navy, 52px tall */
  .adv-float {
    appearance: none;
    -webkit-appearance: none;
    position: absolute;
    top: 4px;
    right: 8px;
    display: flex;
    align-items: center;
    gap: 9px;
    min-height: 52px;
    padding: 0 22px;
    border-radius: 11px;
    background-color: #f5a623;
    background-image: linear-gradient(180deg, #f9b94a, #ec9415);
    border: 1px solid rgba(245, 166, 35, 0.6);
    color: #1a1208;
    font-size: 13px;
    font-weight: 800;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: pointer;
    transition: filter 110ms ease, transform 110ms ease, box-shadow 110ms ease;
    box-shadow:
      0 6px 18px rgba(245, 166, 35, 0.32),
      0 0 0 1px rgba(245, 166, 35, 0.1);
    font-family: inherit;
  }
  .adv-float:hover {
    filter: brightness(1.06);
    transform: translateY(-1px);
    box-shadow:
      0 10px 24px rgba(245, 166, 35, 0.4),
      0 0 0 1px rgba(245, 166, 35, 0.15);
  }
  .adv-float:active { transform: translateY(0); }

  /* ── FOOTER ── */
  .app-footer {
    background: rgba(12, 20, 36, 0.97);
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.15));
    border-radius: 14px;
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    padding: 0 22px;
    gap: 18px;
  }
  .pwr-btn {
    appearance: none;
    -webkit-appearance: none;
    display: flex;
    align-items: center;
    gap: 14px;
    min-height: 86px;
    min-width: 170px;
    padding: 0 28px;
    border-radius: 14px;
    background-color: rgba(245, 166, 35, 0.18);
    background-image: linear-gradient(180deg, rgba(245, 166, 35, 0.22), rgba(245, 166, 35, 0.12));
    border: none;
    color: #f5a623;
    font-size: 15px;
    font-weight: 800;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    cursor: pointer;
    transition: background-color 110ms ease, transform 110ms ease, box-shadow 110ms ease;
    box-shadow: 0 8px 24px rgba(245, 166, 35, 0.16);
    font-family: inherit;
  }
  .pwr-btn:hover {
    background-color: rgba(245, 166, 35, 0.28);
    box-shadow: 0 12px 32px rgba(245, 166, 35, 0.24);
  }
  .pwr-btn:active { transform: scale(0.98); }
  .pwr-btn.primary {
    background-color: rgba(245, 166, 35, 0.28);
    box-shadow:
      0 8px 24px rgba(245, 166, 35, 0.22),
      0 0 0 1px rgba(245, 166, 35, 0.35);
  }

  .mics {
    display: flex;
    align-items: center;
    gap: 8px;
    justify-content: center;
  }
  .footer-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }
  /* Mic toggles — Larger, borderless, layered gradient + glow */
  .mbtn {
    appearance: none;
    -webkit-appearance: none;
    display: flex;
    align-items: center;
    gap: 16px;
    min-height: 96px;
    min-width: 180px;
    padding: 0 24px;
    border-radius: 14px;
    border: none;
    background-color: rgba(15, 23, 42, 0.7);
    background-image: linear-gradient(180deg, rgba(30, 41, 59, 0.55), rgba(8, 14, 26, 0.55));
    color: var(--color-copy-soft, #94a3b8);
    cursor: pointer;
    transition: background-color 160ms ease, color 160ms ease, box-shadow 160ms ease, transform 110ms ease;
    box-shadow:
      0 8px 22px rgba(0, 0, 0, 0.35),
      inset 0 1px 0 rgba(148, 163, 184, 0.06);
    font-family: inherit;
    text-align: left;
    position: relative;
    overflow: hidden;
  }
  .mbtn:active { transform: scale(0.98); }

  .mbtn-icon {
    position: relative;
    display: grid;
    place-items: center;
    width: 36px;
    height: 36px;
    flex-shrink: 0;
  }
  .mbtn-icon :global(svg) {
    transition: opacity 200ms ease;
  }

  /* Mini equalizer — only rendered when .live; 4 vertical bars that pulse */
  .mbtn-eq {
    position: absolute;
    bottom: -10px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    align-items: flex-end;
    gap: 2px;
    height: 8px;
    pointer-events: none;
  }
  .mbtn-eq > span {
    width: 3px;
    background: currentColor;
    border-radius: 1px;
    box-shadow: 0 0 4px currentColor;
    animation: eq-bar 900ms ease-in-out infinite;
  }
  .mbtn-eq > span:nth-child(1) { animation-delay: 0ms;   height: 4px; }
  .mbtn-eq > span:nth-child(2) { animation-delay: 180ms; height: 8px; }
  .mbtn-eq > span:nth-child(3) { animation-delay: 360ms; height: 6px; }
  .mbtn-eq > span:nth-child(4) { animation-delay: 540ms; height: 5px; }
  @keyframes eq-bar {
    0%, 100% { transform: scaleY(0.4); }
    50%      { transform: scaleY(1.0); }
  }

  .mbtn-text {
    display: flex;
    flex-direction: column;
    gap: 4px;
    align-items: flex-start;
    line-height: 1.1;
  }
  .mbtn-name {
    font-size: 18px;
    font-weight: 800;
    letter-spacing: -0.01em;
  }
  .mbtn-status {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    opacity: 0.95;
  }
  .mbtn-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 6px currentColor;
  }

  .mbtn.live {
    color: #4ade80;
    background-color: rgba(34, 197, 94, 0.16);
    background-image: linear-gradient(180deg, rgba(34, 197, 94, 0.22), rgba(34, 197, 94, 0.06));
    box-shadow:
      0 0 24px rgba(34, 197, 94, 0.22),
      0 8px 22px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(74, 222, 128, 0.22);
  }
  .mbtn.live .mbtn-dot {
    animation: live-pulse 1.4s ease-in-out infinite;
  }
  @keyframes live-pulse {
    0%, 100% { transform: scale(1);   opacity: 1; box-shadow: 0 0 6px currentColor; }
    50%      { transform: scale(1.3); opacity: 0.7; box-shadow: 0 0 10px currentColor; }
  }

  .mbtn.muted {
    color: #fca5a5;
    background-color: rgba(239, 68, 68, 0.14);
    background-image: linear-gradient(180deg, rgba(239, 68, 68, 0.18), rgba(239, 68, 68, 0.06));
    box-shadow:
      0 0 18px rgba(239, 68, 68, 0.14),
      0 8px 22px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(252, 165, 165, 0.16);
  }
  .mbtn.muted .mbtn-icon :global(svg) {
    opacity: 0.6;
  }
  /* Slash through the icon when muted */
  .mbtn.muted .mbtn-icon::after {
    content: '';
    position: absolute;
    top: 50%;
    left: 0;
    right: 0;
    height: 2px;
    background: currentColor;
    transform: rotate(-22deg);
    border-radius: 1px;
  }

  @media (prefers-reduced-motion: reduce) {
    .mbtn-eq > span { animation: none; transform: scaleY(0.7); }
    .mbtn.live .mbtn-dot { animation: none; }
  }

  /* Volume — right aligned, transparent buttons (icons + text only) */
  .vol-grp {
    display: flex;
    align-items: center;
    gap: 10px;
    justify-self: end;
  }
  /* Volume buttons — Larger (60×100), transparent (icon + text only) */
  .vbtn {
    appearance: none;
    -webkit-appearance: none;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    min-height: 76px;
    min-width: 90px;
    padding: 0 12px;
    background-color: transparent;
    border: none;
    color: var(--color-copy-soft, #94a3b8);
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: pointer;
    transition: color 110ms ease, transform 110ms ease;
    font-family: inherit;
  }
  .vbtn:hover { color: #f5a623; }
  .vbtn:active { transform: scale(0.96); }

  @media (prefers-reduced-motion: reduce) {
    .pdot { animation: none; }
    .hero-card { transition: none; }
  }
</style>
