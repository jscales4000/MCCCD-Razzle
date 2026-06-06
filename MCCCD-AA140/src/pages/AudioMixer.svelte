<!--
  AudioMixer — Mockup #13 broadcast-style 4-channel mixer page.

  Replaces the legacy Settings page. Four MixerChannel strips (Lav, Handheld,
  MXA920 Array A + B) + a MasterStrip. Footer hosts scene presets +
  Link-Arrays toggle. Reached via the "Audio" button in Home's footer.

  4-mic design (2026-05-26): Q-SYS replaced by Shure P300-IMX + 2x MXA920W-S.
  The legacy 5th "Ceiling 3" strip was dropped — only 2 ceiling arrays exist
  and Auto Coverage emits one Dante channel per array.
-->
<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { ROOM_NAME, SIGNALS } from '../lib/contract';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import {
    panelOnline,
    audioOutputSelectFb,
    progAudioLevelFb,
    sceneRecallFb,
    audioLinkCeilings12Fb,
    micLavConnected, micHandheldConnected,
    micCeiling1Connected, micCeiling2Connected,
    micLavLevel, micHandheldLevel,
    micCeiling1Level, micCeiling2Level,
    micLavTrimFb, micHandheldTrimFb,
    micCeiling1TrimFb, micCeiling2TrimFb,
    micLavLineOutFb, micHandheldLineOutFb,
    micCeiling1LineOutFb, micCeiling2LineOutFb,
    micLavMuteFb, micHandheldMuteFb,
    micCeiling1MuteFb, micCeiling2MuteFb,
    initMicLevelSubscriptions, teardownMicLevelSubscriptions,
    initAudioMixerSignals, teardownAudioMixerSignals,
  } from '../lib/stores/signals';
  import MixerChannel from '../components/mixer/MixerChannel.svelte';
  import MasterStrip from '../components/mixer/MasterStrip.svelte';

  // Mic level meters (10 Hz from Shure P300 SAMPLE_IN) are subscribed lazily so
  // they don't fire a callback storm when this page isn't mounted. Per-audit H4.
  onMount(initMicLevelSubscriptions);
  onDestroy(teardownMicLevelSubscriptions);
  // Mixer state signals (trim/lineOut/connected/mute for ceiling mics + scene/link)
  // are also gated here — they're AudioMixer-only and don't need to be always live.
  onMount(initAudioMixerSignals);
  onDestroy(teardownAudioMixerSignals);

  // ── Header actions ─────────────────────────────────────────────────
  function volDown() { pulseDigital(SIGNALS.volumeDown); }
  function volUp()   { pulseDigital(SIGNALS.volumeUp); }
  function muteAll() { pulseDigital(SIGNALS.muteAll); }

  let masterDb = $derived(
    $progAudioLevelFb <= 0
      ? '-60 dB'
      : `${Math.round(-60 + (Math.max(0, Math.min(100, $progAudioLevelFb)) / 100) * 60)} dB`,
  );

  // ── MasterStrip wiring ────────────────────────────────────────────
  function onProgLevelChange(n: number) {
    publishAnalog(SIGNALS.progAudioLevel, n);
  }
  function onOutputSelect(out: 1 | 2) {
    publishAnalog(SIGNALS.audioOutputSelect, out);
  }

  // ── Footer presets ────────────────────────────────────────────────
  const PRESETS: Array<{ value: 1 | 2 | 3 | 4; label: string }> = [
    { value: 1, label: 'Lecture' },
    { value: 2, label: 'Presentation' },
    { value: 3, label: 'Hybrid' },
    { value: 4, label: 'Recording' },
  ];

  function recallScene(n: 1 | 2 | 3 | 4) {
    publishAnalog(SIGNALS.sceneRecall, n);
  }

  function toggleLinkCeilings() {
    publishDigital(SIGNALS.audioLinkCeilings12, !$audioLinkCeilings12Fb);
  }
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel — Audio Mixer</title>
</svelte:head>

