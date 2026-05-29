<!--
  AppFooter — MCCCD standard bottom bar.

  Pure presentational component. Owns NO room-specific knowledge:
    - No imports from lib/contract, lib/CrComLib, or lib/stores/signals
    - All state arrives via props; all actions are callback props
    - Owns its own modal + volume-popup state (those ARE part of the standard UX)

  Visual identity is fixed (the "MCCCD footer look"). Per-room variation lives
  in the data passed in: mics array length, names, mute states, shutdown items.

  See ./README.md for the integration recipe.
-->

<script lang="ts">
  import ConfirmShutdownModal from '../ConfirmShutdownModal.svelte';
  import VolumePopup from '../VolumePopup.svelte';
  import type { FooterPower, FooterVolume, MicChannel, ShutdownItem } from './types';

  interface Props {
    power: FooterPower;
    mics: MicChannel[];
    volume: FooterVolume;
    shutdownItems: ShutdownItem[];

    onPowerOn: () => void;
    onShutdownConfirm: () => void;
    onVolumeUp: () => void;
    onVolumeDown: () => void;
    onMuteToggle: () => void;

    /** Optional vacancy minutes shown in the shutdown modal's bottom strip. */
    vacancyMinutes?: number;
    /** Seconds before the auto-confirm ring completes. Defaults to 30. */
    shutdownCountdown?: number;
    /** Custom title for the shutdown modal. Defaults to "Shut Down Room?". */
    shutdownTitle?: string;
  }

  let {
    power,
    mics,
    volume,
    shutdownItems,
    onPowerOn,
    onShutdownConfirm,
    onVolumeUp,
    onVolumeDown,
    onMuteToggle,
    vacancyMinutes,
    shutdownCountdown = 30,
    shutdownTitle = 'Shut Down Room?',
  }: Props = $props();

  let showShutdownModal = $state(false);
  let volumePopup: { show: () => void } | undefined = $state(undefined);

  function powerTapped() {
    if (power.isOn) {
      showShutdownModal = true;
    } else {
      onPowerOn();
    }
  }

  function confirmShutdown() {
    showShutdownModal = false;
    onShutdownConfirm();
  }
  function cancelShutdown() {
    showShutdownModal = false;
  }

  function handleVolUp() {
    onVolumeUp();
    volumePopup?.show();
  }
  function handleVolDown() {
    onVolumeDown();
    volumePopup?.show();
  }
</script>

