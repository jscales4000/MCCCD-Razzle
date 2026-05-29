<!--
  AppFooter — shared bottom bar used by Home and DisplayRouting.

  Owns its own modal / popup state: the shutdown confirmation lives here
  so any page that mounts the footer gets the same power-off flow.
  Stores are consumed directly (no props needed) — that keeps the
  surface minimal and the behavior identical across pages.

  On shutdown confirm we navigate the panel back to Home so the user
  lands on the splash, regardless of which page they powered off from.
-->

<script lang="ts">
  import { SIGNALS } from '../lib/contract';
  import { publishDigital, pulseDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import { userPoweredOn } from '../lib/stores/session';
  import {
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
    progAudioLevelFb,
    systemPowerFb,
  } from '../lib/stores/signals';
  import ConfirmShutdownModal from './ConfirmShutdownModal.svelte';
  import VolumePopup from './VolumePopup.svelte';
  import MicIcon from '../lib/ui/MicIcon.svelte';
  import VolIcon from '../lib/ui/VolIcon.svelte';

  let systemOn = $derived($systemPowerFb || $userPoweredOn);

  let showShutdownModal = $state(false);
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
    // No popup for mute — matches Home's behavior.
  }

  function toggleLavMute() {
    publishDigital(SIGNALS.micLavMute, !$micLavMuteFb);
  }
  function toggleHandheldMute() {
    publishDigital(SIGNALS.micHandheldMute, !$micHandheldMuteFb);
  }

  function powerButtonTapped() {
    if (systemOn) {
      showShutdownModal = true;
    } else {
      userPoweredOn.set(true);
      pulseDigital(SIGNALS.displayPower);
    }
  }

  function confirmShutdown() {
    showShutdownModal = false;
    userPoweredOn.set(false);
    pulseDigital(SIGNALS.displayPower);
    // Send the user back to Home so they see the splash after powering
    // off from any page (no-op when already on Home).
    goToPage('home');
  }
  function cancelShutdown() {
    showShutdownModal = false;
  }
</script>

<footer class="app-footer">
  <button
    class="pwr-btn"
    class:primary={systemOn}
    onclick={powerButtonTapped}
    aria-label={systemOn ? 'System on — tap to shut down' : 'System off — tap to power on'}
    type="button"
  >
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
      <path d="M12 3v9"/>
      <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
    </svg>
    Power
  </button>

  <div class="mics">
    <span class="footer-label">Mics</span>
    <button class="mbtn" class:live={!$micLavMuteFb} class:muted={$micLavMuteFb} onclick={toggleLavMute} type="button">
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
    <button class="mbtn" class:live={!$micHandheldMuteFb} class:muted={$micHandheldMuteFb} onclick={toggleHandheldMute} type="button">
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
    <button class="vbtn" onclick={volDown} aria-label="Volume down" type="button">
      <VolIcon variant="down" size={28} />
      −
    </button>
    <button class="vbtn" onclick={toggleMaster} aria-label="Mute toggle" type="button">
      <VolIcon variant="mute" size={28} />
      Mute
    </button>
    <button class="vbtn" onclick={volUp} aria-label="Volume up" type="button">
      <VolIcon variant="up" size={28} />
      +
    </button>
  </div>
</footer>

<ConfirmShutdownModal
  open={showShutdownModal}
  countdown={30}
  vacancyMinutes={$occupancyState === 2 ? $shutdownCountdown : undefined}
  shutdownItems={[
    { icon: 'display', label: '4 Displays (D1 Front Left, D2 Front Right, D3 Rear, D4 Podium)' },
    { icon: 'audio',   label: 'Audio system + all 5 microphone channels' },
    { icon: 'camera',  label: 'Camera system (2 PTZ cameras)' },
  ]}
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>

<VolumePopup bind:this={volumePopup} level={$progAudioLevelFb} />

<style>
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
    animation: af-eq-bar 900ms ease-in-out infinite;
  }
  .mbtn-eq > span:nth-child(1) { animation-delay: 0ms;   height: 4px; }
  .mbtn-eq > span:nth-child(2) { animation-delay: 180ms; height: 8px; }
  .mbtn-eq > span:nth-child(3) { animation-delay: 360ms; height: 6px; }
  .mbtn-eq > span:nth-child(4) { animation-delay: 540ms; height: 5px; }
  @keyframes af-eq-bar {
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
    animation: af-live-pulse 1.4s ease-in-out infinite;
  }
  @keyframes af-live-pulse {
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

  .vol-grp {
    display: flex;
    align-items: center;
    gap: 10px;
    justify-self: end;
  }
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
</style>