<div class="mixer-page">
  <!-- HEADER ─────────────────────────────────────────────────────── -->
  <header class="mixer-header">
    <button class="back-btn" onclick={() => goToPage('home')} aria-label="Back to Home" type="button">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
        <path d="M19 12H5M12 5l-7 7 7 7"/>
      </svg>
      Home
    </button>
    <div class="sep"></div>
    <span class="room">{ROOM_NAME}</span>
    <div class="sep"></div>
    <span class="eyebrow">Audio Mixer</span>
    <div class="hsp"></div>

    <div class="master-chip">
      <span class="mc-label">Master</span>
      <button class="mc-step" onclick={volDown} aria-label="Volume down" type="button">−</button>
      <span class="mc-db" aria-live="polite">{masterDb}</span>
      <button class="mc-step" onclick={volUp} aria-label="Volume up" type="button">+</button>
    </div>

    <button
      type="button"
      class="status-pill"
      class:online={$panelOnline}
      aria-live="polite"
      tabindex="-1"
    >
      <span class="status-dot"></span>
      <span>{$panelOnline ? 'Online' : 'Offline'}</span>
    </button>

    <button class="mute-all" onclick={muteAll} type="button">Mute All</button>
  </header>

  <!-- BODY ────────────────────────────────────────────────────────── -->
  <div class="mixer-body">
    <MixerChannel
      type="Wireless · Ch 1"
      name="Lavalier"
      model="CCS-UWB Beltpack"
      connected={$micLavConnected}
      level={$micLavLevel}
      lineOut={$micLavLineOutFb}
      trim={$micLavTrimFb}
      muted={$micLavMuteFb}
      onLineOutChange={(n) => publishAnalog(SIGNALS.micLavLineOut, n)}
      onTrimChange={(n) => publishAnalog(SIGNALS.micLavTrim, n)}
      onMuteToggle={() => publishDigital(SIGNALS.micLavMute, !$micLavMuteFb)}
    />
    <MixerChannel
      type="Wireless · Ch 2"
      name="Handheld"
      model="CCS-UWB Handheld"
      connected={$micHandheldConnected}
      level={$micHandheldLevel}
      lineOut={$micHandheldLineOutFb}
      trim={$micHandheldTrimFb}
      muted={$micHandheldMuteFb}
      onLineOutChange={(n) => publishAnalog(SIGNALS.micHandheldLineOut, n)}
      onTrimChange={(n) => publishAnalog(SIGNALS.micHandheldTrim, n)}
      onMuteToggle={() => publishDigital(SIGNALS.micHandheldMute, !$micHandheldMuteFb)}
    />
    <MixerChannel
      type="Ceiling · Ch 3"
      name="Array A"
      model="MXA920W-S"
      connected={$micCeiling1Connected}
      level={$micCeiling1Level}
      lineOut={$micCeiling1LineOutFb}
      trim={$micCeiling1TrimFb}
      muted={$micCeiling1MuteFb}
      onLineOutChange={(n) => publishAnalog(SIGNALS.micCeiling1LineOut, n)}
      onTrimChange={(n) => publishAnalog(SIGNALS.micCeiling1Trim, n)}
      onMuteToggle={() => publishDigital(SIGNALS.micCeiling1Mute, !$micCeiling1MuteFb)}
    />
    <MixerChannel
      type="Ceiling · Ch 4"
      name="Array B"
      model="MXA920W-S"
      connected={$micCeiling2Connected}
      level={$micCeiling2Level}
      lineOut={$micCeiling2LineOutFb}
      trim={$micCeiling2TrimFb}
      muted={$micCeiling2MuteFb}
      onLineOutChange={(n) => publishAnalog(SIGNALS.micCeiling2LineOut, n)}
      onTrimChange={(n) => publishAnalog(SIGNALS.micCeiling2Trim, n)}
      onMuteToggle={() => publishDigital(SIGNALS.micCeiling2Mute, !$micCeiling2MuteFb)}
    />

    <div class="divider" aria-hidden="true"></div>

    <MasterStrip
      progLevel={$progAudioLevelFb}
      audioOutput={$audioOutputSelectFb}
      onProgLevelChange={onProgLevelChange}
      onOutputSelect={onOutputSelect}
    />
  </div>

  <!-- FOOTER ──────────────────────────────────────────────────────── -->
  <footer class="mixer-footer">
    <span class="f-label">Presets</span>
    <div class="presets">
      {#each PRESETS as p}
        {@const isActive = $sceneRecallFb === p.value}
        <button
          type="button"
          class="preset"
          class:active={isActive}
          onclick={() => recallScene(p.value)}
          aria-pressed={isActive}
        >{p.label}</button>
      {/each}
    </div>

    <div class="fsp"></div>

    <button
      type="button"
      class="link-chip"
      class:on={$audioLinkCeilings12Fb}
      onclick={toggleLinkCeilings}
      aria-pressed={$audioLinkCeilings12Fb}
    >
      <span class="link-dot" class:on={$audioLinkCeilings12Fb}></span>
      Link Arrays A+B
    </button>
  </footer>
</div>

<style>
  .mixer-page {
    width: 100%;
    height: 100%;
    display: grid;
    grid-template-rows: 60px 1fr 88px;
    gap: 10px;
    padding: 10px;
    box-sizing: border-box;
  }

  /* ── Header ─────────────────────────────────────────────────────── */
  .mixer-header {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    align-items: center;
    padding: 0 16px;
    gap: 14px;
  }
  .back-btn {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 8px 14px;
    border-radius: 8px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    cursor: pointer;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease;
  }
  .back-btn:hover {
    background: rgba(30, 41, 59, 0.85);
    color: var(--color-copy);
    border-color: var(--color-accent-soft);
  }
  .room {
    font-size: 18px;
    font-weight: 900;
    color: var(--color-copy);
  }
  .sep {
    width: 1px;
    height: 20px;
    background: var(--color-border);
  }
  .eyebrow {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .hsp { flex: 1; }

  .master-chip {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 6px 10px;
    border-radius: 9px;
    background: rgba(245, 166, 35, 0.10);
    border: 0.5px solid rgba(245, 166, 35, 0.28);
    color: var(--color-accent);
  }
  .mc-label {
    font-size: 9px;
    font-weight: 800;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .mc-db {
    font-size: 14px;
    font-weight: 900;
    letter-spacing: 0.02em;
    font-variant-numeric: tabular-nums;
    min-width: 56px;
    text-align: center;
  }
  .mc-step {
    width: 28px;
    height: 28px;
    padding: 0;
    border-radius: 7px;
    border: 0.5px solid rgba(245, 166, 35, 0.35);
    background: rgba(245, 166, 35, 0.06);
    color: var(--color-accent);
    font-size: 16px;
    font-weight: 800;
    cursor: pointer;
    transition: background 110ms ease;
  }
  .mc-step:hover { background: rgba(245, 166, 35, 0.20); }

  .status-pill {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 6px 10px;
    border-radius: 999px;
    background: rgba(15, 23, 42, 0.5);
    border: 0.5px solid var(--color-border);
    color: var(--color-copy-soft);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: default;
  }
  .status-pill .status-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: rgba(248, 113, 113, 0.7);
    box-shadow: 0 0 5px rgba(248, 113, 113, 0.55);
  }
  .status-pill.online .status-dot {
    background: var(--color-success);
    box-shadow: 0 0 5px rgba(34, 197, 94, 0.6);
  }

  .mute-all {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 9px 16px;
    border-radius: 9px;
    background: rgba(239, 68, 68, 0.10);
    border: 0.5px solid rgba(239, 68, 68, 0.30);
    color: #fca5a5;
    font-size: 12px;
    font-weight: 800;
    letter-spacing: 0.10em;
    text-transform: uppercase;
    cursor: pointer;
    transition: background 110ms ease;
  }
  .mute-all:hover { background: rgba(239, 68, 68, 0.20); }

  /* ── Body ───────────────────────────────────────────────────────── */
  .mixer-body {
    display: grid;
    grid-template-columns: repeat(4, 1fr) 2px 140px;
    gap: 0;
    min-height: 0;
  }
  .divider {
    align-self: stretch;
    width: 2px;
    background: var(--color-border);
    margin: 4px 8px;
  }

  /* ── Footer ─────────────────────────────────────────────────────── */
  .mixer-footer {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    align-items: center;
    padding: 0 18px;
    gap: 14px;
  }
  .f-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .presets {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .preset {
    padding: 9px 16px;
    border-radius: 9px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    cursor: pointer;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease;
  }
  .preset:hover {
    background: rgba(51, 65, 85, 0.7);
    color: var(--color-copy);
  }
  .preset.active {
    background: rgba(245, 166, 35, 0.18);
    border-color: rgba(245, 166, 35, 0.55);
    color: var(--color-accent);
  }

  .fsp { flex: 1; }

  .link-chip {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 9px 14px;
    border-radius: 9px;
    background: rgba(100, 116, 139, 0.08);
    border: 0.5px solid var(--color-border);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    transition: background 110ms ease, border-color 110ms ease, color 110ms ease;
  }
  .link-chip.on {
    background: rgba(34, 197, 94, 0.10);
    border-color: rgba(34, 197, 94, 0.28);
    color: #86efac;
  }
  .link-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: rgba(100, 116, 139, 0.5);
  }
  .link-dot.on {
    background: currentColor;
    box-shadow: 0 0 6px currentColor;
  }
</style>