<footer class="app-footer" style:--mic-count={mics.length}>
  <!-- ── Power (V2: Inline Status Chip) ─────────────────────────────── -->
  <button
    class="pwr"
    class:on={power.isOn}
    onclick={powerTapped}
    aria-label={power.isOn ? 'System on — tap to shut down' : 'System off — tap to power on'}
    type="button"
  >
    <span class="pwr-icon" aria-hidden="true">
      <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
        <path d="M12 3v9"/>
        <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
      </svg>
    </span>
    <span class="pwr-name">Power</span>
    <span class="pwr-status">{power.isOn ? 'On' : 'Off'}</span>
  </button>

  <!-- ── Mics (V4: Live Waveform) ───────────────────────────────────── -->
  {#if mics.length > 0}
    <div class="mics">
      <span class="mics-label">{mics.length === 1 ? 'Mic' : 'Mics'}</span>
      {#each mics as mic (mic.id)}
        <button
          class="mbtn"
          class:live={!mic.isMuted}
          class:muted={mic.isMuted}
          onclick={mic.onToggle}
          aria-pressed={mic.isMuted}
          aria-label="{mic.label} — {mic.isMuted ? 'muted' : 'live'}, tap to toggle"
          type="button"
        >
          <span class="mbtn-icon" aria-hidden="true">
            <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
              <rect x="9" y="3" width="6" height="11" rx="3"/>
              <path d="M5 11a7 7 0 0 0 14 0M12 18v3M8 21h8"/>
            </svg>
          </span>
          <span class="mbtn-text">
            <span class="mbtn-name">{mic.label}</span>
            {#if mic.isMuted}
              <span class="mbtn-mute-label">Muted</span>
            {:else}
              <span class="mbtn-waveform" aria-hidden="true">
                <span></span><span></span><span></span><span></span><span></span><span></span>
              </span>
            {/if}
          </span>
        </button>
      {/each}
    </div>
  {:else}
    <span class="mics-spacer" aria-hidden="true"></span>
  {/if}

  <!-- ── Vol (F: Bold +/−) ──────────────────────────────────────────── -->
  <div class="vol">
    <span class="vol-label">Vol</span>
    <button class="vbtn" onclick={handleVolDown} aria-label="Volume down" type="button">−</button>
    <span class="vol-readout">
      {volume.level}<small>level</small>
    </span>
    <button class="vbtn" onclick={handleVolUp} aria-label="Volume up" type="button">+</button>
    <div class="vol-divider" aria-hidden="true"></div>
    <button class="vbtn-mute" onclick={onMuteToggle} aria-label="Mute toggle" type="button">
      <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <path d="M3 9v6h4l5 4V5L7 9zM18 9l4 6M22 9l-4 6"/>
      </svg>
    </button>
  </div>
</footer>

<ConfirmShutdownModal
  open={showShutdownModal}
  countdown={shutdownCountdown}
  title={shutdownTitle}
  {vacancyMinutes}
  {shutdownItems}
  onConfirm={confirmShutdown}
  onCancel={cancelShutdown}
/>

<VolumePopup bind:this={volumePopup} level={volume.level} />

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
    min-height: 112px;
  }
  .mics-spacer { display: block; min-height: 1px; }

  /* ── Power: V2 Inline Status Chip ─────────────────────────────────── */
  .pwr {
    appearance: none;
    -webkit-appearance: none;
    background: transparent;
    border: none;
    padding: 12px 16px;
    display: flex;
    align-items: center;
    gap: 14px;
    color: var(--color-accent, #f5a623);
    cursor: pointer;
    font-family: inherit;
    transition: transform 110ms ease;
  }
  .pwr:active { transform: scale(0.97); }
  .pwr:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 4px;
    border-radius: 8px;
  }

  .pwr-icon {
    width: 34px;
    height: 34px;
    display: grid;
    place-items: center;
    color: var(--color-accent, #f5a623);
  }

  .pwr-name {
    font-size: 18px;
    font-weight: 800;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    color: var(--color-copy, #e2e8f0);
  }

  .pwr-status {
    font-size: 10px;
    font-weight: 800;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    padding: 3px 10px;
    border-radius: 4px;
    background: var(--color-accent-soft, rgba(245, 166, 35, 0.18));
    color: var(--color-accent, #f5a623);
  }
  .pwr:not(.on) .pwr-status {
    background: rgba(100, 116, 139, 0.18);
    color: var(--color-copy-soft, #94a3b8);
  }
  .pwr:not(.on) .pwr-icon {
    color: var(--color-copy-soft, #94a3b8);
  }

  /* ── Mics: V4 Live Waveform ───────────────────────────────────────── */
  .mics {
    display: flex;
    align-items: center;
    gap: 18px;
    justify-content: center;
  }

  .mics-label, .vol-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }

  .mbtn {
    appearance: none;
    -webkit-appearance: none;
    background: transparent;
    border: none;
    padding: 14px 16px;
    color: var(--color-copy-soft, #94a3b8);
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 12px;
    font-family: inherit;
    transition: transform 110ms ease;
    /* Mic buttons shrink slightly when there are more than 3 in the footer.
       Override --mic-min-width in the consuming project's CSS if needed. */
    min-width: var(--mic-min-width, auto);
  }
  .mbtn:active { transform: scale(0.97); }
  .mbtn:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 3px;
    border-radius: 8px;
  }

  .mbtn-icon {
    width: 36px;
    height: 36px;
    display: grid;
    place-items: center;
    color: inherit;
    position: relative;
    transition: color 200ms ease;
  }
  .mbtn.live .mbtn-icon { color: var(--color-mic-live, #4ade80); }
  .mbtn.muted .mbtn-icon { color: var(--color-mic-muted, #fca5a5); }
  .mbtn.muted .mbtn-icon::after {
    content: '';
    position: absolute;
    width: 32px;
    height: 2px;
    background: currentColor;
    transform: rotate(-22deg);
    border-radius: 1px;
  }

  .mbtn-text {
    display: flex;
    flex-direction: column;
    gap: 3px;
    align-items: flex-start;
  }

  .mbtn-name {
    font-size: 17px;
    font-weight: 800;
    letter-spacing: -0.01em;
    color: var(--color-copy, #e2e8f0);
  }

  .mbtn-waveform {
    display: flex;
    align-items: flex-end;
    gap: 3px;
    height: 16px;
  }
  .mbtn-waveform > span {
    width: 3px;
    border-radius: 1px;
    background: var(--color-mic-live, #4ade80);
    box-shadow: 0 0 4px var(--color-mic-live, #4ade80);
    animation: af-wave 1100ms ease-in-out infinite;
  }
  .mbtn-waveform > span:nth-child(1) { height: 40%; animation-delay: 0ms; }
  .mbtn-waveform > span:nth-child(2) { height: 90%; animation-delay: 120ms; }
  .mbtn-waveform > span:nth-child(3) { height: 55%; animation-delay: 240ms; }
  .mbtn-waveform > span:nth-child(4) { height: 75%; animation-delay: 360ms; }
  .mbtn-waveform > span:nth-child(5) { height: 35%; animation-delay: 480ms; }
  .mbtn-waveform > span:nth-child(6) { height: 60%; animation-delay: 600ms; }
  @keyframes af-wave {
    0%, 100% { transform: scaleY(0.3); }
    50%      { transform: scaleY(1.0); }
  }

  .mbtn-mute-label {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: var(--color-mic-muted, #fca5a5);
  }

  /* ── Vol: F Bold +/− ──────────────────────────────────────────────── */
  .vol {
    display: flex;
    align-items: center;
    gap: 14px;
    justify-self: end;
  }

  .vbtn {
    appearance: none;
    -webkit-appearance: none;
    background: transparent;
    border: none;
    width: 60px;
    height: 70px;
    display: grid;
    place-items: center;
    color: var(--color-copy-soft, #94a3b8);
    cursor: pointer;
    font-family: inherit;
    font-size: 44px;
    font-weight: 900;
    line-height: 1;
    transition: color 110ms ease, transform 110ms ease;
  }
  .vbtn:hover { color: var(--color-accent, #f5a623); }
  .vbtn:active { transform: scale(0.94); }
  .vbtn:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 3px;
    border-radius: 8px;
  }

  .vol-readout {
    font-size: 28px;
    font-weight: 900;
    color: var(--color-accent, #f5a623);
    line-height: 0.95;
    text-shadow: 0 0 12px rgba(245, 166, 35, 0.5);
    display: inline-flex;
    flex-direction: column;
    align-items: center;
    min-width: 52px;
  }
  .vol-readout small {
    font-size: 10px;
    color: var(--color-copy-soft, #94a3b8);
    letter-spacing: 0.28em;
    text-transform: uppercase;
    font-weight: 700;
    display: block;
    margin-top: 3px;
    text-shadow: none;
  }

  .vol-divider {
    width: 1px;
    height: 56px;
    background: var(--color-border, rgba(148, 163, 184, 0.22));
    margin: 0 4px;
  }

  .vbtn-mute {
    appearance: none;
    -webkit-appearance: none;
    background: transparent;
    border: none;
    width: 56px;
    height: 56px;
    display: grid;
    place-items: center;
    color: var(--color-copy-soft, #94a3b8);
    cursor: pointer;
    border-radius: 8px;
    font-family: inherit;
    transition: color 110ms ease, background 110ms ease, transform 110ms ease;
  }
  .vbtn-mute:hover {
    color: var(--color-accent, #f5a623);
    background: rgba(245, 166, 35, 0.08);
  }
  .vbtn-mute:active { transform: scale(0.94); }
  .vbtn-mute:focus-visible {
    outline: 2px solid var(--color-accent);
    outline-offset: 2px;
  }

  @media (prefers-reduced-motion: reduce) {
    .mbtn-waveform > span { animation: none; transform: scaleY(0.7); }
    .pwr, .mbtn, .vbtn, .vbtn-mute { transition: none; }
  }
</style>
